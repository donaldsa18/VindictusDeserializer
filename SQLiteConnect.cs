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

        public static Dictionary<int, Dictionary<string, double>> gemstoneStats;
        public static Dictionary<int, string> idToGemstone;

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
            };
            
        }

        private static void ReadGemstoneInfo() {
            gemstoneStats = new Dictionary<int, Dictionary<string, double>>();
            string sql = "SELECT * FROM GemstoneInfo";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();
            idToGemstone = new Dictionary<int, string>();
            while (reader.Read()) {
                Dictionary<string, double> stats = new Dictionary<string, double>();
                LoadStats(reader, stats);
                int id = int.Parse(reader["ID"].ToString());
                gemstoneStats[id] = stats;
                idToGemstone[id] = reader["ItemClass"].ToString();
            }
        }

        private static void ReadCombineCraftPartsInfo() {
            combineStats = new Dictionary<int, Dictionary<string, double>>();
            classCombineStats = new Dictionary<string, Dictionary<string, double>>();
            idToItemClass = new Dictionary<int, string>();
            idToEnhanceType = new Dictionary<int, string>();
            idToQualityType = new Dictionary<int, string>();

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
            string sql = "SELECT Stat,Value FROM EnhanceStatInfo WHERE EnhanceLevel = " + enhance.ToString() + " and EnhanceType='" + itemType + "'";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string stat = reader["Stat"].ToString();
                double modifier = double.Parse(reader["Value"].ToString());
                if (stats.TryGetValue(stat, out double val))
                {
                    stats[stat] = (val + modifier);
                }
                else {
                    stats[stat] = modifier;
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
            string[] statStrs = new string[statsArr.Length];
            for (int i = 0; i < statsArr.Length; i++) {
                statStrs[i] = statsArr.GetValue(i).ToString();
            }
            return statStrs;
        }
        
    }
}
