// Decompiled with JetBrains decompiler
// Type: ServiceCore.EndPointNetwork.TradeItemInfo
// Assembly: ServiceCore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 684923D5-4DD3-4D7C-B4F7-A6A443C0EAC2
// Assembly location: C:\Users\Anthony\VirtualBox VMs\Shared\vindictus\packetsearch\Bin\ServiceCore.dll

using System;

namespace ServiceCore.EndPointNetwork
{
  [Serializable]
  public sealed class TradeItemInfo : IMessage
  {
    public long TID { get; set; }

    public long CID { get; set; }

    public string ChracterName { get; set; }

    public string ItemClass { get; set; }

    public long ItemCount { get; set; }

    public long ItemPrice { get; set; }

    public int CloseDate { get; set; }

    public bool HasAttribute
    {
      get
      {
        return this.AttributeEX != null;
      }
    }

    public string AttributeEX { get; set; }

    public int MaxArmorCondition { get; set; }

    public int color1 { get; set; }

    public int color2 { get; set; }

    public int color3 { get; set; }

    public int MinPrice { get; set; }

    public int MaxPrice { get; set; }

    public int AvgPrice { get; set; }

    public byte TradeType { get; set; }

    public override string ToString()
    {
      return "" + "TID " + (object) this.TID + "\n" + "CID " + (object) this.CID + "\n" + "CharacterName " + this.ChracterName + "\n" + "ItemClass " + this.ItemClass + "\n" + "ItemCount " + (object) this.ItemCount + "\n" + "ItemPrice " + (object) this.ItemPrice + "\n" + "CloseDate " + (object) this.CloseDate + "\n" + "HasAttribute " + (object) this.HasAttribute + "\n" + "AttributeEX " + this.AttributeEX + "\n" + "MaxArmorCondition " + (object) this.MaxArmorCondition + "\n" + "ArmorCondition " + (object) this.MaxArmorCondition + "\n" + "color1 " + (object) this.color1 + "\n" + "color2 " + (object) this.color2 + "\n" + "color3 " + (object) this.color3 + "\n" + "MinPrice " + (object) this.MinPrice + "\n" + "MaxPrice " + (object) this.MaxPrice + "\n" + "AvgPrice " + (object) this.AvgPrice + "\n" + "TradeType " + (object) this.TradeType + "\n";
    }
  }
}
