using System;
using System.Collections.Generic;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using System.Text;
using System.Net;
using Devcat.Core.Net.Message;
using Devcat.Core.Net;
using ServiceCore;
using Utility;
using System.Text.RegularExpressions;
using System.Reflection;

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
                int bufLen = 0;
                int decodeOffset = 0;

                Dictionary<int, String> classNames = new Dictionary<int, String>();
                Dictionary<int, Guid> getGuid = new Dictionary<int, Guid>();
                bool haveDicts = false;
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

                            Console.WriteLine(timestamp + ": " + src + "->" + dst + " bytes=" + dataBytes);

                            if (dataBytes > 0)
                            {
                                Buffer.BlockCopy(packet.Buffer, dataStart, buffer, bufLen, dataBytes);
                                bufLen += dataBytes;

                                //Console.WriteLine("packet is {0} bytes long", bufLen);
                            }
                            if (0 < dataBytes && dataBytes < 1460)
                            {

                                ArraySegment<byte> dataSeg = new ArraySegment<byte>(buffer, decodeOffset, bufLen - decodeOffset);
                                Console.WriteLine("Decrypting a {0} byte array segment", dataSeg.Count);
                                long salt = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, 0));
                                ct.Decrypt(dataSeg, salt);
                                dataSeg = new ArraySegment<byte>(buffer, 0, bufLen);
                                Devcat.Core.Net.Message.Packet p = new Devcat.Core.Net.Message.Packet(dataSeg)
                                {
                                    InstanceId = salt
                                };
                                Object obj;
                                Console.WriteLine(p);
                                if (p.BodyOffset >= p.Length) {
                                    Console.WriteLine("this packet is too short body offset={0} length={1}", p.BodyOffset, p.Length);
                                }

                                try
                                {

                                    SerializeReaderFixed.FromBinary<Object>(p, out obj);

                                    /*if (!haveDicts)
                                    {
                                        String contents = obj.ToString();
                                        setupDicts(classNames, getGuid, contents);
                                        haveDicts = true;
                                        FileLog.Log("objects.log", contents);
                                    }*/
                                    

                                    Console.WriteLine("Found a {0}", obj.GetType().ToString());
                                    
                                    
                                    bufLen = 0;
                                    decodeOffset = 0;
                                }
                                catch (System.InvalidOperationException e)
                                {
                                    if (classNames.ContainsKey(p.CategoryId))
                                    {
                                        String className = classNames[p.CategoryId];
                                        Guid guid = getGuid[p.CategoryId];
                                        Console.WriteLine("Library said {0}, I think its a {1} ({2})", e.Message, className, guid.ToString());
                                    }
                                    else {
                                        Console.WriteLine("Library said {0}, idk either", e.Message);
                                    }
                                    
                                    decodeOffset = bufLen;
                                }
                                
                            }
                            if (dataBytes == 0) {
                                bufLen = 0;
                                decodeOffset = 0;
                            }


                            break;
                        default:
                            throw new InvalidOperationException("The result " + result + " shoudl never be reached here");
                    }
                } while (true);
            }
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
                /*foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (Type type in assembly.GetTypes())
                        {
                            if ((!(type.Namespace != "System") || !(type.Namespace != "System.Collections.Generic")) && (type.IsSerializable && !type.IsDefined(typeof(ObsoleteAttribute), false)) && (!type.IsInterface && !type.IsAbstract && (!type.IsArray && type.IsVisible)))
                            {
                                loaded.Add(String.Format("{0} {1}", type.FullName, type.GUID.ToString()), true);
                            }
                            else if (getGuid.ContainsValue(type.GUID)) {
                                bool notSys = (!(type.Namespace != "System") || !(type.Namespace != "System.Collections.Generic"));
                                bool obsolete = type.IsDefined(typeof(ObsoleteAttribute), false);
                                String typeStatus = String.Format("Type {0}: NameSpace: {1} serializable: {2} ObsoleteAttribute: {3} IsInterface: {4} IsAbstract: {5} IsArray: {6} IsVisible: {7}, NotSys: {8}", type.FullName, type.Namespace, type.IsSerializable, obsolete, type.IsInterface, type.IsAbstract, type.IsArray, type.IsVisible, notSys);
                                FileLog.Log("typeStatus.log", typeStatus);
                                Console.WriteLine(typeStatus);
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException)
                    {
                    }
                }
                foreach (KeyValuePair<String, bool> entry in loaded) {
                    if (!entry.Value)
                    {
                        String missingStatus = String.Format("Missing {0}", entry.Key);
                        FileLog.Log("typeStatus.log", missingStatus);
                        Console.WriteLine(missingStatus);
                    }
                }*/
            }
        }
    }
}