
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
    class ChannelProcessor
    {
        private static byte[] buffer = new byte[66000];
        private static byte[] newbuffer = new byte[66000];
        private static int bufLen = 0;
        //private static LinkedList<int> recvSize = new LinkedList<int>();

        //private static Dictionary<int, String> classNames = new Dictionary<int, String>();
        private static Dictionary<int, Guid> getGuid = new Dictionary<int, Guid>();

        private static bool first = true;
        private static MessageHandlerFactory mf = new MessageHandlerFactory();
        private static MessagePrinter mp = new MessagePrinter();

        private static MessageAnalyzer ma = new MessageAnalyzer();

        private static bool sawSyn = false;
        public static int Main(string[] args)
        {
            string filter = "src host 192.168.0.200 and tcp port 27003";

            PacketDevice selectedDevice = getDevice();

            // Open the device
            using (PacketCommunicator communicator =
                selectedDevice.Open(65536,                                  // portion of the packet to capture
                                                                            // 65536 guarantees that the whole packet will be captured on all the link layers
                                    PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                    1000))                                  // read timeout
            {
                Console.WriteLine("Waiting for TCP stream to start on {0}", selectedDevice.Description);
                communicator.SetFilter(filter);
                // Retrieve the packets

                do
                {
                    PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out PcapDotNet.Packets.Packet packet);

                    if (result != PacketCommunicatorReceiveResult.Ok)
                    {
                        continue;
                    }
                    HandlePacket(packet);

                } while (true);
            }
        }

        private static void HandlePacket(PcapDotNet.Packets.Packet packet)
        {

            uint srcPort = ((uint)packet.Buffer[34] << 8) | (uint)packet.Buffer[35];
            uint dstPort = ((uint)packet.Buffer[36] << 8) | (uint)packet.Buffer[37];

            String srcIp = BytesToIpAddr(packet.Buffer, 26);
            String dstIp = BytesToIpAddr(packet.Buffer, 30);

            String src = String.Format("{0}:{1}", srcIp, srcPort);
            String dst = String.Format("{0}:{1}", dstIp, dstPort);
            int tcpStart = 34;

            //int seqNum = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(packet.Buffer, 38));


            //strip tcp header
            int dataStart = tcpStart + 20;
            if ((packet.Buffer[tcpStart + 12] >> 4) > 5)
            {
                dataStart += (packet.Buffer[tcpStart + 12] >> 4) * 4 - 20;
            }

            int dataBytes = packet.Length - dataStart;

            if (dataBytes == 6)
            {
                Console.WriteLine("Ping");
                return;
            }

            if (dataBytes == 0) {
                ma.CryptoTransform = new CryptoTransform();
                bufLen = 0;
                Console.WriteLine("Reset");
            }

            String timestamp = packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff");
            
            Console.WriteLine("{0}: {1}->{2} bytes={3}", timestamp, src, dst, dataBytes);
            Devcat.Core.Net.Message.Packet p;
            ArraySegment<byte> dataSeg;

            dataSeg = new ArraySegment<byte>(packet.Buffer, dataStart, dataBytes);

            //trying no decryption
            Buffer.BlockCopy(packet.Buffer, dataStart, buffer, bufLen, dataBytes);
            bufLen += dataBytes;
            int pLen = 0;
            /*ma.Add(dataSeg);
            foreach(ArraySegment<byte> seg in ma) {
                p = new Packet(seg);
                pLen = p.Length + p.BodyOffset;
                Buffer.BlockCopy(seg.Array, seg.Offset, buffer, bufLen, pLen);
                bufLen += pLen;
            }*/
            dataSeg = new ArraySegment<byte>(buffer, 0, bufLen);
            p = new Packet(dataSeg);
            pLen = p.Length + p.BodyOffset;
            Console.WriteLine("Need {0} bytes, have {1} bytes in buffer",pLen,bufLen);
            if (pLen <= bufLen)
            {
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
                if (pLen == bufLen)
                {
                    bufLen = 0;
                }
                else {
                    Buffer.BlockCopy(buffer, pLen, newbuffer, 0, bufLen - pLen);
                    bufLen -= pLen;

                    //swap them so buffer doesn't have a blank space
                    byte[] temp = buffer;
                    buffer = newbuffer;
                    newbuffer = temp;
                }
            }
        }

        static String BytesToIpAddr(byte[] p, int offset)
        {
            byte[] addrBytes = new byte[4] { p[offset], p[offset + 1], p[offset + 2], p[offset + 3] };
            IPAddress addr = new IPAddress(addrBytes);
            return addr.ToString();
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
                    getGuid.Add(categoryId, guid);
                    loaded.Add(String.Format("{0} {1}", className, guid.ToString()), false);
                }
            }
        }
        static PacketDevice getDevice()
        {
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
