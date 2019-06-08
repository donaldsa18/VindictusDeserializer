using System;
using System.Collections.Generic;
using PcapDotNet.Core;
using System.Net;
using Devcat.Core.Net.Message;
using Devcat.Core.Net;
using System.Text.RegularExpressions;

namespace PacketCap
{
    class Program
    {
        static void Main(string[] args)
        {
            // Retrieve the device list from the local machine
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;

            if (allDevices.Count == 0)
            {
                Console.WriteLine("No interfaces found! Make sure WinPcap is installed.");
                return;
            }
            PacketDevice selectedDevice = null;
            foreach (LivePacketDevice device in allDevices) {
                
                foreach (DeviceAddress addr in device.Addresses)
                {
                    if (addr.Address.ToString().Contains("Internet 192.168.0")) {
                        Console.WriteLine("Found device with IP "+ addr.Address);
                        selectedDevice = device;
                        break;
                    }
                }
                if (selectedDevice != null)
                {
                    break;
                }
            }

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
                ICryptoTransform ct = new ServiceCore.CryptoTransformHeroes();
                byte[] buffer = new byte[66000];
                byte[] newbuffer = new byte[66000];
                int bufLen = 0;
 
                Dictionary<int, String> classNames = new Dictionary<int, String>();
                Dictionary<int, Guid> getGuid = new Dictionary<int, Guid>();
                bool first = true;
                MessageHandlerFactory mf = new MessageHandlerFactory();
                MessagePrinter mp = new MessagePrinter();
                Queue<int> recvSize = new Queue<int>();
                DateTime last = new DateTime();
                
                do
                {
                    PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out packet);
                    
                    //ArraySegment<byte> bufSeg = new ArraySegment<byte>(buffer, 0, 0);
                    switch (result)
                    {
                        case PacketCommunicatorReceiveResult.Timeout:
                            // Timeout elapsed
                            continue;
                        case PacketCommunicatorReceiveResult.Ok:
                            //clear the buffer if no messages for the last second
                            DateTime now = new DateTime();
                            if (now.Subtract(last).TotalSeconds > 1) {
                                bufLen = 0;
                                recvSize.Clear();
                            }
                            uint srcPort = ((uint)packet.Buffer[34] << 8) | (uint)packet.Buffer[35];
                            uint dstPort = ((uint)packet.Buffer[36] << 8) | (uint)packet.Buffer[37];
                            String srcIp = BytesToIpAddr(packet.Buffer, 26);
                            String dstIp = BytesToIpAddr(packet.Buffer, 30);

                            String src = String.Format("{0}:{1}", srcIp, srcPort);
                            String dst = String.Format("{0}:{1}", dstIp, dstPort);
                            int tcpStart = 34;

                            
                            //int bufferLen = 0;
                            //Console.WriteLine("\t" + PrintBytes(packet.Buffer));
                            //strip tcp header
                            int dataStart = tcpStart + 20;
                            if ((packet.Buffer[tcpStart + 12] >> 4) > 5) {
                                dataStart += (packet.Buffer[tcpStart + 12] >> 4) * 4 - 20;
                            }
                            
                            int dataBytes = packet.Length - dataStart;
                            String timestamp = packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff");
                            recvSize.Enqueue(dataBytes);
                            Console.WriteLine(timestamp + ": " + src + "->" + dst + " bytes=" + dataBytes);
                            Devcat.Core.Net.Message.Packet p;
                            ArraySegment<byte> dataSeg;
                            if (dataBytes > 0)
                            {
                                dataSeg = new ArraySegment<byte>(packet.Buffer, dataStart, dataBytes);
                                p = new Devcat.Core.Net.Message.Packet(dataSeg);
                                Console.WriteLine("Decrypting a {0} byte array segment", dataSeg.Count);
                                long salt = p.InstanceId;
                                ct.Decrypt(dataSeg, salt);
                                
                                Buffer.BlockCopy(packet.Buffer, dataStart, buffer, bufLen, dataBytes);
                                bufLen += dataBytes;
                                dataSeg = new ArraySegment<byte>(buffer, 0, bufLen);
                                p = new Devcat.Core.Net.Message.Packet(dataSeg);
                                
                                int pLen = 0;
                                try {
                                    pLen = p.Length;
                                    Console.WriteLine("bufLen={0} pLen={1}", bufLen, pLen);
                                }
                                catch(System.Runtime.Serialization.SerializationException e) {
                                    Console.WriteLine("Bad length {0}", e.Message);
                                    int firstPLen = recvSize.Dequeue();
                                    if (bufLen > firstPLen)
                                    {
                                        Buffer.BlockCopy(buffer, firstPLen, newbuffer, 0, bufLen - firstPLen);
                                        bufLen -= firstPLen;

                                        //swap them so buffer doesn't have a blank space
                                        byte[] temp = buffer;
                                        buffer = newbuffer;
                                        newbuffer = temp;
                                    }
                                    else {
                                        bufLen = 0;
                                        recvSize.Clear();
                                    }
                                    break;
                                }
                                if (pLen == 0) {

                                }
                                if (pLen > bufLen) {
                                    Console.WriteLine("Waiting for {0} bytes", pLen-bufLen);
                                }
                                if (pLen <= bufLen) {
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
                                            setupDicts(classNames, getGuid, obj.ToString());
                                            mf.Handle(p, "hello");
                                            mp.registerPrinters(mf, getGuid);
                                        }
                                        else
                                        {
                                            mf.Handle(p, "hello");
                                        }

                                        Buffer.BlockCopy(buffer, p.Length, newbuffer, 0, bufLen - p.Length);
                                        bufLen -= p.Length;

                                        //swap them so buffer doesn't have a blank space
                                        byte[] temp = buffer;
                                        buffer = newbuffer;
                                        newbuffer = temp;
                                        recvSize.Clear();
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
                                        bufLen = 0;
                                        recvSize.Clear();
                                    }
                                    catch (System.Runtime.Serialization.SerializationException e)
                                    {
                                        Console.WriteLine("The packet wasn't ready: {0}", e.Message);
                                        int firstPLen = recvSize.Dequeue();
                                        if (bufLen > firstPLen)
                                        {
                                            Buffer.BlockCopy(buffer, firstPLen, newbuffer, 0, bufLen - firstPLen);
                                            bufLen -= firstPLen;

                                            //swap them so buffer doesn't have a blank space
                                            byte[] temp = buffer;
                                            buffer = newbuffer;
                                            newbuffer = temp;
                                        }
                                        else
                                        {
                                            bufLen = 0;
                                            recvSize.Clear();
                                        }
                                    }
                                    catch (System.ArgumentOutOfRangeException e)
                                    {
                                        Console.WriteLine("The packet was too short: {0}", e.Message);
                                        bufLen = RemovePacket(recvSize,buffer,newbuffer,bufLen);
                                    }
                                    catch (System.ArgumentException e)
                                    {
                                        Console.WriteLine("Serializing failed bacause a dict was made with 2 identical keys: {0}", e.Message);
                                    }
                                    //copy the rest of the buffer to the other one
                                    //Buffer.BlockCopy(buffer, pLen, newbuffer, 0, bufLen - pLen);
                                    //bufLen -= pLen;

                                    //swap them so buffer doesn't have a blank space
                                    //byte[] temp = buffer;
                                    //buffer = newbuffer;
                                    //newbuffer = temp;
                                }
                            }
                            if (dataBytes == 0) {
                                bufLen = 0;
                                recvSize.Clear();
                            }

                            break;
                        default:
                            throw new InvalidOperationException("The result " + result + " shoudl never be reached here");
                    }
                } while (true);
            }
        }

        private static int RemovePacket(Queue<int> recvSize, byte[] buffer, byte[] newbuffer, int bufLen) {
            int firstPLen = recvSize.Dequeue();
            if (bufLen > firstPLen)
            {
                Buffer.BlockCopy(buffer, firstPLen, newbuffer, 0, bufLen - firstPLen);
                bufLen -= firstPLen;

                //swap them so buffer doesn't have a blank space
                byte[] temp = buffer;
                buffer = newbuffer;
                newbuffer = temp;
            }
            else
            {
                bufLen = 0;
                recvSize.Clear();
            }
            return bufLen;
        }

        static String BytesToIpAddr(byte[] p, int offset) {
            byte[] addrBytes = new byte[4]{ p[offset], p[offset+1], p[offset + 2], p[offset + 3] };
            IPAddress addr = new IPAddress(addrBytes);
            return addr.ToString();
        }
        static void setupDicts(Dictionary<int, String> classNames, Dictionary<int, Guid> getGuid, String contents)
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
    }
}