using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ServiceCore.CharacterServiceOperations;
namespace PacketCap
{
    public class SQLiteConnect
    {
        //private SQLiteConnection conn;
        public static Dictionary<int, Dictionary<string, double>> combineStats;
        public static Dictionary<string, Dictionary<string, double>> classCombineStats;
        public static Dictionary<int, string> idToItemClass;
        public static Dictionary<int, string> idToQualityType;
        public static Dictionary<int, string> idToEnhanceType;
        public static Dictionary<int, int> idToEnchantMaxLevel;

        public static Dictionary<int, Dictionary<string, double>> gemstoneStats;
        public static Dictionary<int, string> idToGemstone;
        public static Dictionary<int, int> idToGemstoneRank;

        public static string[] StatList;
        public static SQLiteConnection conn;

        public static Dictionary<string, string> statToReadableStat;

        public static void SetupDicts() {
            conn = new SQLiteConnection("Data Source=heroes.db3;Version=3");
            conn.Open();
            StatList = getStatsList();
            ReadCombineCraftPartsInfo();
            ReadGemstoneInfo();
            statToReadableStat = new Dictionary<string, string> {
                {"ATK","ATT"},
                {"MATK","MATT"},
                {"DEX","AGI"},
                {"WILL","WIL"},
                {"LUCK","LUK"},
                {"STAMINA","Stamina"},
                {"ATK_Speed","Attack Speed"},
                {"ATK_Absolute","Additional Damage"},
                {"Res_Critical","Critical Resistance"},
                {"PVP_ATK","PVP ATT"},
                {"PVP_MATK","PVP MATT"},
                {"PVP_DEF","PVP DEF"},
                {"TOWN_SPEED","Movement Speed(Town)"},
                {"ATK_LimitOver","Remove ATT limit"},
            };
            
        }

        private static void ReadGemstoneInfo() {
            gemstoneStats = new Dictionary<int, Dictionary<string, double>>();
            string sql = "SELECT * FROM GemstoneInfo";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();
            idToGemstone = new Dictionary<int, string>();
            idToGemstoneRank = new Dictionary<int, int>();
            while (reader.Read()) {
                Dictionary<string, double> stats = new Dictionary<string, double>();
                LoadStats(reader, stats);
                int id = int.Parse(reader["ID"].ToString());
                gemstoneStats[id] = stats;
                idToGemstone[id] = reader["ItemClass"].ToString();
                idToGemstoneRank[id] = int.Parse(reader["Grade"].ToString());
            }
        }

        public static string EnchantNumToLevel(int enchant) {
            switch (enchant) {
                case 1:
                    return "F";
                case 2:
                    return "E";
                case 3:
                    return "D";
                case 4:
                    return "C";
                case 5:
                    return "B";
                case 6:
                    return "A";
                case 7:
                    return "9";
                case 8:
                    return "8";
                case 9:
                    return "7";
                case 10:
                    return "6";
                case 11:
                    return "5";
            }
            return "-1";
        }

        private static void ReadCombineCraftPartsInfo() {
            combineStats = new Dictionary<int, Dictionary<string, double>>();
            classCombineStats = new Dictionary<string, Dictionary<string, double>>();
            idToItemClass = new Dictionary<int, string>();
            idToEnhanceType = new Dictionary<int, string>();
            idToQualityType = new Dictionary<int, string>();
            idToEnchantMaxLevel = new Dictionary<int, int>();

            string sql = "SELECT * FROM CombineCraftPartsInfo";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                Dictionary<string, double> stats = new Dictionary<string, double>();
                LoadStats(reader, stats);

                int id = int.Parse(reader["ID"].ToString());
                idToItemClass[id] = reader["ItemClass"].ToString();
                combineStats[id] = stats;
                string itemClass = reader["ItemClass"].ToString();
                classCombineStats[itemClass] = stats;
                idToEnhanceType[id] = reader["EnhanceType"].ToString();
                idToQualityType[id] = reader["QualityType"].ToString();
                int.TryParse(reader["EnchantMaxLevel"].ToString(), out int enchantMaxLevel);
                if (enchantMaxLevel == 0) {
                    enchantMaxLevel = 100;
                }
                idToEnchantMaxLevel[id] = enchantMaxLevel;
            }
        }

