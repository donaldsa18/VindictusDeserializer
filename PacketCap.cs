using System;
using System.Collections.Generic;
using PcapDotNet.Core;
using System.Net;
using Devcat.Core.Net.Message;
using Devcat.Core.Net;
using System.Text.RegularExpressions;
using System.Text;

namespace PacketCap
{
    class Program
    {
        private static byte[] buffer = new byte[66000];
        private static byte[] newbuffer = new byte[66000];
        private static int bufLen = 0;
        private static LinkedList<int> recvSize = new LinkedList<int>();

        private static Dictionary<int, String> classNames = new Dictionary<int, String>();
        private static Dictionary<int, Guid> getGuid = new Dictionary<int, Guid>();
        private static ICryptoTransform ct = new ServiceCore.CryptoTransformHeroes();
        private static bool first = true;
        private static MessageHandlerFactory mf = new MessageHandlerFactory();
        private static MessagePrinter mp = new MessagePrinter();

        private static DateTime last = new DateTime();

        static void Main(string[] args)
        {
            PacketDevice selectedDevice = getDevice();

            // Open the device
            using (PacketCommunicator communicator =
                selectedDevice.Open(65536,                                  // portion of the packet to capture
                                                                            // 65536 guarantees that the whole packet will be captured on all the link layers
                                    PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                    1000))                                  // read timeout
            {
                Console.WriteLine("Listening on " + selectedDevice.Description + "...");
                communicator.SetFilter("src host 192.168.0.200 and tcp src portrange 27000-27015");
                // Retrieve the packets
                PcapDotNet.Packets.Packet packet;
                
                do
                {
                    PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out packet);
                    
                    if (result != PacketCommunicatorReceiveResult.Ok) {
                        continue;
                    }
                    HandlePacket(packet);
                    
                    
                } while (true);
            }
        }

        private static void HandlePacket(PcapDotNet.Packets.Packet packet) {
            //clear the buffer if no messages for the last second
            DateTime now = new DateTime();
            if (now.Subtract(last).TotalSeconds > 1)
            {
                ClearBuffer();
                Console.WriteLine("Cleared buffer");
            }
            last = now;
            uint srcPort = ((uint)packet.Buffer[34] << 8) | (uint)packet.Buffer[35];
            uint dstPort = ((uint)packet.Buffer[36] << 8) | (uint)packet.Buffer[37];
            String srcIp = BytesToIpAddr(packet.Buffer, 26);
            String dstIp = BytesToIpAddr(packet.Buffer, 30);

            String src = String.Format("{0}:{1}", srcIp, srcPort);
            String dst = String.Format("{0}:{1}", dstIp, dstPort);
            int tcpStart = 34;

            //strip tcp header
            int dataStart = tcpStart + 20;
            if ((packet.Buffer[tcpStart + 12] >> 4) > 5)
            {
                dataStart += (packet.Buffer[tcpStart + 12] >> 4) * 4 - 20;
            }

            int dataBytes = packet.Length - dataStart;
            if (dataBytes == 0)
            {
                ClearBuffer();
                Console.WriteLine("TCP connection reset");
                ct = new ServiceCore.CryptoTransformHeroes();
                return;
            }
            if (dataBytes == 6) {
                Console.WriteLine("Ping");
                ClearBuffer();
                return;
            }

            String timestamp = packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff");
            recvSize.AddLast(dataBytes);
            Console.WriteLine(timestamp + ": " + src + "->" + dst + " bytes=" + dataBytes);
            Devcat.Core.Net.Message.Packet p;
            ArraySegment<byte> dataSeg;

            dataSeg = new ArraySegment<byte>(packet.Buffer, dataStart, dataBytes);
            p = new Devcat.Core.Net.Message.Packet(dataSeg);
            //Console.WriteLine("Decrypting a {0} byte array segment", dataSeg.Count);
            long salt = p.InstanceId;
            ct.Decrypt(dataSeg, salt);

            Buffer.BlockCopy(packet.Buffer, dataStart, buffer, bufLen, dataBytes);
            bufLen += dataBytes;

            bool reachedEnd = false;
            while (!reachedEnd && bufLen != 0)
            {
                dataSeg = new ArraySegment<byte>(buffer, 0, bufLen);
                p = new Devcat.Core.Net.Message.Packet(dataSeg);
                int pLen = 0;
                try
                {
                    pLen = p.Length;
                    if (pLen == 0)
                    {
                        ClearBuffer();
                        Console.WriteLine("Received ping");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("bufLen={0} pLen={1} recvSize={2}", bufLen, pLen, recvSizeToString());
                    }

                }
                catch (System.Runtime.Serialization.SerializationException e)
                {
                    Console.WriteLine("bufLen={0} recvSize={1}", bufLen, recvSizeToString());
                    Console.WriteLine("Bad length {0}", e.Message);
                    RemovePacket();
                    continue;
                }

                if (pLen > bufLen)
                {
                    String lookForMsgType = "";
                    try
                    {
                        if (classNames.ContainsKey(p.CategoryId))
                        {
                            lookForMsgType = String.Format(" for a {0} packet", classNames[p.CategoryId]);
                        }
                    }
                    catch { }
                    Console.WriteLine("Waiting for {0} bytes{1}", pLen - bufLen, lookForMsgType);
                    return;
                }
                if (pLen <= 2 || pLen == 6) {
                    //ClearBuffer();
                    ShortenBuffer(pLen);
                    Console.WriteLine("Invalid data packet with Length={0}",pLen);
                    continue;
                }
                Console.WriteLine("Read {0} bytes but need {1} bytes, creating object", bufLen, pLen);
                dataSeg = new ArraySegment<byte>(buffer, 0, pLen);
                p = new Devcat.Core.Net.Message.Packet(dataSeg);
                try
                {
                    Console.WriteLine(p);
                    if (classNames.ContainsKey(p.CategoryId))
                    {
                        String className = classNames[p.CategoryId];
                        Console.WriteLine("Found a {0}", className);
                    }
                    if (first)
                    {
                        first = false;
                        Object obj;
                        SerializeReader.FromBinary<Object>(p, out obj);
                        setupDicts(obj.ToString());
                        mf.Handle(p, "hello");
                        mp.registerPrinters(mf, getGuid);
                        Console.WriteLine("Received TypeConverer");
                    }
                    else
                    {
                        mf.Handle(p, "hello");
                    }

                    //ShortenBuffer(p.Length);
                    //Assume the rest of the buffer is filler
                    ClearBuffer();
                }

                catch (InvalidOperationException e)
                {
                    if (classNames.ContainsKey(p.CategoryId))
                    {
                        String className = classNames[p.CategoryId];
                        Guid guid = getGuid[p.CategoryId];
                        Console.WriteLine("Library said {0}, I think its a {1} ({2})", e.Message, className, guid.ToString());
                    }
                    else
                    {
                        Console.WriteLine("Library said {0}, idk either", e.Message);
                    }
                    ClearBuffer();
                }
                catch (System.Runtime.Serialization.SerializationException e)
                {
                    Console.WriteLine("The packet wasn't ready: {0}", e.Message);
                    RemovePacket();
                }
                catch (System.ArgumentOutOfRangeException e)
                {
                    Console.WriteLine("The packet was too short: {0}", e.Message);
                    ShortenBuffer(pLen);
                }
                catch (System.ArgumentException e)
                {
                    Console.WriteLine("Serializing failed bacause a dict was made with 2 identical keys: {0}", e.Message);
                    ClearBuffer();
                }
            }
        }

