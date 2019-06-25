using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PacketCap;
using ServiceCore.EndPointNetwork;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketCap.Database
{
    [BsonIgnoreExtraElements]
    class TradeItem
    {
        public long TID;

        public long CID;
        public string CharacterName;

        public string ItemName;
        public long Quantity;
        public long Price;

        public DateTime Expire;
        public DateTime Listed;

        public string Attribute;
        public int MaxDurability;

        public int Color1;
        public int Color2;
        public int Color3;



        public TradeItem(long TID) {
            this.TID = TID;
        }

        public TradeItem(TradeItemInfo item) {
            TID = item.TID;
            CID = item.CID;
            CharacterName = item.ChracterName;
            ItemName = item.ItemClass;//TODO: translate to user friendly name
            Quantity = item.ItemCount;
            Price = item.ItemPrice;
            Attribute = item.AttributeEX;
            MaxDurability = item.MaxArmorCondition;
            Color1 = item.color1;
            Color2 = item.color2;
            Color3 = item.color3;

            Expire = MessagePrinter.CloseDateToDateTime(item.CloseDate);

            if (item.CloseDate > DaysToSeconds(10))
            {
                Listed = DateTime.UtcNow;
            }
            else {
                int[] days = { 1,3,7,10};

                foreach (int day in days)
                {
                    int seconds = DaysToSeconds(day);
                    if (item.CloseDate <= seconds) {
                        Listed = DateTime.UtcNow.AddSeconds(item.CloseDate - seconds);
                        break;
                    }
                }
            }
        }
        private int DaysToSeconds(int days) {
            return days * 24 * 60 * 60;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("TradeItem:\n\tTID=");
            sb.Append(TID);
            sb.Append("\n\tCID=");
            sb.Append(CID);
            sb.Append("\n\tCharacterName=");
            sb.Append(CharacterName);
            sb.Append("\n\tItemName=");
            sb.Append(ItemName);
            sb.Append("\n\tQuantity=");
            sb.Append(Quantity);
            sb.Append("\n\tPrice=");
            sb.Append(Price.ToString("#,##0"));
            sb.Append("\n\tExpire=");
            sb.Append(Expire.ToString("yyyy-MM-dd hh:mm:ss.fff"));
            sb.Append("\n\tListed=");
            sb.Append(Listed.ToString("yyyy-MM-dd hh:mm:ss.fff"));
            if (Attribute!= null && Attribute.Length != 0) {
                sb.Append("\n\tAttribute=");
                sb.Append(Attribute);
            }
            
            sb.Append("\n\tMaxDurability=");
            sb.Append(MaxDurability);
            sb.Append("\n\tColor1=");
            sb.Append(Color.FromArgb(Color1).ToString().Replace("Color ",""));
            sb.Append("\n\tColor2=");
            sb.Append(Color.FromArgb(Color2).ToString().Replace("Color ", ""));
            sb.Append("\n\tColor3=");
            sb.Append(Color.FromArgb(Color3).ToString().Replace("Color ", ""));
            return sb.ToString();
        }
    }
}
