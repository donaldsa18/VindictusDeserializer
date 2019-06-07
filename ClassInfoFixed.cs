// Decompiled with JetBrains decompiler
// Type: Devcat.Core.Net.Message.ClassInfo`1
// Assembly: Core, Version=1.2.5546.20396, Culture=neutral, PublicKeyToken=null
// MVID: B643316F-8FEA-46C6-ACBF-30466BA32C8C
// Assembly location: C:\Users\Anthony\VirtualBox VMs\Shared\vindictus\packetsearch\Bin\Core.dll

using System;

namespace PacketCap
{
    internal static class ClassInfoFixed<T>
    {
        private static int categoryId;

        public static int CategoryId
        {
            get
            {
                return ClassInfoFixed<T>.categoryId;
            }
            set
            {
                if (ClassInfoFixed<T>.categoryId != value && ClassInfoFixed<T>.categoryId != 0)
                    throw new InvalidOperationException(string.Format("CategoryId is already set. T = {0}", (object)typeof(T).AssemblyQualifiedName));
                ClassInfoFixed<T>.categoryId = value;
            }
        }
    }
}
