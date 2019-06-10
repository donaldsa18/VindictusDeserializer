using System;
using System.Collections.Generic;
using PcapDotNet.Core;
using System.Net;
using Devcat.Core.Net.Message;
using Devcat.Core.Net;
using System.Text.RegularExpressions;
using System.Text;
using PcapDotNet.Packets.Ip;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.IpV6;
using PcapDotNet.Packets.Transport;
using PcapDotNet.Packets.Ethernet;
using Utility;

namespace PacketCap
{
    class PacketCap
    {
        private byte[] buffer = new byte[66000];
        private byte[] newbuffer = new byte[66000];
        private int bufLen = 0;
        private LinkedList<int> recvSize = new LinkedList<int>();

        private ICryptoTransform ct = null;

        private static Dictionary<int, String> classNames = new Dictionary<int, String>();
        public static Dictionary<int, Guid> getGuid = new Dictionary<int, Guid>();

        public static MessageHandlerFactory mf = new  MessageHandlerFactory();
        private static MessagePrinter mp = new MessagePrinter();
        
        internal bool sawSyn { get; private set; }
        
        private bool encrypt;

        private static Dictionary<string, PacketCap> portHandler = new Dictionary<string, PacketCap>();

        private static string myIp = "";

        private static string filter = "host 192.168.0.200 and tcp portrange 27000-27015";

        private string connString;

        public PacketCap(string connString) {
            this.connString = connString;
        }

        static void Main(string[] args)
        {
            PacketDevice selectedDevice = GetDevice();
            
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
            string connString = GetConnString(packet);
            if (portHandler.TryGetValue(connString, out PacketCap cap))
            {
                cap.HandlePacket(packet);
            }
            else {
                cap = new PacketCap(connString);
                portHandler.Add(connString, cap);
                Console.WriteLine("Creating PacketCap for connection {0}", connString);
                cap.HandlePacket(packet);
            }
        }