        private static void ShortenBuffer(int pLen) {
            Buffer.BlockCopy(buffer, pLen, newbuffer, 0, bufLen - pLen);
            bufLen -= pLen;

            //swap them so buffer doesn't have a blank space
            byte[] temp = buffer;
            buffer = newbuffer;
            newbuffer = temp;
            int removed = pLen;
            while (recvSize.Count != 0 && removed > 0) {
                removed -= recvSize.Last.Value;
                recvSize.RemoveLast();
            }
            if (removed == 0) {
                return;
            }
            if (recvSize.Count == 0)
            {
                recvSize.AddFirst(bufLen);
            }
            else {
                recvSize.AddFirst(-removed);
            }
        }

        private static void ClearBuffer() {
            bufLen = 0;
            recvSize.Clear();
        }

        private static void RemovePacket() {
            ShortenBuffer(recvSize.First.Value);
        }

        static String BytesToIpAddr(byte[] p, int offset) {
            byte[] addrBytes = new byte[4]{ p[offset], p[offset+1], p[offset + 2], p[offset + 3] };
            IPAddress addr = new IPAddress(addrBytes);
            return addr.ToString();
        }

        private static String recvSizeToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (int cur in recvSize) {
                sb.Append(cur);
                sb.Append(",");
            }
            if (recvSize.Count != 0) {
                sb.Remove(sb.Length - 1, 1);
            }
            sb.Append("]");
            return sb.ToString();
        }

        static void setupDicts(String contents)
        {
            if (contents.StartsWith("TypeConverter"))
            {
                MatchCollection mc = Regex.Matches(contents, @"0x([A-F0-9]+)[,\s\{]*FullName = ([A-Za-z\._]+), GUID = ([a-z0-9-]+)");
                Dictionary<String, bool> loaded = new Dictionary<String, bool>();
                foreach (Match m in mc)
                {
                    int categoryId = int.Parse(m.Groups[1].ToString(), System.Globalization.NumberStyles.HexNumber);
                    String className = m.Groups[2].ToString();
                    Guid guid = Guid.Parse(m.Groups[3].ToString());
                    classNames.Add(categoryId, className);
                    getGuid.Add(categoryId, guid);
                    loaded.Add(String.Format("{0} {1}",className,guid.ToString()), false);
                }
            }
        }
        static PacketDevice getDevice() {
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            if (allDevices.Count == 0)
            {
                Console.WriteLine("No interfaces found! Make sure WinPcap is installed.");
                return null;
            }
            foreach (LivePacketDevice device in allDevices)
            {
                foreach (DeviceAddress addr in device.Addresses)
                {
                    if (addr.Address.ToString().Contains("Internet 192.168.0"))
                    {
                        Console.WriteLine("Found device with IP " + addr.Address);
                        return device;
                    }
                }
            }
            return null;
        }
    }
}