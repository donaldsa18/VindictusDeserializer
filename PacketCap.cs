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
    class PacketCap
    {
        private byte[] buffer = new byte[66000];
        private byte[] newbuffer = new byte[66000];
        private int bufLen = 0;
        private LinkedList<int> recvSize = new LinkedList<int>();

        private Dictionary<int, String> classNames = new Dictionary<int, String>();
        public Dictionary<int, Guid> getGuid = new Dictionary<int, Guid>();

        private ICryptoTransform ct = null;


        private MessageHandlerFactory mf = new  MessageHandlerFactory();
        private MessagePrinter mp = new MessagePrinter();
        
        internal bool sawSyn { get; private set; }
        
        private bool encrypt;

        private static Dictionary<string, PacketCap> portHandler = new Dictionary<string, PacketCap>();

        private static string myIp = "";

        private static string filter = "host 192.168.0.200 and tcp portrange 27000-27015";

        static void Main(string[] args)
        {
            PacketDevice selectedDevice = getDevice();

            //Open the device with a 65kB buffer, promiscuous mode, 1s timeout
            using (PacketCommunicator communicator = selectedDevice.Open(65536,PacketDeviceOpenAttributes.Promiscuous,1000))
            {
                Console.WriteLine("Waiting for TCP stream to start on {0}", selectedDevice.Description);

                communicator.SetFilter(filter);

                // Retrieve the packets
                communicator.ReceivePackets(0, HandlePacketPort);
            }
        }
        private static void HandlePacketPort(PcapDotNet.Packets.Packet packet)
        {
            string connString = getConnString(packet.Buffer);
            if (portHandler.TryGetValue(connString, out PacketCap cap))
            {
                cap.HandlePacket(packet,connString);
            }
            else {
                cap = new PacketCap();
                portHandler.Add(connString, cap);
                Console.WriteLine("Creating PacketCap for connection {0}", connString);
                cap.HandlePacket(packet,connString);
            }
        }


        private void HandlePacket(PcapDotNet.Packets.Packet packet,String connString) {

            uint srcPort = ((uint)packet.Buffer[34] << 8) | (uint)packet.Buffer[35];
            String srcIp = BytesToIpAddr(packet.Buffer, 26);

            int tcpStart = 34;

            //strip tcp header
            int dataStart = tcpStart + 20;
            if ((packet.Buffer[tcpStart + 12] >> 4) > 5)
            {
                dataStart += (packet.Buffer[tcpStart + 12] >> 4) * 4 - 20;
            }

            int dataBytes = packet.Length - dataStart;
            bool syn = (packet.Buffer[47] & 0b10) == 0b10;
            if (syn && dataBytes == 0)
            {
                ct = new ServiceCore.CryptoTransformHeroes();
                ClearBuffer();
                encrypt = !anySynSeen();
                Console.WriteLine("TCP connection starting{0}",encrypt?" with encryption" : "");
                sawSyn = true;
                return;
            }
            else if (!sawSyn)
            {
                Console.WriteLine("Haven't seen SYN yet from {0}", srcPort);
                return;
            }
            
            if (dataBytes == 6 || dataBytes == 0) {
                //Console.WriteLine("Ping from port {0}",srcPort);
                ClearBuffer();
                return;
            }

            String timestamp = packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff");
            Console.WriteLine("{0}: {1} bytes={2}",timestamp,connString,dataBytes);
            
            recvSize.AddLast(dataBytes);
            Buffer.BlockCopy(packet.Buffer, dataStart, buffer, bufLen, dataBytes);

            ArraySegment<byte> dataSeg = new ArraySegment<byte>(buffer, bufLen, dataBytes);
            Devcat.Core.Net.Message.Packet p = new Devcat.Core.Net.Message.Packet(dataSeg);
            if (encrypt) {
                long salt = p.InstanceId;
                ct.Decrypt(dataSeg, salt);
            }
            
            bufLen += dataBytes;
            
            while (bufLen != 0)
            {
                dataSeg = new ArraySegment<byte>(buffer, 0, bufLen);
                p = new Devcat.Core.Net.Message.Packet(dataSeg);
                int pLen = 0;
                try
                {
                    pLen = p.Length+p.BodyOffset;
                    if (pLen == 0)
                    {
                        ClearBuffer();
                        //Console.WriteLine("Received ping");
                        return;
                    }
                    else
                    {
                        //Console.WriteLine("bufLen={0} pLen={1} recvSize={2}", bufLen, pLen, recvSizeToString());
                    }

                }
                catch (System.Runtime.Serialization.SerializationException e)
                {
                    Console.WriteLine("bufLen={0} recvSize={1}", bufLen, recvSizeToString());
                    Console.WriteLine("Bad length {0}", e.Message);//this was the original code
                    RemovePacket();
                    continue;
                }

                if (pLen > bufLen)
                {
                    /*String lookForMsgType = "";
                    try
                    {
                        if (classNames.ContainsKey(p.CategoryId))
                        {
                            lookForMsgType = String.Format(" for a {0} packet", classNames[p.CategoryId]);
                        }
                    }
                    catch { }
                    Console.WriteLine("Waiting for {0} bytes{1}", pLen - bufLen, lookForMsgType);*/
                    return;
                }
                if (pLen <= 3 || pLen == 6) {
                    //ClearBuffer();
                    ShortenBuffer(pLen);
                    Console.WriteLine("Invalid data packet with Length={0}",pLen);
                    continue;
                }
                //Console.WriteLine("Read {0} bytes but need {1} bytes, creating object", bufLen, pLen);
                dataSeg = new ArraySegment<byte>(buffer, 0, pLen);
                p = new Devcat.Core.Net.Message.Packet(dataSeg);
                try
                {
                    //Console.WriteLine(p);
                    if (srcIp == myIp)
                    {
                        Console.WriteLine("Client:");
                    }
                    else {
                        Console.WriteLine("Server:");
                    }
                    if (classNames.Count == 0)
                    {
                        processTypeConverter(p);
                        Console.WriteLine("Received TypeConverter");
                        String reverse = reverseConnString(connString);
                        Console.WriteLine("Sending TypeConverter to Client {0}",reverse);
                        portHandler[reverse].processTypeConverter(p);
                    }
                    else
                    {
                        mf.Handle(p, null);
                    }

                    ShortenBuffer(pLen);
                    //Assume the rest of the buffer is filler
                    //ClearBuffer();
                }

                catch (InvalidOperationException e)
                {
                    if (classNames.ContainsKey(p.CategoryId))
                    {
                        String className = classNames[p.CategoryId];
                        Guid guid = getGuid[p.CategoryId];
                        Console.WriteLine("Found {0}, but {1}",className,e.Message);
                    }
                    else
                    {
                        Console.WriteLine("Unknown class error {0}", e.Message);
                    }
                    ShortenBuffer(pLen);
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
                    Console.WriteLine("Serializing failed bacause a dict was made with 2 identical keys: {0}", e.StackTrace);
                    ClearBuffer();
                }
            }
        }
        private void ShortenBuffer(int pLen) {
            if (pLen == bufLen) {
                ClearBuffer();
                return;
            }
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

        public void processTypeConverter(Devcat.Core.Net.Message.Packet p) {
            SerializeReader.FromBinary<Object>(p, out Object obj);
            SetupDicts(obj.ToString());
            mf.Handle(p, null);
            mp.registerPrinters(mf, getGuid);
        }

        private static String reverseConnString(String connString) {
            String[] parts = connString.Split(' ');
            StringBuilder sb = new StringBuilder();
            //'IP:Port','to','IP:port'
            sb.Append(parts[2]);
            sb.Append(" to ");
            sb.Append(parts[0]);
            return sb.ToString();
        }

        private static string getConnString(byte[] Buffer) {
            uint srcPort = ((uint)Buffer[34] << 8) | (uint)Buffer[35];
            uint dstPort = ((uint)Buffer[36] << 8) | (uint)Buffer[37];
            String srcIp = BytesToIpAddr(Buffer, 26);
            String dstIp = BytesToIpAddr(Buffer, 30);
            StringBuilder sb = new StringBuilder();
            sb.Append(srcIp);
            sb.Append(":");
            sb.Append(srcPort);
            sb.Append(" to ");
            sb.Append(dstIp);
            sb.Append(":");
            sb.Append(dstPort);
            return sb.ToString();
        }

        private void ClearBuffer() {
            bufLen = 0;
            recvSize.Clear();
        }

        private void RemovePacket() {
            ShortenBuffer(recvSize.First.Value);
        }

        static String BytesToIpAddr(byte[] p, int offset) {
            byte[] addrBytes = new byte[4]{ p[offset], p[offset+1], p[offset + 2], p[offset + 3] };
            IPAddress addr = new IPAddress(addrBytes);
            return addr.ToString();
        }

        private String recvSizeToString() {
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

        private bool anySynSeen() {
            int numSyn = 0;
            foreach (KeyValuePair<string,PacketCap> entry in portHandler) {
                if (entry.Value.sawSyn) {
                    numSyn++;
                }
            }
            return numSyn > 1;
        }

        private void SetupDicts(String contents)
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
        private static PacketDevice getDevice() {
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
                        myIp = addr.Address.ToString().Split(' ')[1];
                        return device;
                    }
                }
            }
            return null;
        }
    }
}