using MongoDB.Bson.Serialization.Attributes;
using PacketCap;
using ServiceCore.CharacterServiceOperations;
using ServiceCore.EndPointNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketCap.Database
{
    [BsonIgnoreExtraElements]
    class Character
    {
        public long CID { get; set; }

        public int NexonSN { get; set; }

        public string CharacterID { get; set; }

        public int CharacterSN { get; set; }

        public BaseCharacter BaseCharacter { get; set; }

        public int Level { get; set; }

        public int Exp { get; set; }

        public int LevelUpExp { get; set; }

        public int Title { get; set; }

        public int TitleCount { get; set; }

        public CostumeInfo Costume { get; set; }

        public int DailyMicroPlayCount { get; set; }

        public int DailyFreePlayCount { get; set; }

        public string Quote { get; set; }

        public int TotalUsedAP { get; set; }

        public int GuildId { get; set; }

        public string GuildName { get; set; }

        public int CafeType { get; set; }

        public bool IsPremium { get; set; }

        public bool HasBonusEffect { get; set; }

        public bool IsReturn { get; set; }

        public bool IsDeleting { get; set; }

        public int DeleteWaitLeftSec { get; set; }

        public bool IsShouldNameChange { get; set; }

        public int VIPCode { get; set; }

        public VocationEnum VocationClass { get; set; }

        public int VocationLevel { get; set; }

        public int VocationExp { get; set; }

        public int VocationLevelUpExp { get; set; }

        public int VocationSkillPointAvailable { get; set; }

        public int FreeMatchWinCount { get; set; }

        public int FreeMatchLoseCount { get; set; }

        public PetStatusInfo Pet { get; set; }

        public bool IsEventJumping { get; set; }

        public string FreeTitleName { get; set; }

        public int Pattern { get; set; }

        public int ArenaWinCount { get; set; }

        public int ArenaLoseCount { get; set; }

        public int ArenaSuccessiveWinCount { get; set; }

        public DateTime Recorded;
        public Character(CharacterSummary c)
        {
            this.CID = c.CID;
            this.NexonSN = c.NexonSN;
            this.CharacterID = c.CharacterID;
            this.CharacterSN = c.CharacterSN;
            this.BaseCharacter = c.BaseCharacter;
            this.Level = c.Level;
            this.Exp = c.Exp;
            this.LevelUpExp = c.LevelUpExp;
            this.Title = c.Title;
            this.TitleCount = c.TitleCount;
            this.Costume = c.Costume;
            this.DailyMicroPlayCount = c.DailyMicroPlayCount;
            this.DailyFreePlayCount = c.DailyFreePlayCount;
            this.TotalUsedAP = c.TotalUsedAP;
            this.Quote = c.Quote;
            this.GuildId = c.GuildId;
            this.GuildName = c.GuildName;
            this.CafeType = c.CafeType;
            this.IsPremium = c.IsPremium;
            this.HasBonusEffect = c.HasBonusEffect;
            this.IsReturn = c.IsReturn;
            this.IsDeleting = c.IsDeleting;
            this.DeleteWaitLeftSec = c.DeleteWaitLeftSec;
            this.IsShouldNameChange = c.IsShouldNameChange;
            this.VIPCode = VIPCode;
            this.VocationClass = c.VocationClass;
            this.VocationExp = c.VocationExp;
            this.VocationLevel = c.VocationLevel;
            this.VocationLevelUpExp = c.VocationLevelUpExp;
            this.VocationSkillPointAvailable = c.VocationSkillPointAvailable;
            this.FreeMatchWinCount = c.FreeMatchWinCount;
            this.FreeMatchLoseCount = c.FreeMatchLoseCount;
            this.Pet = c.Pet;
            this.IsEventJumping = c.IsEventJumping;
            this.FreeTitleName = c.FreeTitleName;
            this.Pattern = c.Pattern;
            this.ArenaWinCount = c.ArenaWinCount;
            this.ArenaLoseCount = c.ArenaLoseCount;
            this.ArenaSuccessiveWinCount = c.ArenaSuccessiveWinCount;
            this.Recorded = DateTime.UtcNow;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Character:");

            string t = "\n\t";
            sb.Append(t);
            sb.Append("CharacterID=");
            sb.Append(CharacterID);
            
            sb.Append(t);
            sb.Append("BaseCharacter=");
            sb.Append(MessagePrinter.BaseCharacterToString(BaseCharacter));

            
            sb.Append(t);
            sb.Append("Level=");
            sb.Append(Level);
            
            sb.Append(t);
            sb.Append("Title=");
            sb.Append(Title.ToString());
            
            sb.Append(t);
            sb.Append("TitleCount=");
            sb.Append(TitleCount.ToString());
            

            String temp = MessagePrinter.CostumeInfoToString(Costume, 1, CharacterID.ToString(), "CostumeInfo");
            sb.Append("\n");
            sb.Append(temp);
            sb.Append(t);
            sb.Append("Quote=");
            sb.Append(Quote);
            
            sb.Append(t);
            sb.Append("GuildName=");
            sb.Append(GuildName);
            
            sb.Append(t);
            sb.Append("Vocation=");
            sb.Append(VocationClass.ToString());
            
            sb.Append(t);
            sb.Append("Pet: Name=");
            sb.Append(Pet.PetName);
            sb.Append(", Type=");
            sb.Append(Pet.PetType.ToString());
            
            sb.Append(t);
            sb.Append("FreeTitleName=");
            sb.Append(FreeTitleName);
            return sb.ToString();
        }
    }
}
