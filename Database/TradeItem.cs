using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PacketCap;
using ServiceCore.CharacterServiceOperations;
using ServiceCore.EndPointNetwork;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public BsonDocument Attributes;

        public TradeItem(long TID) {
            this.TID = TID;
        }

        private void ParseAttribute(string attribute,string itemClass) {
            //Console.WriteLine("Parse Attribute");
            Dictionary<string, double> calculatedStats = new Dictionary<string, double>();
            BsonDocument Stat = new BsonDocument();
            bool isEquip = SQLiteConnect.getEquipItemStats(itemClass, calculatedStats, out string qualityType, out string enhanceType);
            int ability = -1;
            if (!isEquip) {
                if (attribute.Contains("VARIABLESTAT")) {
                    ApplyVariableStat(attribute, calculatedStats, itemClass, out ability);
                }
            }
            else {
                if (attribute.Contains("COMBINATION")) {
                    ApplyCombination(attribute, calculatedStats, out qualityType, out enhanceType);
                }
                if (attribute.Contains("GEMSTONEINFO")) {
                    ApplyGemstone(attribute, calculatedStats);
                }
                if (attribute.Contains("QUALITY")) {
                    ApplyQuality(attribute, calculatedStats, qualityType);
                }
                if (attribute.Contains("ENHANCE")) {
                    ApplyEnhance(attribute, calculatedStats, enhanceType);
                }
                if (attribute.Contains("SPIRIT_INJECTION")) {
                    ApplySpiritInjection(attribute, calculatedStats);
                }
                if (attribute.Contains("PREFIX")) {
                    ApplyEnchantType("PREFIX", attribute, calculatedStats);
                }
                if (attribute.Contains("SUFFIX")) {
                    ApplyEnchantType("SUFFIX", attribute, calculatedStats);
                }
            }
            if (calculatedStats.Count != 0) {
                SetStats(Stat, calculatedStats);
                SetAbility(ability, Stat);
                Attributes.Add("Stat", Stat);
                string statStr = GetStatString(calculatedStats);
                Attributes.Add("StatStr", statStr);
            }
            if (attribute.Contains("ANTIBIND")) {
                SetAntiBind(attribute);
            }
        }

        private void SetAbility(int ability, BsonDocument Stat) {
            if (ability == -1)
            {
                return;
            }
            string abilityClass = SQLiteConnect.GetAbilityClass(ability);
            string abilityDesc = MongoDBConnect.connection.GetAbilityString(abilityClass);
            Stat.Add("Ability", abilityDesc);
        }

        private void SetAntiBind(string attribute)
        {
            MatchCollection mc = Regex.Matches(attribute, @"ANTIBIND:([0-9]+)");
            foreach (Match m in mc)
            {
                int antibind = int.Parse(m.Groups[1].ToString());
                Attributes.Add("AntiBind", antibind);
                return;
            }
        }
        private void SetStats(BsonDocument doc, Dictionary<string, double> stats) {
            Console.WriteLine("Set Stats");
            foreach (KeyValuePair<string, double> entry in stats)
            {
                string stat = entry.Key;
                if (SQLiteConnect.statToReadableStat.TryGetValue(stat,out string readableStat)) {
                    stat = readableStat;
                }
                doc.Add(stat, entry.Value);
            }
        }

        private void ApplyEnchantType(string type, string attribute, Dictionary<string, double> calculatedStats)
        {
            Console.WriteLine("Apply Enchant");
            BsonDocument doc = new BsonDocument();
            MatchCollection mc = Regex.Matches(attribute, type+@":([a-z_0-9]+)");
            foreach (Match m in mc)
            {
                string enchant = m.Groups[1].ToString();
                Dictionary<string, double> stats = new Dictionary<string, double>();
                SQLiteConnect.ApplyEnchantStats(enchant, stats, out string buff);
                SetStats(doc, stats);
                doc.Add("Enchant", enchant);
                if (buff.Length != 0) {
                    doc.Add("Buff",buff);
                }
                CombineStats(calculatedStats, stats);
            }
            Attributes.Add(type, doc);
        }

        private static string GetStatString(Dictionary<string, double> calculatedStats) {
            StringBuilder sb = new StringBuilder();
            string[] stats = { "ATK","MATK", "PVP_ATK", "PVP_MATK", "Balance","Critical","ATK_Speed","ATK_Absolute","DEF", "PVP_DEF", "STR","DEX","INT","WILL","LUCK","HP","STAMINA","Res_Critical","TOWN_SPEED","ATK_LimitOver"};
            foreach (string stat in stats) {
                if (calculatedStats.TryGetValue(stat, out double val)) {
                    string readableStat = stat;
                    if (SQLiteConnect.statToReadableStat.ContainsKey(stat)) {
                        readableStat = SQLiteConnect.statToReadableStat[stat];
                    }
                    int calculatedStat = (int)val;
                    sb.Append(" ");
                    sb.Append(readableStat);
                    sb.Append(calculatedStat.ToString("+0;-#"));
                }
            }
            if (sb.Length > 1) {
                sb.Remove(0, 1);
            }

            return sb.ToString();
        }

        private void ApplyVariableStat(string attribute, Dictionary<string, double> calculatedStats,string itemClass, out int ability) {
            Console.WriteLine("Apply Variable Stat");
            CombineStats(calculatedStats, SQLiteConnect.classCombineStats[itemClass]);
            MatchCollection mc = Regex.Matches(attribute, @"VARIABLESTAT:([0-9;]+)");
            ability = -1;
            foreach (Match m in mc)
            {
                string statDiff = m.Groups[1].ToString();
                ApplyStats(statDiff, calculatedStats);
            }
            mc = Regex.Matches(attribute, @"VARIABLESTAT:[0-9;]+\(([0-9]+)\)");
            foreach (Match m in mc)
            {
                int.TryParse(m.Groups[1].ToString(), out int abilityClassID);
                Console.WriteLine("Found an ability {0}",abilityClassID);
                ability = abilityClassID;
            }
            SQLiteConnect.GetEnhanceQualityEnchant(itemClass, out string enhanceType, out string qualityType, out int enchantMaxLevel);
            SetMaxEnhanceString(enhanceType, qualityType, enchantMaxLevel);
        }

        private void ApplyCombination(string attribute, Dictionary<string, double> calculatedStats, out string qualityType, out string enhanceType) {
            Console.WriteLine("Apply Combination");
            string splitAttributes = attribute.Replace(",", "\n");
            MatchCollection mc = Regex.Matches(splitAttributes, @"PS_([0-9]+):([0-9;]+)\(([0-9]+)\)<?([0-9]*)>?");
            qualityType = "";
            enhanceType = "";
            BsonArray Composite = new BsonArray();
            int enchantMaxLevel = 100;
            int rarity = 100;
            foreach (Match m in mc)
            {
                string statDiff = m.Groups[2].ToString();
                int combinePartID = int.Parse(m.Groups[3].ToString());
                Dictionary<string, double> stats = new Dictionary<string, double>();
                CombineStats(stats,SQLiteConnect.combineStats[combinePartID]);
                ApplyStats(statDiff, stats);

                int.TryParse(m.Groups[4].ToString(), out int abilityID);
                string itemClass = SQLiteConnect.idToItemClass[combinePartID];
                if (abilityID != 0)
                {
                    AddComposite(stats, itemClass, Composite, abilityID);
                }
                else
                {
                    AddComposite(stats, itemClass, Composite);
                }
                CombineStats(calculatedStats, stats);
                int newEnchantLevel = SQLiteConnect.idToEnchantMaxLevel[combinePartID];
                enchantMaxLevel = newEnchantLevel < enchantMaxLevel ? newEnchantLevel : enchantMaxLevel;
                qualityType = GetLowerType(SQLiteConnect.idToQualityType[combinePartID], qualityType);
                enhanceType = GetLowerType(SQLiteConnect.idToEnhanceType[combinePartID], enhanceType);
                MatchCollection mc2 = Regex.Matches(itemClass, @"_([0-9])_");
                foreach (Match m2 in mc2) {
                    int newRarity = int.Parse(m2.Groups[1].ToString());
                    rarity = newRarity < rarity ? newRarity : rarity;
                }
            }
            Attributes.Add("Composite", Composite);
            Attributes.Add("Rarity",rarity);
            SetMaxEnhanceString(enhanceType, qualityType, enchantMaxLevel);
        }

        private void SetMaxEnhanceString(string enhanceType, string qualityType, int enchantMaxLevel) {
            StringBuilder sb = new StringBuilder();
            sb.Append("Max Enhance Level ");
            sb.Append(SQLiteConnect.GetMaxEnhance(enhanceType));
            sb.Append(", Max Quality ");
            sb.Append(SQLiteConnect.GetMaxQuality(qualityType));
            sb.Append(" Star, Max Enchant Rank ");
            sb.Append(SQLiteConnect.EnchantNumToLevel(enchantMaxLevel));

            Attributes.Add("MaxEnhance", sb.ToString());
        }

        private void ApplySpiritInjection(string attribute, Dictionary<string, double> calculatedStats) {
            Console.WriteLine("Apply Spirit Injection");
            MatchCollection mc = Regex.Matches(attribute, @"SPIRIT_INJECTION:([^\(]+)\(([^)]+)\)");
            foreach (Match m in mc)
            {
                string stat = m.Groups[1].ToString();
                int modifier = int.Parse(m.Groups[2].ToString());
                if (calculatedStats.TryGetValue(stat, out double val))
                {
                    calculatedStats[stat] = val + modifier;
                }
                else
                {
                    calculatedStats[stat] = modifier;
                }
                if (SQLiteConnect.statToReadableStat.ContainsKey(stat)) {
                    stat = SQLiteConnect.statToReadableStat[stat];
                }
                string modifierStr = stat + modifier.ToString("+0;-#");
                Attributes.Add("SpiritInjection", modifierStr);
            }
        }

        private void ApplyGemstone(string attribute,Dictionary<string,double> calculatedStats) {
            Console.WriteLine("Apply Gemstone");
            string splitAttributes = attribute.Replace(",", "\n");
            MatchCollection mc = Regex.Matches(splitAttributes, @"GS_(\d):([0-9;]+)\(([0-9]+)\)<([0-9]+)>");
            BsonArray Composite = new BsonArray();
            Dictionary<string, Dictionary<string, double>> savedStats = new Dictionary<string, Dictionary<string, double>>();
            int lowestRank = 5;
            foreach (Match m in mc)
            {
                string statDiff = m.Groups[2].ToString();
                int gemstoneID = int.Parse(m.Groups[4].ToString());
                Dictionary<string, double> stats = new Dictionary<string, double>();
                CombineStats(stats,SQLiteConnect.gemstoneStats[gemstoneID]);
                ApplyStats(statDiff, stats);
                string itemClass = SQLiteConnect.idToGemstone[gemstoneID];
                savedStats[itemClass] = stats;
                CombineStats(calculatedStats, stats);
                int rank = SQLiteConnect.idToGemstoneRank[gemstoneID];
                lowestRank = rank < lowestRank ? rank : lowestRank;
            }

            MatchCollection mc2 = Regex.Matches(attribute, @"GEMSTONEINFO:([0-9;]+)");
            string[] gemOrder = { "diamond", "sapphire", "ruby", "emerald" };
            string[] gems = new string[4];
            foreach (Match m in mc2) {
                string[] gemIDs = m.Groups[1].ToString().Split(';');
                for (int i = 0; i < gemIDs.Length; i+=2) {
                    int gemRank = int.Parse(gemIDs[i]);
                    string gem = gemOrder[gemRank];
                    gems[gemRank] = gem;
                }
            }

            foreach (string gem in gems) {
                if (gem == null) {
                    continue;
                }
                bool foundGem = false;
                foreach (var entry in savedStats) {
                    if (entry.Key.Contains(gem)) {
                        AddComposite(entry.Value, entry.Key, Composite);
                        foundGem = true;
                        break;
                    }
                }
                if (!foundGem) {
                    BsonDocument compositeStat = new BsonDocument();
                    string icon = string.Format("gemstone_{0}_rank{1}",gem,lowestRank);
                    compositeStat.Add("Icon", icon);
                    string message = string.Format("{0}{1}s will fit.", gem.First().ToString().ToUpper(), gem.Substring(1));
                    compositeStat.Add("Message", message);
                    Composite.Add(compositeStat);
                }
            }
            
            Attributes.Add("Composite", Composite);
        }

        private static void CombineStats(Dictionary<string,double> calculatedStats, Dictionary<string, double> stats) {
            Console.WriteLine("Combine Stats");
            foreach (KeyValuePair<string, double> entry in stats)
            {
                if (calculatedStats.TryGetValue(entry.Key, out double val))
                {
                    calculatedStats[entry.Key] = val + entry.Value;
                }
                else
                {
                    calculatedStats[entry.Key] = entry.Value;
                }
            }
        }

        private void AddComposite(Dictionary<string, double> stats, string itemClass, BsonArray Composite, int ability)
        {
            Console.WriteLine("Add Composite");
            BsonDocument compositeStat = new BsonDocument();
            SetStats(compositeStat, stats);
            compositeStat.Add("ItemClass", itemClass);
            string abilityClass = SQLiteConnect.GetAbilityClass(ability);
            string abilityDesc = MongoDBConnect.connection.GetAbilityString(abilityClass);
            string icon = SQLiteConnect.GetIcon(itemClass);
            compositeStat.Add("Icon", icon);
            compositeStat.Add("Ability", abilityDesc);
            string statStr = GetStatString(stats);
            compositeStat.Add("StatStr", statStr);
            Composite.Add(compositeStat);
        }

        private void AddComposite(Dictionary<string,double> stats,string itemClass,BsonArray Composite) {
            Console.WriteLine("Add Composite");
            BsonDocument compositeStat = new BsonDocument();
            SetStats(compositeStat, stats);
            compositeStat.Add("ItemClass", itemClass);
            string icon = SQLiteConnect.GetIcon(itemClass);
            compositeStat.Add("Icon", icon);
            string statStr = GetStatString(stats);
            compositeStat.Add("StatStr", statStr);
            Composite.Add(compositeStat);
        }

        private static string GetLowerType(string q1, string q2)
        {
            if (q1 == q2)
            {
                return q1;
            }
            int q1Num = GetTypeNum(q1);
            int q2Num = GetTypeNum(q2);
            if (q1Num < q2Num) {
                return q1;
            }
            return q2;
        }
        private static int GetTypeNum(string type) {
            MatchCollection mc = Regex.Matches(type, @"_([0-9]+)$");
            foreach (Match m in mc)
            {
                return int.Parse(m.Groups[1].ToString());
            }
            return 100;
        }

        private static void ApplyEnhance(string attribute, Dictionary<string, double> stats, string enhanceType) {
            Console.WriteLine("Apply Enhance");
            MatchCollection mc = Regex.Matches(attribute, @"ENHANCE:([0-9]+)");
            foreach (Match m in mc)
            {
                int enhance = int.Parse(m.Groups[1].ToString());
                SQLiteConnect.ApplyEnhanceStats(enhance, enhanceType, stats);
            }
        }

        private static void ApplyQuality(string attribute, Dictionary<string, double> stats,string qualityType) {
            Console.WriteLine("Apply Quality");
            MatchCollection mc = Regex.Matches(attribute, @"QUALITY:\(([0-9]+)\)");
            foreach (Match m in mc)
            {
                int quality =  int.Parse(m.Groups[1].ToString());
                SQLiteConnect.ApplyQualityMultiplier(quality, qualityType, stats);
            }
        }

        private static string StatIntToString(int stat) {
            return ((Stats)stat).ToString();
        }

        private static void ApplyStats(String diffStr, Dictionary<string, double> stats) {
            Console.WriteLine("Apply Stats {0}",diffStr);
            if (diffStr == null || diffStr.Length == 0) {
                return;
            }
            string[] diffStrArr = diffStr.Split(';');
            for (int i = 0; i+1 < diffStrArr.Length; i += 2) {
                int statNum = int.Parse(diffStrArr[i]);
                if (statNum < 0 || statNum > 23) {
                    Console.WriteLine("Stat out of range");
                    continue;
                }
                string stat = StatIntToString(statNum);
                double val = double.Parse(diffStrArr[i + 1]);
                if (stats.TryGetValue(stat, out double oldVal))
                {
                    stats[stat] = oldVal + val;
                }
                else {
                    stats[stat] = val;
                }
            }
        }

        private void AddColor(string name, int colorNum) {
            string color = MessagePrinter.IntToRGB(colorNum);
            if (color.Length != 0) {
                Attributes.Add(name, color);
            }
        }

        public TradeItem(TradeItemInfo item) {
            Attributes = new BsonDocument();
            TID = item.TID;
            CID = item.CID;
            CharacterName = item.ChracterName;
            ItemName = item.ItemClass;//TODO: translate to user friendly name
            Quantity = item.ItemCount;
            Price = item.ItemPrice;
            Attribute = item.AttributeEX;
            AddColor("Color1", item.color1);
            AddColor("Color2", item.color2);
            AddColor("Color3", item.color3);

            Expire = MessagePrinter.CloseDateToDateTime(item.CloseDate);
            Console.WriteLine("Parsing {0}", item.ItemClass);
            
            
            ParseAttribute(item.AttributeEX, item.ItemClass);
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
            Console.WriteLine("Parsed {0}",item.ItemClass);
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
            return sb.ToString();
        }
    }
}
