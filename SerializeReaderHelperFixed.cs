// Decompiled with JetBrains decompiler
// Type: Devcat.Core.Net.Message.SerializeReaderHelperFixed`2
// Assembly: Core, Version=1.2.5546.20396, Culture=neutral, PublicKeyToken=null
// MVID: B643316F-8FEA-46C6-ACBF-30466BA32C8C
// Assembly location: C:\Users\Anthony\VirtualBox VMs\Shared\vindictus\packetsearch\Bin\Core.dll

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace PacketCap
{
    internal static class SerializeReaderHelperFixed<T, R> where R : struct
    {
        private static DeserializeDelegate deserializeCore;
        private static DeserializeDelegate deserialize;
        [ThreadStatic]
        private static IDictionary<Type, DeserializeDelegate> deserializers;

        static SerializeReaderHelperFixed()
        {
            GenerateDeserializeMethod();
        }

        private static void DeserializeCore(ref R reader, out T value)
        {
            deserializeCore(ref reader, out value);
        }

        private static void Validate(Type type)
        {
            if (type == (Type)null)
                throw new ArgumentNullException(nameof(type));
            if (type.IsArray)
                Validate(type.GetElementType());
            else if (type.IsGenericType)
            {
                Type[] genericArguments = type.GetGenericArguments();
                int index = 0;
                for (int length = genericArguments.Length; index < length; ++index)
                    Validate(genericArguments[index]);
            }
            else if (type.GetType() == typeof(SerializeReaderFixed.UnknownType))
                throw new InvalidOperationException(string.Format("UnknownType {{ GUID = {0} }}", (object)type.GUID));
        }

        private static void DeserializeVirtual(ref R reader, out T value)
        {
            Type type;
            SerializeReaderHelperFixed<Type, R>.Deserialize(ref reader, out type);
            Validate(type);
            if (deserializers == null)
                deserializers = new Dictionary<Type, SerializeReaderHelperFixed<T, R>.DeserializeDelegate>();
            DeserializeDelegate deserializeDelegate;
            if (!deserializers.TryGetValue(type, out deserializeDelegate))
            {
                MethodInfo method = typeof(SerializeReaderHelperFixed<,>).MakeGenericType(type, typeof(R)).GetMethod("DeserializeCore", BindingFlags.Static | BindingFlags.NonPublic);
                DynamicMethod dynamicMethod = new DynamicMethod(string.Format("{0}.{1}.DeserializeAs[{2}.{3}]", (object)typeof(SerializeReaderHelperFixed<T, R>).Namespace, (object)typeof(SerializeReaderHelperFixed<T, R>).Name, (object)typeof(T).Namespace, (object)typeof(T).Name), (Type)null, new Type[2]
                {
          typeof (R).MakeByRefType(),
          typeof (T).MakeByRefType()
                }, typeof(SerializeReaderHelperFixed<T, R>), true);
                dynamicMethod.DefineParameter(1, ParameterAttributes.In | ParameterAttributes.Out, nameof(reader));
                dynamicMethod.DefineParameter(2, ParameterAttributes.Out, nameof(value));
                ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                LocalBuilder local = ilGenerator.DeclareLocal(type);
                ilGenerator.Emit(OpCodes.Ldloca_S, local);
                ilGenerator.EmitCall(OpCodes.Call, method, (Type[])null);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldloc, local);
                if (type.IsValueType)
                    ilGenerator.Emit(OpCodes.Box, type);
                ilGenerator.Emit(OpCodes.Stind_Ref);
                ilGenerator.Emit(OpCodes.Ret);
                deserializeDelegate = dynamicMethod.CreateDelegate(typeof(SerializeReaderHelperFixed<T, R>.DeserializeDelegate)) as SerializeReaderHelperFixed<T, R>.DeserializeDelegate;
                deserializers.Add(type, deserializeDelegate);
            }
            deserializeDelegate(ref reader, out value);
        }

        public static void Deserialize(ref R reader, out T value)
        {
            deserialize(ref reader, out value);
        }

        private static MethodInfo FindDefinedDeserializeMethod(Type type)
        {
            if (type.IsEnum)
                type = Enum.GetUnderlyingType(type);
            MethodInfo method = typeof(R).GetMethod("Read", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.ExactBinding, (Binder)null, new Type[1]
            {
        type.MakeByRefType()
            }, (ParameterModifier[])null);
            if (method != (MethodInfo)null)
                return method;
            if (type.IsArray)
            {
                MethodInfo methodInfo1 = ((IEnumerable<MethodInfo>)typeof(R).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)).Where<MethodInfo>((Func<MethodInfo, bool>)(info =>
                {
                    if (info.Name == "Read" && info.IsGenericMethodDefinition && (info.GetParameters().Length == 1 && info.GetParameters()[0].IsOut))
                        return info.GetParameters()[0].ParameterType.GetElementType().IsArray;
                    return false;
                })).SingleOrDefault<MethodInfo>();
                if (methodInfo1 != (MethodInfo)null)
                {
                    MethodInfo methodInfo2 = methodInfo1.MakeGenericMethod(type.GetElementType());
                    if (methodInfo2 != (MethodInfo)null)
                        return methodInfo2;
                }
            }
            IEnumerable<MethodInfo> source = ((IEnumerable<MethodInfo>)typeof(R).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)).Where<MethodInfo>((Func<MethodInfo, bool>)(info =>
            {
                if (info.Name == "Read" && info.IsGenericMethodDefinition && (info.GetParameters().Length == 1 && info.GetParameters()[0].IsOut))
                    return info.GetParameters()[0].ParameterType.GetElementType().IsGenericType;
                return false;
            }));
            if (type.IsGenericType)
            {
                MethodInfo methodInfo1 = source.Where<MethodInfo>((Func<MethodInfo, bool>)(info =>
                {
                    if (type.GetGenericTypeDefinition() == info.GetParameters()[0].ParameterType.GetElementType().GetGenericTypeDefinition())
                        return info.GetGenericArguments().Length == type.GetGenericArguments().Length;
                    return false;
                })).SingleOrDefault<MethodInfo>();
                if (methodInfo1 != (MethodInfo)null)
                {
                    MethodInfo methodInfo2 = methodInfo1.MakeGenericMethod(type.GetGenericArguments());
                    if (methodInfo2 != (MethodInfo)null)
                        return methodInfo2;
                }
            }
            IEnumerable<MethodInfo> methodInfos = ((IEnumerable<MethodInfo>)typeof(R).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)).Where<MethodInfo>((Func<MethodInfo, bool>)(info =>
            {
                if (info.Name == "Read" && info.IsGenericMethodDefinition && (info.GetParameters().Length == 1 && info.GetParameters()[0].IsOut))
                    return info.GetParameters()[0].ParameterType == ((IEnumerable<Type>)info.GetGenericArguments()).Last<Type>().MakeByRefType();
                return false;
            }));
            for (Type type1 = type; type1 != (Type)null; type1 = type1.BaseType)
            {
                foreach (MethodInfo methodInfo1 in methodInfos)
                {
                    Type[] genericArguments = type1.GetGenericArguments();
                    Type[] typeArray = new Type[genericArguments.Length + 1];
                    Array.Copy((Array)genericArguments, (Array)typeArray, genericArguments.Length);
                    typeArray[typeArray.Length - 1] = type;
                    if (typeArray.Length == methodInfo1.GetGenericArguments().Length)
                    {
                        try
                        {
                            MethodInfo methodInfo2 = methodInfo1.MakeGenericMethod(typeArray);
                            if (methodInfo2 != (MethodInfo)null)
                                return methodInfo2;
                        }
                        catch (ArgumentException ex)
                        {
                        }
                    }
                }
            }
            foreach (Type type1 in type.GetInterfaces())
            {
                foreach (MethodInfo methodInfo1 in methodInfos)
                {
                    Type[] genericArguments = type1.GetGenericArguments();
                    Type[] typeArray = new Type[genericArguments.Length + 1];
                    Array.Copy((Array)genericArguments, (Array)typeArray, genericArguments.Length);
                    typeArray[typeArray.Length - 1] = type;
                    if (typeArray.Length == methodInfo1.GetGenericArguments().Length)
                    {
                        try
                        {
                            MethodInfo methodInfo2 = methodInfo1.MakeGenericMethod(typeArray);
                            if (methodInfo2 != (MethodInfo)null)
                                return methodInfo2;
                        }
                        catch (ArgumentException ex)
                        {
                        }
                    }
                }
            }
            return (MethodInfo)null;
        }

        private static void EmitReadPredefinedType(ILGenerator il, MethodInfo methodInfo)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.EmitCall(OpCodes.Call, methodInfo, (Type[])null);
        }

        private static void EmitReadFields(ILGenerator il, Type type)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!field.IsInitOnly && !field.IsLiteral && !field.IsNotSerialized)
                {
                    Type fieldType = field.FieldType;
                    MethodInfo methodInfo = FindDefinedDeserializeMethod(fieldType);
                    if (methodInfo == (MethodInfo)null)
                        methodInfo = typeof(SerializeReaderHelperFixed<,>).MakeGenericType(fieldType, typeof(R)).GetMethod("Deserialize");
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    if (!type.IsValueType)
                        il.Emit(OpCodes.Ldind_Ref);
                    il.Emit(OpCodes.Ldflda, field);
                    il.EmitCall(OpCodes.Call, methodInfo, (Type[])null);
                }
            }
        }

        private static void GenerateDeserializeMethod()
        {
            DynamicMethod dynamicMethod = new DynamicMethod(string.Format("{0}.{1}.DeserializeCore[{2}.{3}]", (object)typeof(SerializeReaderHelperFixed<T, R>).Namespace, (object)typeof(SerializeReaderHelperFixed<T, R>).Name, (object)typeof(T).Namespace, (object)typeof(T).Name), (Type)null, new Type[2]
            {
        typeof (R).MakeByRefType(),
        typeof (T).MakeByRefType()
            }, typeof(SerializeReaderHelperFixed<T, R>), true);
            dynamicMethod.DefineParameter(1, ParameterAttributes.In | ParameterAttributes.Out, "reader");
            dynamicMethod.DefineParameter(2, ParameterAttributes.Out, "value");
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
            Type type = typeof(T);
            if (!type.IsSerializable && FindDefinedDeserializeMethod(type) == (MethodInfo)null)
                throw new SerializationException(string.Format("Type is not serializable: {0}", (object)type.AssemblyQualifiedName));
            Label? nullable = new Label?();
            if (type.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Initobj, type);
            }
            else if (FindDefinedDeserializeMethod(type) == (MethodInfo)null)
            {
                MethodInfo methodInfo = FindDefinedDeserializeMethod(typeof(bool));
                if (methodInfo == (MethodInfo)null)
                    methodInfo = typeof(SerializeReaderHelperFixed<,>).MakeGenericType(typeof(bool), typeof(R)).GetMethod("Deserialize");
                ilGenerator.Emit(OpCodes.Ldarg_0);
                LocalBuilder local = ilGenerator.DeclareLocal(typeof(bool));
                ilGenerator.Emit(OpCodes.Ldloca_S, local);
                ilGenerator.EmitCall(OpCodes.Call, methodInfo, (Type[])null);
                ilGenerator.Emit(OpCodes.Ldloc, local);
                nullable = new Label?(ilGenerator.DefineLabel());
                ilGenerator.Emit(OpCodes.Brfalse, nullable.Value);
                if (type.IsInterface || type.IsAbstract)
                {
                    ilGenerator.Emit(OpCodes.Ldstr, string.Format("Type cannot be properly initialized: {0}", (object)type.AssemblyQualifiedName));
                    ConstructorInfo constructor = typeof(SerializationException).GetConstructor(BindingFlags.Instance | BindingFlags.Public, (Binder)null, new Type[1]
                    {
            typeof (string)
                    }, (ParameterModifier[])null);
                    ilGenerator.Emit(OpCodes.Newobj, constructor);
                    ilGenerator.Emit(OpCodes.Throw);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    ConstructorInfo constructor = type.GetConstructor(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, (Binder)null, Type.EmptyTypes, (ParameterModifier[])null);
                    if (constructor != (ConstructorInfo)null)
                    {
                        ilGenerator.Emit(OpCodes.Newobj, constructor);
                    }
                    else
                    {
                        MethodInfo method1 = typeof(RuntimeTypeHandle).GetMethod("Allocate", BindingFlags.Static | BindingFlags.NonPublic);
                        MethodInfo method2 = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public);
                        ilGenerator.Emit(OpCodes.Ldtoken, type);
                        ilGenerator.Emit(OpCodes.Call, method2);
                        ilGenerator.EmitCall(OpCodes.Call, method1, (Type[])null);
                    }
                    ilGenerator.Emit(OpCodes.Stind_Ref);
                }
            }
            Stack<Type> typeStack = new Stack<Type>();
            for (; type != (Type)null; type = type.BaseType)
            {
                MethodInfo deserializeMethod = FindDefinedDeserializeMethod(type);
                if (deserializeMethod != (MethodInfo)null)
                {
                    EmitReadPredefinedType(ilGenerator, deserializeMethod);
                    break;
                }
                if (type.IsSerializable)
                    typeStack.Push(type);
            }
            while (0 < typeStack.Count)
                EmitReadFields(ilGenerator, typeStack.Pop());
            if (nullable.HasValue)
                ilGenerator.MarkLabel(nullable.Value);
            ilGenerator.Emit(OpCodes.Ret);
            deserializeCore = dynamicMethod.CreateDelegate(typeof(DeserializeDelegate)) as DeserializeDelegate;
            if (typeof(T).IsSealed || FindDefinedDeserializeMethod(typeof(T)) != (MethodInfo)null)
                deserialize = deserializeCore;
            else
                deserialize = new DeserializeDelegate(DeserializeVirtual);
        }

        private delegate void DeserializeDelegate(ref R reader, out T value);
    }
}