        private void HandlePacket(PcapDotNet.Packets.Packet packet) {
            String srcIp = "";
            TcpDatagram tcp = null;
            EthernetDatagram eth = packet.Ethernet;
            int dataStart = eth.HeaderLength;
            switch (eth.EtherType) {
                case EthernetType.IpV4:
                    IpV4Datagram ip = eth.IpV4;
                    tcp = ip.Tcp;
                    srcIp = ip.Source.ToString();
                    dataStart += ip.HeaderLength + tcp.RealHeaderLength;
                    break;
                case EthernetType.IpV6:
                    IpV6Datagram ip6 = eth.IpV6;
                    tcp = ip6.Tcp;
                    srcIp = ip6.Source.ToString();
                    dataStart += 40 + tcp.RealHeaderLength;
                    Console.WriteLine("IPv6?");
                    break;
                default:
                    Console.WriteLine("We should never see anything not ipv4 or ipv6 since we filtered by tcp");
                    return;
            }
            
            ushort srcPort = tcp.SourcePort;
            int dataBytes = tcp.PayloadLength;
            bool syn = tcp.IsSynchronize;
            //Console.WriteLine("dataStart={0} dataByes={1} srcPort={2} syn={3} srcIp={4}",dataStart,dataBytes,srcPort,syn,srcIp);

            if (syn && dataBytes == 0)
            {
                ct = new ServiceCore.CryptoTransformHeroes();
                ClearBuffer();
                encrypt = !AnySynSeen();
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

            //String timestamp = packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff");
            //Console.WriteLine("{0}: {1} bytes={2}",timestamp,connString,dataBytes);
            
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
                    Console.WriteLine("bufLen={0} recvSize={1}", bufLen, RecvSizeToString());
                    Console.WriteLine("Bad length {0}", e.Message);//this was the original code
                    RemovePacket();
                    continue;
                }

                if (pLen > bufLen)
                {
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
                        ProcessTypeConverter(p);
                        Console.WriteLine("Received TypeConverter");
                        //String reverse = reverseConnString(connString);
                        //Console.WriteLine("Sending TypeConverter to Client {0}",reverse);
                        //portHandler[reverse].processTypeConverter(p);
                    }
                    else
                    {
                        mf.Handle(p, null);
                    }
                    ShortenBuffer(pLen);
                }
                catch (InvalidOperationException e)
                {
                    String errMsg = e.Message;
                    String className = "";
                    MatchCollection mc = Regex.Matches(errMsg, @"\.([^,\.]{2,}),");
                    foreach (Match m in mc)
                    {
                        className = m.Groups[1].ToString();
                        if (className != "Identify") {
                            String methodStr = GenMethodString(className);
                            FileLog.Log("unhandled.log", methodStr);
                        }
                    }
                    if (className != "Identify")
                    {
                        Console.WriteLine("Unknown class error {0} {1}", errMsg, connString);
                    }
                    else {
                        Console.WriteLine("Identify: [?]");
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
        private static string GenMethodString(string className) {
            StringBuilder sb = new StringBuilder();
            sb.Append("\nprivate static void Print");
            sb.Append(className);
            sb.Append("(");
            sb.Append(className);
            sb.Append(" msg, object tag) {\n\tConsole.WriteLine(\"");
            sb.Append(className);
            sb.Append(":\");\n}");
            return sb.ToString();
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

        public void ProcessTypeConverter(Devcat.Core.Net.Message.Packet p)
        {
            SerializeReader.FromBinary<Object>(p, out Object obj);
            String s1 = obj.ToString();
            SetupDicts(s1);
            mp.registerPrinters(mf, getGuid);
        }

        private static String ReverseConnString(String connString) {
            String[] parts = connString.Split(' ');
            StringBuilder sb = new StringBuilder();
            //'IP:Port','to','IP:port'
            sb.Append(parts[2]);
            sb.Append(" to ");
            sb.Append(parts[0]);
            return sb.ToString();
        }

        private static string GetConnString(PcapDotNet.Packets.Packet packet) {
            String srcIp = "";
            String dstIp = "";
            TcpDatagram tcp = null;
            EthernetDatagram eth = packet.Ethernet;
            switch (eth.EtherType)
            {
                case EthernetType.IpV4:
                    IpV4Datagram ip = eth.IpV4;
                    tcp = ip.Tcp;
                    srcIp = ip.Source.ToString();
                    dstIp = ip.Destination.ToString();
                    break;
                case EthernetType.IpV6:
                    IpV6Datagram ip6 = eth.IpV6;
                    tcp = ip6.Tcp;
                    srcIp = ip6.Source.ToString();
                    dstIp = ip6.CurrentDestination.ToString();
                    break;
                default:
                    Console.WriteLine("We should never see anything not ipv4 or ipv6 since we filtered by tcp");
                    return "";
            }
           
            StringBuilder sb = new StringBuilder();
            sb.Append(srcIp);
            sb.Append(":");
            sb.Append(tcp.SourcePort);
            sb.Append(" to ");
            sb.Append(dstIp);
            sb.Append(":");
            sb.Append(tcp.DestinationPort);
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

        private String RecvSizeToString() {
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

        private bool AnySynSeen() {
            int numSyn = 0;
            foreach (KeyValuePair<string,PacketCap> entry in portHandler) {
                if (entry.Value.sawSyn) {
                    numSyn++;
                }
            }
            //0 -> no
            //1 -> no
            //2 -> yes
            //3 -> yes
            //4 -> no
            return 1 < numSyn && numSyn < 4;
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
                    if (classNames.TryGetValue(categoryId, out string dictClassName)) {
                        if (dictClassName != null && dictClassName != className) {
                            Console.WriteLine("Error! Conflicting types going to dictionaries, please remove the static keyword");
                        }
                    }
                    else {
                        classNames.Add(categoryId, className);
                        getGuid.Add(categoryId, guid);
                    }
                }
            }
        }
        private static PacketDevice GetDevice() {
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