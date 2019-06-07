// Decompiled with JetBrains decompiler
// Type: ServiceCore.EndPointNetwork.TradeSearchResult
// Assembly: ServiceCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 684923D5-4DD3-4D7C-B4F7-A6A443C0EAC2
// Assembly location: C:\Users\Anthony\VirtualBox VMs\Shared\vindictus\packetsearch\Bin\ServiceCore.dll

using System;
using System.Collections.Generic;

namespace ServiceCore.EndPointNetwork
{
  [Serializable]
  public sealed class TradeSearchResult : IMessage
  {
    public ICollection<TradeItemInfo> TradeItemList { get; set; }

    public int UniqueNumber { get; set; }

    public bool IsMoreResult { get; set; }

    public int result { get; set; }
  }
}
