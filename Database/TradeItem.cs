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
            if (!isEquip) {
                if (attribute.Contains("VARIABLESTAT")) {
                    ApplyVariableStat(attribute, calculatedStats, itemClass);
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
                Attributes.Add("Stat", Stat);
            }
            if (attribute.Contains("ANTIBIND")) {
                SetAntiBind(attribute);
            }
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

        private void ApplyVariableStat(string attribute, Dictionary<string, double> calculatedStats,string itemClass) {
            Console.WriteLine("Apply Variable Stat");
            CombineStats(calculatedStats, SQLiteConnect.classCombineStats[itemClass]);
            MatchCollection mc = Regex.Matches(attribute, @"VARIABLESTAT:([0-9;]+)");
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
                calculatedStats["Ability"] = (double)abilityClassID;
            }
        }

        private void ApplyCombination(string attribute, Dictionary<string, double> calculatedStats, out string qualityType, out string enhanceType) {
            Console.WriteLine("Apply Combination");
            string splitAttributes = attribute.Replace(",", "\n");
            MatchCollection mc = Regex.Matches(splitAttributes, @"PS_([0-9]+):([0-9;]+)\(([0-9]+)\)");
            qualityType = "";
            enhanceType = "";
            BsonArray Composite = new BsonArray();
            
            foreach (Match m in mc)
            {
                string statDiff = m.Groups[2].ToString();
                int combinePartID = int.Parse(m.Groups[3].ToString());
                Dictionary<string, double> stats = new Dictionary<string, double>();
                CombineStats(stats,SQLiteConnect.combineStats[combinePartID]);
                ApplyStats(statDiff, stats);
                AddComposite(stats, SQLiteConnect.idToItemClass[combinePartID],Composite);
                CombineStats(calculatedStats, stats);
                qualityType = getLowerType(SQLiteConnect.idToQualityType[combinePartID], qualityType);
                enhanceType = getLowerType(SQLiteConnect.idToEnhanceType[combinePartID], enhanceType);
            }
            Attributes.Add("Composite", Composite);
        }

        private void ApplySpiritInjection(string attribute, Dictionary<string, double> calculatedStats) {
            Console.WriteLine("Apply Spirit Injection");
            MatchCollection mc = Regex.Matches(attribute, @"SPIRIT_INJECTION:([^\(]+)\(([0-9]+)\)");
            foreach (Match m in mc)
            {
                string stat = m.Groups[1].ToString();
                double modifier = double.Parse(m.Groups[2].ToString());
                if (calculatedStats.TryGetValue(stat, out double val))
                {
                    calculatedStats[stat] = val + modifier;
                }
                else
                {
                    calculatedStats[stat] = modifier;
                }
            }
        }

        private void ApplyGemstone(string attribute,Dictionary<string,double> calculatedStats) {
            Console.WriteLine("Apply Gemstone");
            string splitAttributes = attribute.Replace(",", "\n");
            MatchCollection mc = Regex.Matches(splitAttributes, @"GS_(\d):([0-9;]+)\(([0-9]+)\)<([0-9]+)>");
            BsonArray Composite = new BsonArray();
            foreach (Match m in mc)
            {
                string statDiff = m.Groups[2].ToString();
                int gemstoneID = int.Parse(m.Groups[4].ToString());
                Dictionary<string, double> stats = new Dictionary<string, double>();
                CombineStats(stats,SQLiteConnect.gemstoneStats[gemstoneID]);
                ApplyStats(statDiff, stats);
                AddComposite(stats, SQLiteConnect.idToGemstone[gemstoneID],Composite);
                CombineStats(calculatedStats, stats);
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

        private void AddComposite(Dictionary<string,double> stats,string itemClass,BsonArray Composite) {
            Console.WriteLine("Add Composite");
            BsonDocument compositeStat = new BsonDocument();
            SetStats(compositeStat, stats);
            compositeStat.Add("ItemClass", itemClass);
            Composite.Add(compositeStat);
        }

        private static string getLowerType(string q1, string q2)
        {
            if (q1 == q2)
            {
                return q1;
            }
            int q1Num = getTypeNum(q1);
            int q2Num = getTypeNum(q2);
            if (q1Num < q2Num) {
                return q1;
            }
            return q2;
        }
        private static int getTypeNum(string type) {
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
