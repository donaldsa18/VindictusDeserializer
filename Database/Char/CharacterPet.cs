using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ServiceCore.EndPointNetwork;

namespace PacketCap.Database.Char
{
    [BsonIgnoreExtraElements]
    class CharacterPet
    {

        public string CharacterID;
        public long PetID { get; set; }
        public string PetName { get; set; }
        public int PetType { get; set; }
        public int Slot { get; set; }
        public int Level { get; set; }
        public int Exp { get; set; }
        public int Desire { get; set; }
        public BsonDocument Stat { get; set; }
        public BsonArray Skills { get; set; }
        public BsonArray Accessories { get; set; }
        public byte PetStatus { get; set; }
        public int RemainActiveTime { get; set; }
        public DateTime RemainExpiredTime { get; set; }

        public CharacterPet(PetStatusInfo p,string name) {
            this.CharacterID = name;
            this.PetID = p.PetID;
            this.PetName = p.PetName;
            this.PetType = p.PetType;
            this.Slot = p.Slot;
            this.Level = p.Level;
            this.Exp = p.Exp;
            this.Desire = p.Desire;

            this.Stat = PetStatElementToBson(p.Stat);
            this.Skills = PetSkillElementListToBson(p.Skills);
            this.Accessories = PetAccessoryElementListToBson(p.Accessories);
            
            this.PetStatus = p.PetStatus;
            this.RemainActiveTime = p.RemainActiveTime;
            this.RemainExpiredTime = p.RemainExpiredTime;
        }

        private BsonArray PetAccessoryElementListToBson(List<PetAccessoryElement> list)
        {
            BsonArray arr = new BsonArray();
            foreach (PetAccessoryElement p in list)
            {
                arr.Add(PetAccessoryElementToBson(p));
            }
            return arr;
        }

        private BsonDocument PetAccessoryElementToBson(PetAccessoryElement p) {
            BsonDocument doc = new BsonDocument();
            doc.Add("ItemClass", p.ItemClass);
            doc.Add("SlotOrder", p.SlotOrder);
            doc.Add("AccessorySize", p.AccessorySize);
            doc.Add("RemainingTime", p.RemainingTime);
            return doc;
        }


        private BsonDocument PetStatElementToBson(PetStatElement p) {
            BsonDocument doc = new BsonDocument();
            doc.Add("RequiredExp", p.RequiredExp);
            doc.Add("MaxExp", p.MaxExp);
            doc.Add("Hp", p.Hp);
            doc.Add("ResDamage", p.ResDamage);
            doc.Add("HpRecovery", p.HpRecovery);
            doc.Add("DefBreak", p.DefBreak);
            doc.Add("AtkBalance", p.AtkBalance);
            doc.Add("Atk", p.Atk);
            doc.Add("Def", p.Def);
            doc.Add("Critical", p.Critical);
            doc.Add("ResCritical", p.ResCritical);
            return doc;
        }

        private BsonArray PetSkillElementListToBson(List<PetSkillElement> list)
        {
            BsonArray arr = new BsonArray();
            foreach (PetSkillElement p in list) {
                arr.Add(PetSkillElementToBson(p));
            }
            return arr;
        }
        private BsonDocument PetSkillElementToBson(PetSkillElement p)
        {
            BsonDocument doc = new BsonDocument();
            doc.Add("SkillID",p.SkillID);
            doc.Add("SlotOrder",p.SlotOrder);
            doc.Add("OpenLevel",p.OpenLevel);
            doc.Add("HasExpireDateTimeInfo", p.HasExpireDateTimeInfo);
            doc.Add("ExpireDateTimeDiff",p.ExpireDateTimeDiff);

            return doc;
        }
    }
}
