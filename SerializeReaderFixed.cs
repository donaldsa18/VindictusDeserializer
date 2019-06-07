// Decompiled with JetBrains decompiler
// Type: Devcat.Core.Net.Message.SerializeReaderFixed
// Assembly: Core, Version=1.2.5546.20396, Culture=neutral, PublicKeyToken=null
// MVID: B643316F-8FEA-46C6-ACBF-30466BA32C8C
// Assembly location: C:\Users\Anthony\VirtualBox VMs\Shared\vindictus\packetsearch\Bin\Core.dll

using Devcat.Core.Net.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace PacketCap
{
    public struct SerializeReaderFixed
    {
        private static readonly Guid arrayGuid = typeof(Array).GUID;
        private Packet data;
        private int position;
        [ThreadStatic]
        private static IDictionary<Guid, Type> dictionary;

        private SerializeReaderFixed(Packet packet)
        {
            this.data = packet;
            this.position = packet.Bytes.Offset + packet.BodyOffset;
        }

        public static void FromBinary<T>(Packet packet, out T value)
        {
            int categoryId = ClassInfoFixed<T>.CategoryId;
            SerializeReaderFixed reader = new SerializeReaderFixed(packet);
            if (packet.CategoryId != 0)
            {
                if (packet.CategoryId != categoryId && typeof(T).IsSealed)
                    throw new SerializationException(string.Format("Unexpected category {0:X8} for type {1}", (object)packet.CategoryId, (object)typeof(T).AssemblyQualifiedName));
                SerializeReaderHelperFixed<T, SerializeReaderFixed>.Deserialize(ref reader, out value);
            }
            else
            {
                object obj;
                SerializeReaderHelperFixed<object, SerializeReaderFixed>.Deserialize(ref reader, out obj);
                value = (T)obj;
            }
        }

        private void Read(out sbyte value)
        {
            value = (sbyte)this.data.Bytes.Array[this.position++];
        }

        private void Read(out byte value)
        {
            value = this.data.Bytes.Array[this.position++];
        }

        private void Read(out short value)
        {
            value = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(this.data.Bytes.Array, this.position));
            this.position += 2;
        }

        private void Read(out ushort value)
        {
            value = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(this.data.Bytes.Array, this.position));
            this.position += 2;
        }

        private void Read(out int value)
        {
            value = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(this.data.Bytes.Array, this.position));
            this.position += 4;
        }

        private void Read(out uint value)
        {
            value = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(this.data.Bytes.Array, this.position));
            this.position += 4;
        }

        private void Read(out long value)
        {
            value = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(this.data.Bytes.Array, this.position));
            this.position += 8;
        }

        private void Read(out ulong value)
        {
            value = (ulong)IPAddress.NetworkToHostOrder(BitConverter.ToInt64(this.data.Bytes.Array, this.position));
            this.position += 8;
        }

        private void Read(out char value)
        {
            value = (char)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(this.data.Bytes.Array, this.position));
            this.position += 2;
        }

        private unsafe void Read(out float value)
        {
            int hostOrder = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(this.data.Bytes.Array, this.position));
            value = *(float*)&hostOrder;
            this.position += 4;
        }

        private unsafe void Read(out double value)
        {
            long hostOrder = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(this.data.Bytes.Array, this.position));
            value = *(double*)&hostOrder;
            this.position += 8;
        }

        private void Read(out bool value)
        {
            value = this.data.Bytes.Array[this.position] != (byte)0;
            ++this.position;
        }

        private void ReadCount(out int value)
        {
            byte num;
            this.Read(out num);
            if (((int)num & 128) == 0)
            {
                value = (int)num;
            }
            else
            {
                if (((int)num & 224) == 192)
                {
                    value = (int)num << 6 & 1984;
                }
                else
                {
                    if (((int)num & 240) == 224)
                    {
                        value = (int)num << 12 & 61440;
                    }
                    else
                    {
                        if (((int)num & 248) == 240)
                        {
                            value = (int)num << 18 & 1835008;
                        }
                        else
                        {
                            if (((int)num & 252) == 248)
                            {
                                value = (int)num << 24 & 50331648;
                            }
                            else
                            {
                                if (((int)num & 252) != 252)
                                    throw new SerializationException(string.Format("Invalid leading 6-bits '{0:X2}'", (object)((int)num & 252)));
                                value = (int)num << 30 & -1073741824;
                                this.Read(out num);
                                if (((int)num & 192) != 128)
                                    throw new SerializationException("0x10wwwwww");
                                value |= (int)num << 24 & 1056964608;
                                if ((uint)value <= 67108863U)
                                    throw new SerializationException("0x111111ww");
                            }
                            this.Read(out num);
                            if (((int)num & 192) != 128)
                                throw new SerializationException("0x10zzzzzz");
                            value |= (int)num << 18 & 16515072;
                            if ((uint)value <= 2097151U)
                                throw new SerializationException("0x111110ww");
                        }
                        this.Read(out num);
                        if (((int)num & 192) != 128)
                            throw new SerializationException("0x10zzyyyy");
                        value |= (int)num << 12 & 258048;
                        if ((uint)value <= (uint)ushort.MaxValue)
                            throw new SerializationException("0x11110zzz");
                    }
                    this.Read(out num);
                    if (((int)num & 192) != 128)
                        throw new SerializationException("0x10yyyyxx");
                    value |= (int)num << 6 & 4032;
                    if ((uint)value <= 2047U)
                        throw new SerializationException("0x1110yyyy");
                }
                this.Read(out num);
                if (((int)num & 192) != 128)
                    throw new SerializationException("0x10xxxxxx");
                value |= (int)num & 63;
                if ((uint)value <= (uint)sbyte.MaxValue)
                    throw new SerializationException("0x110yyyxx");
            }
        }

        private unsafe void Read(out IntPtr value)
        {
            long num;
            this.Read(out num);
            value = new IntPtr((void*)num);
        }

        private void Read(out string value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = (string)null;
            }
            else
            {
                int num2;
                value = Encoding.UTF8.GetString(this.data.Bytes.Array, this.position, num2 = num1 - 1);
                this.position += num2;
            }
        }

        private void Read(out Type value)
        {
            Guid guid;
            SerializeReaderHelperFixed<Guid, SerializeReaderFixed>.Deserialize(ref this, out guid);
            if (guid == Guid.Empty)
                value = (Type)null;
            else if (guid == SerializeReaderFixed.arrayGuid)
            {
                Type type;
                this.Read(out type);
                int rank;
                this.ReadCount(out rank);
                value = rank == 0 ? type.MakeArrayType() : type.MakeArrayType(rank);
            }
            else
            {
                if (SerializeReaderFixed.dictionary == null)
                {
                    SerializeReaderFixed.dictionary = (IDictionary<Guid, Type>)new Dictionary<Guid, Type>();
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            foreach (Type type in assembly.GetTypes())
                            {
                                if ((!(type.Namespace != "System") || !(type.Namespace != "System.Collections.Generic")) && (type.IsSerializable && !type.IsDefined(typeof(ObsoleteAttribute), false)) && (!type.IsInterface && !type.IsAbstract && (!type.IsArray && type.IsVisible)))
                                    SerializeReaderFixed.dictionary.Add(type.GUID, type);
                            }
                        }
                        catch (ReflectionTypeLoadException)
                        {
                        }
                    }
                    Assembly dll = Assembly.LoadFrom(@"ServiceCore.dll");
                    try
                    {
                        foreach (Type type in dll.GetExportedTypes())
                        {
                            if (type.IsSerializable && !type.IsInterface && !type.IsAbstract && !type.IsArray && type.IsVisible && type.Namespace == "ServiceCore.EndPointNetwork" && !type.IsDefined(typeof(ObsoleteAttribute), false))
                            {
                                SerializeReaderFixed.dictionary.Add(type.GUID, type);
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException)
                    {
                    }
                }
                //look in the system first for built in guids
                if (!SerializeReaderFixed.dictionary.TryGetValue(guid, out value))
                {
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            foreach (Type type in assembly.GetTypes())
                            {
                                //then look among the custom imports
                                if (!(type.GUID != guid) && type.IsSerializable && !type.IsDefined(typeof(ObsoleteAttribute), false) && (type.IsVisible || !type.FullName.StartsWith("System.")))
                                {
                                    value = type;
                                    break;
                                }
                            }
                            //
                            if (!(value == (Type)null))
                                break;
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                        }
                    }
                    if (value == (Type)null)
                    {
                        value = (Type)new SerializeReaderFixed.UnknownType(guid);
                        return;
                    }
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            foreach (Type type in assembly.GetTypes())
                            {
                                if (type.Namespace == value.Namespace && type.IsSerializable && !type.IsDefined(typeof(ObsoleteAttribute), false) && !type.IsInterface && !type.IsAbstract && !type.IsArray && type.IsVisible)
                                {
                                    if (SerializeReaderFixed.dictionary.ContainsKey(type.GUID))
                                        SerializeReaderFixed.dictionary[type.GUID] = (Type)null;
                                    else
                                        SerializeReaderFixed.dictionary.Add(type.GUID, type);
                                }
                            }
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                        }
                    }
                }
                if (value == (Type)null)
                {
                    value = (Type)new SerializeReaderFixed.UnknownType(guid);
                }
                else
                {
                    if (!value.IsGenericTypeDefinition)
                        return;
                    Type[] typeArray = new Type[value.GetGenericArguments().Length];
                    for (int index = 0; index < typeArray.Length; ++index)
                        this.Read(out typeArray[index]);
                    value = value.MakeGenericType(typeArray);
                }
            }
        }

        private void Read(out sbyte[] value)
        {
            int num;
            this.ReadCount(out num);
            if (num == 0)
            {
                value = (sbyte[])null;
            }
            else
            {
                int count;
                value = new sbyte[count = num - 1];
                Buffer.BlockCopy((Array)this.data.Bytes.Array, this.position, (Array)value, 0, count);
                this.position += count;
            }
        }

        private void Read(out byte[] value)
        {
            int num;
            this.ReadCount(out num);
            if (num == 0)
            {
                value = (byte[])null;
            }
            else
            {
                int count;
                value = new byte[count = num - 1];
                Buffer.BlockCopy((Array)this.data.Bytes.Array, this.position, (Array)value, 0, count);
                this.position += count;
            }
        }

        private void Read(out short[] value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = (short[])null;
            }
            else
            {
                int num2;
                value = new short[num2 = num1 - 1];
                for (int index = 0; index < num2; ++index)
                    this.Read(out value[index]);
            }
        }

        private void Read(out ushort[] value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = (ushort[])null;
            }
            else
            {
                int num2;
                value = new ushort[num2 = num1 - 1];
                for (int index = 0; index < num2; ++index)
                    this.Read(out value[index]);
            }
        }

        private void Read(out int[] value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = (int[])null;
            }
            else
            {
                int num2;
                value = new int[num2 = num1 - 1];
                for (int index = 0; index < num2; ++index)
                    this.Read(out value[index]);
            }
        }

        private void Read(out uint[] value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = (uint[])null;
            }
            else
            {
                int num2;
                value = new uint[num2 = num1 - 1];
                for (int index = 0; index < num2; ++index)
                    this.Read(out value[index]);
            }
        }

        private void Read(out long[] value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = (long[])null;
            }
            else
            {
                int num2;
                value = new long[num2 = num1 - 1];
                for (int index = 0; index < num2; ++index)
                    this.Read(out value[index]);
            }
        }

        private void Read(out ulong[] value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = (ulong[])null;
            }
            else
            {
                int num2;
                value = new ulong[num2 = num1 - 1];
                for (int index = 0; index < num2; ++index)
                    this.Read(out value[index]);
            }
        }

        private void Read(out char[] value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = (char[])null;
            }
            else
            {
                int num2;
                value = new char[num2 = num1 - 1];
                for (int index = 0; index < num2; ++index)
                    this.Read(out value[index]);
            }
        }

        private void Read(out float[] value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = (float[])null;
            }
            else
            {
                int num2;
                value = new float[num2 = num1 - 1];
                for (int index = 0; index < num2; ++index)
                    this.Read(out value[index]);
            }
        }

        private void Read(out double[] value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = (double[])null;
            }
            else
            {
                int num2;
                value = new double[num2 = num1 - 1];
                for (int index = 0; index < num2; ++index)
                    this.Read(out value[index]);
            }
        }

        private void Read(out bool[] value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = (bool[])null;
            }
            else
            {
                int num2;
                value = new bool[num2 = num1 - 1];
                for (int index = 0; index < num2; ++index)
                    this.Read(out value[index]);
            }
        }

        private void Read(out string[] value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = (string[])null;
            }
            else
            {
                int num2;
                value = new string[num2 = num1 - 1];
                for (int index = 0; index < num2; ++index)
                    this.Read(out value[index]);
            }
        }

        private void Read(out Type[] value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = (Type[])null;
            }
            else
            {
                int num2;
                value = new Type[num2 = num1 - 1];
                for (int index = 0; index < num2; ++index)
                    this.Read(out value[index]);
            }
        }

        private void Read<T>(out T[] value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = (T[])null;
            }
            else
            {
                int num2;
                value = new T[num2 = num1 - 1];
                for (int index = 0; index < num2; ++index)
                    SerializeReaderHelperFixed<T, SerializeReaderFixed>.Deserialize(ref this, out value[index]);
            }
        }

        private void Read(out sbyte? value)
        {
            bool flag;
            this.Read(out flag);
            if (!flag)
            {
                value = new sbyte?();
            }
            else
            {
                sbyte num;
                this.Read(out num);
                value = new sbyte?(num);
            }
        }

        private void Read(out byte? value)
        {
            bool flag;
            this.Read(out flag);
            if (!flag)
            {
                value = new byte?();
            }
            else
            {
                byte num;
                this.Read(out num);
                value = new byte?(num);
            }
        }

        private void Read(out short? value)
        {
            bool flag;
            this.Read(out flag);
            if (!flag)
            {
                value = new short?();
            }
            else
            {
                short num;
                this.Read(out num);
                value = new short?(num);
            }
        }

        private void Read(out ushort? value)
        {
            bool flag;
            this.Read(out flag);
            if (!flag)
            {
                value = new ushort?();
            }
            else
            {
                ushort num;
                this.Read(out num);
                value = new ushort?(num);
            }
        }

        private void Read(out int? value)
        {
            bool flag;
            this.Read(out flag);
            if (!flag)
            {
                value = new int?();
            }
            else
            {
                int num;
                this.Read(out num);
                value = new int?(num);
            }
        }

        private void Read(out uint? value)
        {
            bool flag;
            this.Read(out flag);
            if (!flag)
            {
                value = new uint?();
            }
            else
            {
                uint num;
                this.Read(out num);
                value = new uint?(num);
            }
        }

        private void Read(out long? value)
        {
            bool flag;
            this.Read(out flag);
            if (!flag)
            {
                value = new long?();
            }
            else
            {
                long num;
                this.Read(out num);
                value = new long?(num);
            }
        }

        private void Read(out ulong? value)
        {
            bool flag;
            this.Read(out flag);
            if (!flag)
            {
                value = new ulong?();
            }
            else
            {
                ulong num;
                this.Read(out num);
                value = new ulong?(num);
            }
        }

        private void Read(out char? value)
        {
            bool flag;
            this.Read(out flag);
            if (!flag)
            {
                value = new char?();
            }
            else
            {
                char ch;
                this.Read(out ch);
                value = new char?(ch);
            }
        }

        private void Read(out float? value)
        {
            bool flag;
            this.Read(out flag);
            if (!flag)
            {
                value = new float?();
            }
            else
            {
                float num;
                this.Read(out num);
                value = new float?(num);
            }
        }

        private void Read(out double? value)
        {
            bool flag;
            this.Read(out flag);
            if (!flag)
            {
                value = new double?();
            }
            else
            {
                double num;
                this.Read(out num);
                value = new double?(num);
            }
        }

        private void Read(out bool? value)
        {
            bool flag1;
            this.Read(out flag1);
            if (!flag1)
            {
                value = new bool?();
            }
            else
            {
                bool flag2;
                this.Read(out flag2);
                value = new bool?(flag2);
            }
        }

        private void Read<T>(out T? value) where T : struct
        {
            bool flag;
            this.Read(out flag);
            if (!flag)
            {
                value = new T?();
            }
            else
            {
                T obj;
                SerializeReaderHelperFixed<T, SerializeReaderFixed>.Deserialize(ref this, out obj);
                value = new T?(obj);
            }
        }

        private void Read(out ArraySegment<byte> value)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                value = new ArraySegment<byte>();
            }
            else
            {
                int num2;
                value = new ArraySegment<byte>(this.data.Bytes.Array, this.position, num2 = num1 - 1);
                this.position += num2;
            }
        }

        private void Read<T>(out ArraySegment<T> value)
        {
            T[] array;
            this.Read<T>(out array);
            if (array == null)
                value = new ArraySegment<T>();
            else
                value = new ArraySegment<T>(array);
        }

        private void Read(out ICollection values)
        {
            object[] objArray;
            this.Read<object>(out objArray);
            values = (ICollection)objArray;
        }

        private void Read(out IDictionary values)
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                values = (IDictionary)null;
            }
            else
            {
                int num2;
                values = (IDictionary)new Hashtable(num2 = num1 - 1);
                for (int index = 0; index < num2; ++index)
                {
                    DictionaryEntry dictionaryEntry;
                    SerializeReaderHelperFixed<DictionaryEntry, SerializeReaderFixed>.Deserialize(ref this, out dictionaryEntry);
                    values.Add(dictionaryEntry.Key, dictionaryEntry.Value);
                }
            }
        }

        private void Read<T>(out ICollection<T> values)
        {
            T[] objArray;
            this.Read<T>(out objArray);
            values = (ICollection<T>)objArray;
        }

        private void Read<T>(out IList<T> values)
        {
            T[] objArray;
            this.Read<T>(out objArray);
            values = (IList<T>)objArray;
        }

        private void Read<TKey, TValue>(out IDictionary<TKey, TValue> values)
        {
            int num;
            this.ReadCount(out num);
            if (num == 0)
            {
                values = (IDictionary<TKey, TValue>)null;
            }
            else
            {
                int capacity = num - 1;
                values = (IDictionary<TKey, TValue>)new Dictionary<TKey, TValue>(capacity);
                for (int index = 0; index < capacity; ++index)
                {
                    KeyValuePair<TKey, TValue> keyValuePair;
                    SerializeReaderHelperFixed<KeyValuePair<TKey, TValue>, SerializeReaderFixed>.Deserialize(ref this, out keyValuePair);
                    values.Add(keyValuePair);
                }
            }
        }

        private void Read<T, TCollection>(out TCollection values) where TCollection : ICollection<T>, new()
        {
            int num1;
            this.ReadCount(out num1);
            if (num1 == 0)
            {
                values = default(TCollection);
            }
            else
            {
                int num2 = num1 - 1;
                values = new TCollection();
                for (int index = 0; index < num2; ++index)
                {
                    T obj;
                    SerializeReaderHelperFixed<T, SerializeReaderFixed>.Deserialize(ref this, out obj);
                    values.Add(obj);
                }
            }
        }

        internal sealed class UnknownType : Type
        {
            private Guid guid;

            public UnknownType(Guid guid)
            {
                this.guid = guid;
            }

            public override Assembly Assembly
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override string AssemblyQualifiedName
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override Type BaseType
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override string FullName
            {
                get
                {
                    return string.Format("{{{0}}}", (object)this.guid.ToString());
                }
            }

            public override Guid GUID
            {
                get
                {
                    return this.guid;
                }
            }

            protected override TypeAttributes GetAttributeFlagsImpl()
            {
                throw new NotImplementedException();
            }

            protected override ConstructorInfo GetConstructorImpl(
              BindingFlags bindingAttr,
              Binder binder,
              CallingConventions callConvention,
              Type[] types,
              ParameterModifier[] modifiers)
            {
                throw new NotImplementedException();
            }

            public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override Type GetElementType()
            {
                throw new NotImplementedException();
            }

            public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override EventInfo[] GetEvents(BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override FieldInfo GetField(string name, BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override FieldInfo[] GetFields(BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override Type GetInterface(string name, bool ignoreCase)
            {
                throw new NotImplementedException();
            }

            public override Type[] GetInterfaces()
            {
                throw new NotImplementedException();
            }

            public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            protected override MethodInfo GetMethodImpl(
              string name,
              BindingFlags bindingAttr,
              Binder binder,
              CallingConventions callConvention,
              Type[] types,
              ParameterModifier[] modifiers)
            {
                throw new NotImplementedException();
            }

            public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override Type GetNestedType(string name, BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override Type[] GetNestedTypes(BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
            {
                throw new NotImplementedException();
            }

            protected override PropertyInfo GetPropertyImpl(
              string name,
              BindingFlags bindingAttr,
              Binder binder,
              Type returnType,
              Type[] types,
              ParameterModifier[] modifiers)
            {
                throw new NotImplementedException();
            }

            protected override bool HasElementTypeImpl()
            {
                return false;
            }

            public override object InvokeMember(
              string name,
              BindingFlags invokeAttr,
              Binder binder,
              object target,
              object[] args,
              ParameterModifier[] modifiers,
              CultureInfo culture,
              string[] namedParameters)
            {
                throw new NotImplementedException();
            }

            protected override bool IsArrayImpl()
            {
                return false;
            }

            protected override bool IsByRefImpl()
            {
                return false;
            }

            protected override bool IsCOMObjectImpl()
            {
                return false;
            }

            protected override bool IsPointerImpl()
            {
                return false;
            }

            protected override bool IsPrimitiveImpl()
            {
                return false;
            }

            public override Module Module
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override string Namespace
            {
                get
                {
                    return "{UnknownNamespace}";
                }
            }

            public override Type UnderlyingSystemType
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override object[] GetCustomAttributes(bool inherit)
            {
                throw new NotImplementedException();
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                return false;
            }

            public override string Name
            {
                get
                {
                    return "{UnknownType}";
                }
            }
        }
    }
}