        private static void LoadStats(SQLiteDataReader reader, Dictionary<string, double> stats) {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string stat = reader.GetName(i);
                if (StatList.Contains(stat))
                {
                    double newVal = double.Parse(reader[stat].ToString());
                    if (newVal != 0)
                    {
                        if (stats.TryGetValue(stat, out double val))
                        {
                            stats[stat] = val + newVal;
                        }
                        else
                        {
                            stats[stat] = newVal;
                        }
                    }
                }
            }
        }

        public static string GetIcon(string itemClass) {
            string sql = "SELECT Icon FROM ItemClassInfo WHERE ItemClass = '" + itemClass + "' LIMIT 1";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                return reader["Icon"].ToString();
            }
            return "";
        }

        public static string GetAbilityClass(int id) {
            string sql = "SELECT * FROM AbilityClassInfo WHERE ID=" + id.ToString();
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                return reader["AbilityClass"].ToString();
            }
            return "";
        }

        public static void ApplyEnchantStats(string enchant, Dictionary<string, double> stats, out string buff)
        {
            string sql = "SELECT * FROM EnchantStatInfo WHERE EnchantClass = '" + enchant + "'";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();
            buff = "";
            while (reader.Read())
            {
                string statStr = reader["Stat"].ToString();
                MatchCollection mc = Regex.Matches(statStr, @"([@A-Za-z_]+)([+-]){?([0-9]+)}?");
                foreach (Match m in mc) {
                    string diffStr = m.Groups[2].ToString() + m.Groups[3].ToString();
                    double diff = double.Parse(diffStr);
                    string stat = m.Groups[1].ToString();
                    if (stat.StartsWith("@"))
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(stat);
                        sb.Remove(0, 1);
                        sb.Append("_");
                        sb.Append(diff);
                        buff = sb.ToString();
                    }
                    else {
                        if (stats.TryGetValue(stat, out double val))
                        {
                            stats[stat] = val + diff;
                        }
                        else
                        {
                            stats[stat] = diff;
                        }
                    }
                }
            }
        }

        public static int GetMaxQuality(string qualityType) {
            string sql = "SELECT DISTINCT Quality FROM QualityStatInfo WHERE ItemType = '"+qualityType+"' ORDER BY Quality DESC LIMIT 1";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                int quality = int.Parse(reader["Quality"].ToString());
                if (quality == 1) {
                    quality = 2;
                }
                return quality;
            }
            return -1;
        }

        public static void GetEnhanceQualityEnchant(string className, out string enhanceType, out string qualityType, out int enchantMaxLevel)
        {
            string sql = "SELECT EnhanceType,QualityType,EnchantMaxLevel FROM CombineCraftPartsInfo WHERE ItemClass = '" + className + "'";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();
            enhanceType = "";
            qualityType = "";
            enchantMaxLevel = 0;
            while (reader.Read())
            {
                enhanceType = reader["EnhanceType"].ToString();
                qualityType = reader["QualityType"].ToString();
                enchantMaxLevel = int.Parse(reader["EnchantMaxLevel"].ToString());
            }
        }

        public static int GetMaxEnhance(string enhanceType) {
            string sql = "SELECT EnhanceLevel FROM EnhanceInfo WHERE EnhanceType='"+enhanceType+"' ORDER BY EnhanceLevel DESC LIMIT 1";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                return int.Parse(reader["EnhanceLevel"].ToString());
            }
            return 0;
        }

        public static bool getEquipItemStats(string itemClass, Dictionary<string, double> stats, out string qualityType, out string enhanceType) {
            string sql = "SELECT * FROM EquipItemInfo WHERE ItemClass='"+itemClass+"'";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();
            qualityType = "";
            enhanceType = "";
            while (reader.Read()) {
                enhanceType = reader["EnhanceType"].ToString();
                qualityType = reader["QualityType"].ToString();
                LoadStats(reader, stats);
                return true;
            }
            return false;
        }

        public static void ApplyEnhanceStats(int enhance, string itemType, Dictionary<string, double> stats)
        {
            string sql = "SELECT Stat,Value,ValueType FROM EnhanceStatInfo WHERE EnhanceLevel = " + enhance.ToString() + " and EnhanceType='" + itemType + "'";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string stat = reader["Stat"].ToString();
                double modifier = double.Parse(reader["Value"].ToString());
                string valueType = reader["ValueType"].ToString();
                if (valueType == "Multiply")
                {
                    if (stats.TryGetValue(stat, out double val))
                    {
                        stats[stat] = (val * (1 + modifier));
                    }
                }
                else {
                    if (stats.TryGetValue(stat, out double val))
                    {
                        stats[stat] = (val + modifier);
                    }
                    else
                    {
                        stats[stat] = modifier;
                    }
                }
            }
        }

        public static void ApplyQualityMultiplier(int quality, string itemType, Dictionary<string,double> stats)
        {
            string sql = "SELECT Stat,Value FROM QualityStatInfo WHERE Quality = " + quality.ToString() + " and ItemType='" + itemType + "'";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string stat = reader["Stat"].ToString();
                double multiplier = 1+double.Parse(reader["Value"].ToString());
                if (stats.TryGetValue(stat, out double val)) {
                    stats[stat] = (int)(val * multiplier);
                }
            }
        }
        public static string[] getStatsList() {
            Array statsArr = Enum.GetValues(typeof(Stats));
            string[] statStrs = new string[statsArr.Length+1];
            for (int i = 0; i < statsArr.Length; i++) {
                statStrs[i] = statsArr.GetValue(i).ToString();
            }
            statStrs[statsArr.Length] = "ATK_LimitOver";
            return statStrs;
        }
        
    }
}
