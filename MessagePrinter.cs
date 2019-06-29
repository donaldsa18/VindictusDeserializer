using Devcat.Core.Net.Message;
using ServiceCore.CharacterServiceOperations;
using ServiceCore.EndPointNetwork;
using ServiceCore.EndPointNetwork.Captcha;
using ServiceCore.EndPointNetwork.GuildService;
using ServiceCore.EndPointNetwork.Housing;
using ServiceCore.EndPointNetwork.Item;
using ServiceCore.EndPointNetwork.MicroPlay;
using ServiceCore.ItemServiceOperations;
using ServiceCore.RankServiceOperations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using Nexon.CafeAuth;
using System.Drawing;
using ServiceCore.MicroPlayServiceOperations;
using System.Text.RegularExpressions;
using ServiceCore.EndPointNetwork.UserDS;
using ServiceCore.EndPointNetwork.Pvp;
using ServiceCore.PvpServiceOperations;
using ServiceCore.StoryServiceOperations;
using ServiceCore.DSServiceOperations;
using ServiceCore.UserDSHostServiceOperations;
using ServiceCore.TalkServiceOperations;
using ServiceCore.RelayServiceOperations;
using ServiceCore.QuestOwnershipServiceOperations;
using ServiceCore.PartyServiceOperations;
using ServiceCore.ChannelServiceOperations;
using ServiceCore.EndPointNetwork.CharacterList;
using ServiceCore.EndPointNetwork.DS;
using PacketCap.Database;

namespace PacketCap
{
    public class MessagePrinter
    {
        private Dictionary<Guid, int> categoryDict = new Dictionary<Guid, int>();
        private MessageHandlerFactory mf;

        private static BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public;

        private static void PrintLine(String formatStr, params object[] args) {
            string formatted = String.Format(formatStr, args);
            Console.Out.WriteLineAsync(formatted);
        }

        public void RegisterPrinters(MessageHandlerFactory mf, Dictionary<int, Guid> getGuid)
        {
            //PrintLine("registering printers");
            this.mf = mf;
            foreach (KeyValuePair<int, Guid> entry in getGuid)
            {
                categoryDict[entry.Value] = entry.Key;
            }

            foreach (MethodInfo m in this.GetType().GetMethods(bindingFlags))
            {
                if (m.Name.StartsWith("Print"))
                {
                    RegisterGeneric(m);
                }
                else
                {
                    //PrintLine("Not registering method {0}", m.Name);
                }
            }
            if (this.GetType() == typeof(MessagePrinter))
            {
                return;
            }
            foreach (MethodInfo m in typeof(MessagePrinter).GetMethods(bindingFlags))
            {
                if (m.Name.StartsWith("Print"))
                {
                    RegisterGeneric(m);
                }
                else
                {
                    //PrintLine("Not registering method {0}", m.Name);
                }
            }

        }

        private HashSet<Type> registeredTypes = new HashSet<Type>();

        private void RegisterGeneric(MethodInfo m)
        {
            ParameterInfo[] paramList = m.GetParameters();
            if (paramList.Length == 0) {
                return;
            }
            /*Console.Write("Registering {0}(", m.Name);
            foreach (ParameterInfo info in paramList)
            {

                Console.Write("{0} {1}, ", info.ParameterType.Name, info.Name);
            }
            PrintLine(")");
            */
            Type msgType = paramList[0].ParameterType;//...Message Type

            if (registeredTypes.Contains(msgType))
            {
                //PrintLine("Skipping");
                return;
            }
            Type[] typeArgs = { msgType, typeof(object) };

            Type generic = typeof(Action<,>);
            Type t = generic.MakeGenericType(typeArgs);//Action<...Message,object> Type

            MethodInfo register = typeof(MessagePrinter).GetMethod("Register", BindingFlags.NonPublic | BindingFlags.Instance);
            Delegate d = CreateDelegate(m);
            MethodInfo regGen = register.MakeGenericMethod(msgType);
            regGen.Invoke(this, new object[] { d });
            registeredTypes.Add(msgType);
        }

        private static Delegate CreateDelegate(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            if (!method.IsStatic)
            {
                throw new ArgumentException("The provided method must be static.", "method");
            }

            if (method.IsGenericMethod)
            {
                throw new ArgumentException("The provided method must not be generic.", "method");
            }

            return method.CreateDelegate(Expression.GetDelegateType(
                (from parameter in method.GetParameters() select parameter.ParameterType)
                .Concat(new[] { method.ReturnType })
                .ToArray()));
        }

        private void Register<T>(Action<T, object> printer)
        {
            if (categoryDict.ContainsKey(typeof(T).GUID))
            {
                mf.Register<T>(printer, categoryDict[typeof(T).GUID]);
            }
        }

        private static List<CharacterSummary> characters = null;


        public static void PrintCharacterListMessage(CharacterListMessage msg, object tag)
        {
            PrintLine("CharacterListMessage:");
            PrintLine("\tMaxFreeCharacterCount={0}", msg.MaxFreeCharacterCount);
            PrintLine("\tMaxPurchasedCharacterCount={0}", msg.MaxPurchasedCharacterCount);
            PrintLine("\tMaxPremiumCharacters={0}", msg.MaxPremiumCharacters);
            PrintLine("\tProloguePlayed={0}", msg.ProloguePlayed);
            PrintLine("\tPresetUsedCharacterCount={0}", msg.PresetUsedCharacterCount);
            PrintLine("\tLoginPartyState=[{0}]", String.Join(",", msg.LoginPartyState));
			if(msg.Characters == null || msg.Characters.Count == 0) {
				return;
			}

            int i = 0;
            characters = msg.Characters.ToList();
            if (MongoDBConnect.connection != null)
            {
                MongoDBConnect.connection.InsertCharacterSummaryList(characters);
            }
            foreach (CharacterSummary c in msg.Characters)
            {
                String title = String.Format("Character[{0}]", i++);
                
                PrintLine(CharacterSummaryToString(c, title, 1));
            }
        }

        public static string IntToRGB(int val)
        {
            if (val == -1 || val == 0 || val == 16777215)
            {
                return "";
            }
            Color c = Color.FromArgb(val);
            StringBuilder sb = new StringBuilder();
            sb.Append("rgb(");
            sb.Append(c.R);
            sb.Append(",");
            sb.Append(c.G);
            sb.Append(",");
            sb.Append(c.B);
            sb.Append(")");
            return sb.ToString();
        }


        private static String MakeColorFromDict(IDictionary<int, int> dict, int key)
        {
            StringBuilder sb = new StringBuilder();
            int val = -1;
            if (dict == null)
            {
                return "";
            }
            sb.Append(IntToSlot(key));
            sb.Append("=(");
            dict.TryGetValue(key, out val);

            sb.Append(IntToRGB(val));
            sb.Append(",");
            val = -1;
            dict.TryGetValue(key + 1, out val);
            sb.Append(IntToRGB(val));
            sb.Append(",");
            val = -1;
            dict.TryGetValue(key + 2, out val);
            sb.Append(IntToRGB(val));
            sb.Append(")");
            return sb.ToString();
        }

        public static String ColorDictToString(IDictionary<int, int> dict, String name, int numTabs, IDictionary<int, int> otherDict)
        {
            HashSet<int> keys = new HashSet<int>();
            StringBuilder sb = new StringBuilder();
            String t = new string('\t', numTabs);
            sb.Append(t);
            sb.Append(name);
            sb.Append(":");
            if (dict == null)
            {
                return sb.ToString();
            }
            t = new string('\t', numTabs + 1);

            foreach (KeyValuePair<int, int> entry in dict)
            {
                keys.Add(entry.Key / 3);
            }
            if (otherDict != null)
            {
                foreach (KeyValuePair<int, int> entry in otherDict)
                {
                    keys.Add(entry.Key / 3);
                }
            }
            int startLen = sb.Length;
            foreach (int key in keys)
            {
                String color = MakeColorFromDict(dict, key);
                String otherColor = MakeColorFromDict(otherDict, key);
                if (color != otherColor)
                {
                    sb.Append("\n");
                    sb.Append(t);
                    sb.Append(color);
                }
            }
            if (sb.Length == startLen)
            {
                return "";
            }
            else
            {
                return sb.ToString();
            }
        }

        private static String IntToBodyShapeString(int key)
        {
            switch (key)
            {

                case 15:
                    return "Muscles";
                case 17:
                    return "Body Fat";
                case 19:
                    return "Waist";
                case 22:
                    return "Legs";
                case 24:
                    return "Shoulders";
                case 40:
                    return "Arms";
                default:
                    return key.ToString();
            }
        }

        public static string BodyShapeInfoToString(IDictionary<int, float> BodyShapeInfo, int numTabs, IDictionary<int, float> otherBodyShapeInfo)
        {
            StringBuilder sb = new StringBuilder();
            String t = new string('\t', numTabs);
            sb.Append(t);
            sb.Append("BodyShapeInfo:");
            t = new string('\t', numTabs + 1);
            int startLen = sb.Length;
            foreach (KeyValuePair<int, float> entry in BodyShapeInfo)
            {
                if (otherBodyShapeInfo != null && otherBodyShapeInfo.TryGetValue(entry.Key, out float val) && val == entry.Value)
                {
                    continue;
                }
                sb.Append("\n");
                sb.Append(t);
                sb.Append(IntToBodyShapeString(entry.Key));
                sb.Append("=");
                sb.Append(entry.Value);
            }
            if (startLen == sb.Length)
            {
                return "";
            }
            else
            {
                return sb.ToString();
            }

        }

        private static Dictionary<string, CostumeInfo> costumeInfos = new Dictionary<string, CostumeInfo>();

        public static String CostumeInfoToString(CostumeInfo c, int numTabs, String name, String printName)
        {
            //TODO: send to database
            if (c == null)
            {
                return "";
            }
            if (name == null) {
                name = "";
            }
            costumeInfos.TryGetValue(name, out CostumeInfo l);
            if (l == null)
            {
                costumeInfos[name] = c;
            }
            string t = "";
            if (numTabs != 0)
            {
                t = new string('\t', numTabs);
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(t);
            sb.Append(printName);
            sb.Append(":");
            int startLen = sb.Length;
            t = "\n" + new string('\t', numTabs + 1);
            if (l == null || c.Shineness != l.Shineness)
            {
                sb.Append(t);
                sb.Append("Shineness=");
                sb.Append(c.Shineness);

            }
            if (l == null || c.Height != l.Height)
            {
                sb.Append(t);
                sb.Append("Height=");
                sb.Append(c.Height);
            }
            if (l == null || c.Bust != l.Bust)
            {
                sb.Append(t);
                sb.Append("Bust=");
                sb.Append(c.Bust);
            }
            if (l == null || c.PaintingPosX != l.PaintingPosX)
            {
                sb.Append(t);
                sb.Append("PaintingPosX=");
                sb.Append(c.PaintingPosX);
            }
            if (l == null || c.PaintingPosX != l.PaintingPosX)
            {
                sb.Append(t);
                sb.Append("PaintingPosY=");
                sb.Append(c.PaintingPosY);

            }
            if (l == null || c.PaintingRotation != l.PaintingRotation)
            {
                sb.Append(t);
                sb.Append("PaintingRotation=");
                sb.Append(c.PaintingRotation);

            }
            if (l == null || c.PaintingSize != l.PaintingSize)
            {
                sb.Append(t);
                sb.Append("PaintingSize=");
                sb.Append(c.PaintingSize);
            }
            if (l == null || c.HideHeadCostume != l.HideHeadCostume)
            {
                sb.Append(t);
                sb.Append("HideHeadCostume=");
                sb.Append(c.HideHeadCostume);
            }
            if (l == null || c.CafeType != l.CafeType)
            {
                sb.Append(t);
                sb.Append("CafeType=");
                sb.Append(c.CafeType);
            }
            if (l == null || c.IsReturn != l.IsReturn)
            {
                sb.Append(t);
                sb.Append("IsReturn=");
                sb.Append(c.IsReturn);
            }
            if (l == null || c.VIPCode != l.VIPCode)
            {
                sb.Append(t);
                sb.Append("VIPCode=");
                sb.Append(c.VIPCode);
            }
            String temp = CharacterDictToString<int>(c.CostumeTypeInfo, "CostumeTypeInfo", numTabs + 1, l?.CostumeTypeInfo);
            if (temp.Length != 0)
            {
                sb.Append("\n");
                sb.Append(temp);
            }
            temp = ColorDictToString(c.ColorInfo, "ColorInfo", numTabs + 1, l?.ColorInfo);
            if (temp.Length != 0)
            {
                sb.Append("\n");
                sb.Append(temp);
            }
            temp = CharacterDictToString<bool>(c.AvatarInfo, "AvatarInfo", numTabs + 1, l?.AvatarInfo);
            if (temp.Length != 0)
            {
                sb.Append("\n");
                sb.Append(temp);
            }

            if (l?.AvatarHideInfo != null && c.AvatarHideInfo.Count != 0)
            {
                temp = CharacterDictToString<int>(c.AvatarHideInfo, "AvatarHideInfo", numTabs + 1, l?.AvatarHideInfo);
                if (temp.Length != 0)
                {
                    sb.Append("\n");
                    sb.Append(temp);
                }
            }
            temp = CharacterDictToString<byte>(c.PollutionInfo, "PollutionInfo", numTabs + 1, l?.PollutionInfo);
            if (temp.Length != 0)
            {
                sb.Append("\n");
                sb.Append(temp);
            }
            temp = CharacterDictToString<int>(c.EffectInfo, "EffectInfo", numTabs + 1, l?.EffectInfo);
            if (temp.Length != 0)
            {
                sb.Append("\n");
                sb.Append(temp);
            }


            StringBuilder sb2 = new StringBuilder();

            foreach (KeyValuePair<int, int> entry in c.DecorationInfo)
            {
                if (l != null && l.DecorationInfo.TryGetValue(entry.Key, out int val) && val == entry.Value)
                {
                    continue;
                }
                sb2.Append(t);
                sb2.Append(IntToDecorationSlot(entry.Key));
                sb2.Append("=");
                sb2.Append(entry.Value);
            }

            if (sb2.Length != 0)
            {
                sb.Append(t);
                sb.Append("DecorationInfo:");
                sb.Append(sb2.ToString());
            }

            sb2 = new StringBuilder();
            foreach (KeyValuePair<int, int> entry in c.DecorationColorInfo)
            {
                if (l != null && l.DecorationColorInfo.TryGetValue(entry.Key, out int val) && val == entry.Value)
                {
                    continue;
                }
                sb2.Append(t);
                sb2.Append(IntToDecorationColorSlot(entry.Key));
                sb2.Append("=");
                sb2.Append(IntToRGB(entry.Value));

            }

            if (sb2.Length != 0)
            {
                sb.Append(t);
                sb.Append("DecorationColorInfo:");
                sb.Append(sb2.ToString());
            }
            String bodyShape = BodyShapeInfoToString(c.BodyShapeInfo, numTabs + 1, l?.BodyShapeInfo);
            if (bodyShape.Length != 0)
            {
                sb.Append("\n");
                sb.Append(BodyShapeInfoToString(c.BodyShapeInfo, numTabs + 1, l?.BodyShapeInfo));
            }

            costumeInfos[name] = c;
            if (startLen == sb.Length)
            {
                return "";
            }
            else
            {
                return sb.ToString();
            }
        }

        private static Dictionary<string, CharacterSummary> characterDict = new Dictionary<string, CharacterSummary>();

        public static string CharacterSummaryToString(CharacterSummary c, string name, int numTabs)
        {
            characterDict.TryGetValue(c.CharacterID, out CharacterSummary b);
            characterDict[c.CharacterID] = c;
            StringBuilder sb = new StringBuilder();
            String t = new string('\t', numTabs);
            sb.Append(t);
            sb.Append(name);
            sb.Append(":");

            t = "\n" + new string('\t', numTabs + 1);
            sb.Append(t);
            sb.Append("CharacterID=");
            sb.Append(c.CharacterID);
            if (b == null)
            {
                sb.Append(t);
                sb.Append("BaseCharacter=");
                sb.Append(BaseCharacterToString(c.BaseCharacter));
            }
            int startLen = sb.Length;
            if (b == null || c.Level != b.Level)
            {
                sb.Append(t);
                sb.Append("Level=");
                sb.Append(c.Level);
            }
            if (b == null || c.Title != b.Title)
            {
                sb.Append(t);
                sb.Append("Title=");
                sb.Append(c.Title.ToString());
            }
            if (b == null || c.TitleCount != b.TitleCount)
            {
                sb.Append(t);
                sb.Append("TitleCount=");
                sb.Append(c.TitleCount.ToString());
            }

            String temp = CostumeInfoToString(c.Costume, numTabs + 1, c.CharacterID.ToString(), "CostumeInfo");
            if (temp.Length != 0)
            {
                sb.Append("\n");
                sb.Append(temp);
            }

            if (c.Quote != null && c.Quote.Length != 0 && (b == null || c.Quote != b.Quote))
            {
                sb.Append(t);
                sb.Append("Quote=");
                sb.Append(c.Quote);
            }
            if (c.GuildName != null && c.GuildName.Length != 0 && (b == null || c.GuildName != b.GuildName))
            {
                sb.Append(t);
                sb.Append("GuildName=");
                sb.Append(c.GuildName);
            }
            if (b == null || c.VocationClass != b.VocationClass)
            {
                sb.Append(t);
                sb.Append("Vocation=");
                sb.Append(c.VocationClass.ToString());
            }

            if (c.Pet != null && (b == null || c.Pet.PetName != b.Pet.PetName))
            {
                sb.Append(t);
                sb.Append("Pet: Name=");
                sb.Append(c.Pet.PetName);
                sb.Append(", Type=");
                sb.Append(c.Pet.PetType.ToString());
            }
            if (c.FreeTitleName != null && c.FreeTitleName.Length != 0 && (b == null || c.FreeTitleName != b.FreeTitleName))
            {
                sb.Append(t);
                sb.Append("FreeTitleName=");
                sb.Append(c.FreeTitleName);
            }

            if (startLen == sb.Length)
            {
                return "";
            }
            else
            {
                return sb.ToString();
            }
        }

        public static void PrintMailListMessage(MailListMessage msg, object tag)
        {
            PrintLine("MailListMessage:");
            if (msg.ReceivedMailList != null && msg.ReceivedMailList.Count != 0)
            {
                PrintLine(ListToString<BriefMailInfo>(msg.ReceivedMailList, "ReceivedMailList", 1));
            }
            if (msg.SentMailList != null && msg.SentMailList.Count != 0)
            {
                PrintLine(ListToString<BriefMailInfo>(msg.SentMailList, "SentMailList", 1));
            }
        }

        private static Dictionary<string, List<StatusEffectElement>> lastStatusEffects = new Dictionary<string, List<StatusEffectElement>>();

        private static string StatusEffectListToString(List<StatusEffectElement> list, string name, string charName, int numTabs)
        {
            List<StatusEffectElement> last = null;
            lastStatusEffects.TryGetValue(charName, out last);

            StringBuilder sb = new StringBuilder();
            String t = "";
            if (numTabs > 0)
            {
                t = new string('\t', numTabs);
                sb.Append(t);
            }
            sb.Append(name);
            sb.Append(":");
            if (list == null || list.Count == 0)
            {
                lastStatusEffects[charName] = list;
                return sb.ToString();
            }
            t = "\n" + new string('\t', numTabs);
            sb.Append(t);

            foreach (StatusEffectElement e in list)
            {
                bool foundStatusEffect = false;
                foreach (StatusEffectElement l in last)
                {
                    if (e.Type == l.Type && e.Level == l.Level && e.RemainTime == l.RemainTime && e.CombatCount == l.CombatCount)
                    {
                        foundStatusEffect = true;
                        break;
                    }
                }
                if (foundStatusEffect)
                {
                    continue;
                }
                sb.Append(t);
                sb.Append("Type=");
                sb.Append(e.Type);
                sb.Append(" Level=");
                sb.Append(e.Level);
                sb.Append(" RemainTime=");
                sb.Append(e.RemainTime);
                sb.Append(" CombatCount=");
                sb.Append(e.CombatCount);
            }
            lastStatusEffects[charName] = list;
            return sb.ToString();
        }

        public static void PrintStatusEffectUpdated(StatusEffectUpdated msg, object tag)
        {
            PrintLine("StatusEffectUpdated:");
            PrintLine("\tCharacterName={0}", msg.CharacterName);
            PrintLine(StatusEffectListToString(msg.StatusEffects, "StatusEffects", msg.CharacterName, 1));
        }

        public static void PrintQuestProgressMessage(QuestProgressMessage msg, object tag)
        {
            PrintLine("QuestProgressMessage:");
            PrintLine(ListToString<QuestProgressInfo>(msg.QuestProgress, "QuestProgress", 1));
            if (msg.AchievedGoals != null && msg.AchievedGoals.Count != 0)
            {
                PrintLine(ListToString<AchieveGoalInfo>(msg.AchievedGoals, "AchievedGoals", 1));
            }
        }

        private static string IntToEquipmentSlot(int key)
        {
            switch (key)
            {
                case 21:
                    return "Hair";
                case 54:
                    return "Avatar Head";
                case 55:
                    return "Avatar Chest";
                case 56:
                    return "Avatar Pants";
                case 57:
                    return "Avatar Gloves";
                case 58:
                    return "Avatar Boots";
                default:
                    return key.ToString();
            }
        }

        private static IDictionary<int, long> lastEquipmentInfo = null;

        private static string QuickSlotInfoToString(QuickSlotInfo info, string name, int numTabs)
        {
            StringBuilder sb = new StringBuilder();
            String t = "";
            if (numTabs != 0)
            {
                t = new string('\t', numTabs);
            }
            sb.Append(t);
            sb.Append(name);
            sb.Append(":");
            t = "\n" + new string('\t', numTabs + 1);
            foreach (KeyValuePair<int, string> entry in info.SlotItemClasses)
            {
                if (entry.Value != null && entry.Value.Length != 0)
                {
                    sb.Append(t);
                    sb.Append(IntToEquipmentSlot(entry.Key));
                    sb.Append("=");
                    sb.Append(entry.Value);
                }
            }
            return sb.ToString();
        }

        public static void PrintUpdateSharedStorageInfoMessage(UpdateSharedStorageInfoMessage msg, object tag)
        {
            PrintLine("UpdateSharedStorageInfoMessage:");
            PrintLine("\tStorageInfos:");
            foreach (StorageInfo info in msg.StorageInfos)
            {
                PrintLine("\t\tstorageID={0} isAvailable={1} storageName={2} storageTag={3}", info.StorageID, info.IsAvailable, info.StorageName, info.StorageTag);
            }
        }

        public static void PrintInventoryInfoMessage(InventoryInfoMessage msg, object tag)
        {
            //TODO: db connect to share inventory
            PrintLine("InventoryInfoMessage:");
            PrintLine("\tStorageInfos:");
            foreach (StorageInfo info in msg.StorageInfos)
            {
                PrintLine("\t\tstorageID={0} isAvailable={1} storageName={2} storageTag={3}", info.StorageID, info.IsAvailable, info.StorageName, info.StorageTag);
            }

            Dictionary<string, SlotInfo> newInventoryItems = new Dictionary<string, SlotInfo>();
            foreach (SlotInfo slot in msg.SlotInfos)
            {
                string slotStr = slot.Slot != -1 ? slot.Slot.ToString() : slot.ItemClass;
                string infoStr = SlotInfoToString(slot, String.Format("SlotInfo[T={0},S={1}]", slot.Tab, slotStr), 1);
                string concated = Regex.Replace(infoStr, @"\s+", "");
                newInventoryItems[concated] = slot;
                if (inventoryItems.ContainsKey(concated))
                {
                    inventoryItems.Remove(concated);
                }
                else
                {
                    //found a new item
                    PrintLine(infoStr);
                }
            }
            inventoryItems = newInventoryItems;

            PrintLine(DictToString<int, long>(msg.EquipmentInfo, "EquipmentInfo", 1, lastEquipmentInfo));
            PrintLine(QuickSlotInfoToString(msg.QuickSlotInfo, "QuickSlotInfo", 1));
            lastEquipmentInfo = msg.EquipmentInfo;
            PrintLine("\tUnequippableParts=[{0}]", String.Join(",", msg.UnequippableParts));
        }

        public static void PrintTitleListMessage(TitleListMessage msg, object tag)
        {
            //TODO: fully parse
            PrintLine("TitleListMessage:");
            if (msg.AccountTitles != null && msg.AccountTitles.Count != 0)
            {
                PrintLine(ListToString<TitleSlotInfo>(msg.AccountTitles, "AccountTitles", 1));
            }
            if (msg.Titles != null && msg.Titles.Count != 0)
            {
                PrintLine(ListToString<TitleSlotInfo>(msg.Titles, "Titles", 1));
            }
        }

        public static void PrintRandomRankInfoMessage(RandomRankInfoMessage msg, object tag)
        {
            PrintLine("RandomRankInfoMessage:");
            foreach (RandomRankResultInfo info in msg.RandomRankResult)
            {
                PrintLine("\tRandomRankResult:");
                PrintLine("\t\tEventID={0}", info.EventID);
                PrintLine("\t\tPeriodType={0}", info.PeriodType);
                if (info.RandomRankResult != null && info.RandomRankResult.Count != 0)
                {
                    PrintLine(ListToString<RankResultInfo>(info.RandomRankResult, "RandomRankResult", 2));
                }
            }
        }

        public static void PrintManufactureInfoMessage(ManufactureInfoMessage msg, object tag)
        {
            PrintLine("ManufactureInfoMessage:");
            if (msg.ExpDictionary != null && msg.ExpDictionary.Count != 0)
            {
                PrintLine(DictToString<string, int>(msg.ExpDictionary, "ExpDictionary", 1));
            }
            if (msg.GradeDictionary != null && msg.GradeDictionary.Count != 0)
            {
                PrintLine(DictToString<string, int>(msg.GradeDictionary, "GradeDictionary", 1));
            }
            if (msg.Recipes != null && msg.Recipes.Count != 0)
            {
                PrintLine(ListToString<string>(msg.Recipes, "Recipes", 1));
            }
        }

        private static string Vector3DToString(Vector3D v, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name);
            sb.Append("=(");
            sb.Append(v.X);
            sb.Append(",");
            sb.Append(v.Y);
            sb.Append(",");
            sb.Append(v.Z);
            sb.Append(")");
            return sb.ToString();
        }

        private static string ActionSyncToString(ActionSync a, String name, int numTabs, ActionSync b)
        {
            StringBuilder sb = new StringBuilder();
            String t = new string('\t', numTabs);
            sb.Append(t);
            sb.Append(name);
            sb.Append(":");
            t = "\n" + (new string('\t', numTabs + 1));
            if (a.Position.X != b.Position.X || a.Position.Y != b.Position.Y || a.Position.Z != b.Position.Z)
            {
                sb.Append(t);
                sb.Append(Vector3DToString(a.Position, "Position"));
            }

            if (a.Velocity.X != b.Velocity.X || a.Velocity.Y != b.Velocity.Y || a.Velocity.Z != b.Velocity.Z)
            {
                sb.Append(t);
                sb.Append(Vector3DToString(a.Velocity, "Velocity"));
            }
            if (a.Yaw != b.Yaw)
            {
                sb.Append(t);
                sb.Append("Yaw=");
                sb.Append(a.Yaw.ToString());
            }

            sb.Append(t);
            sb.Append("Sequence=");
            sb.Append(a.Sequence.ToString());


            sb.Append(t);
            sb.Append("ActionStateIndex=");
            sb.Append(a.ActionStateIndex.ToString());

            if (a.StartTime != b.StartTime)
            {
                sb.Append(t);
                sb.Append("StartTime=");
                sb.Append(a.StartTime.ToString());
            }

            sb.Append(t);
            sb.Append("State=");
            sb.Append(a.State.ToString());
            return sb.ToString();
        }

        private static NotifyAction lastNotifyAction = null;

        public static void PrintNotifyAction(NotifyAction msg, object tag)
        {
            PrintLine("NotifyAction:");
            PrintLine("\tID={0}", msg.ID);
            ActionSync last = lastNotifyAction == null ? emptyActionSync : lastNotifyAction.Action;
            PrintLine(ActionSyncToString(msg.Action, "Action", 1, last));
            lastNotifyAction = msg;
            if (MongoDBConnect.connection != null) {
                MongoDBConnect.connection.InsertNotifyAction(msg, channel, TownID);
            }
            
        }

        public static void PrintDisappeared(Disappeared msg, object tag)
        {
            PrintLine("Disappeared: ID={0}", msg.ID);
        }

        public static void PrintUserLoginMessage(UserLoginMessage msg, object tag)
        {
            PrintLine("UserLoginMessage:");
            PrintLine("\tPassport={0}", msg.Passport);
            PrintLine("\tLocalAddress={0}", new IPAddress(msg.LocalAddress));
            if (msg.hwID != null && msg.hwID.Length != 0)
            {
                PrintLine("\thwID={0}", msg.hwID);
            }

            PrintLine("\t{0}", new MachineID(msg.MachineID));
            PrintLine("\tGameRoomClient={0}", msg.GameRoomClient);
            PrintLine("\tIsCharacterSelectSkipped={0}", msg.IsCharacterSelectSkipped);
            if (msg.NexonID != null && msg.NexonID.Length != 0)
            {
                PrintLine("\tNexonID={0}", msg.NexonID);
            }
            if (msg.UpToDateInfo != null && msg.UpToDateInfo.Length != 0)
            {
                PrintLine("\tUpToDateInfo={0}", msg.UpToDateInfo);
            }
            PrintLine("\tCheckSum={0}", msg.CheckSum);
        }


        public static void PrintClientLogMessage(ClientLogMessage msg, object tag)
        {
            String logType = ((object)(ClientLogMessage.LogTypes)msg.LogType).ToString();
            PrintLine("ClientLogMessage: {0} {1}={2}", logType, msg.Key, msg.Value);
        }

        public static void PrintEnterRegion(EnterRegion msg, object tag)
        {
            PrintLine("EnterRegion: RegionCode={0}", msg.RegionCode);
        }

        public static void PrintQueryCharacterCommonInfoMessage(QueryCharacterCommonInfoMessage msg, object tag)
        {
            PrintLine("QueryCharacterCommonInfoMessage: [QueryID={0} CID={1}]", msg.QueryID, msg.CID);
        }

        public static void PrintRequestJoinPartyMessage(RequestJoinPartyMessage msg, object tag)
        {
            PrintLine("RequestJoinPartyMessage: RequestType={0}", msg.RequestType);
        }

        private static ActionSync emptyActionSync = new ActionSync();

        public static void PrintEnterChannel(EnterChannel msg, object tag)
        {
            emptyActionSync = new ActionSync();
            emptyActionSync.Position = new Vector3D
            {
                X = -100000,
                Y = -100000,
                Z = -100000
            };
            emptyActionSync.Velocity = new Vector3D
            {
                X = -100000,
                Y = -100000,
                Z = -100000
            };
            emptyActionSync.Yaw = -1000000;
            emptyActionSync.Sequence = -20;
            emptyActionSync.ActionStateIndex = -10000;
            emptyActionSync.StartTime = -10000;
            emptyActionSync.State = -10000;

            PrintLine("EnterChannel:");
            PrintLine("\tChannelID={0}", msg.ChannelID);
            PrintLine("\tPartitionID={0}", msg.PartitionID);
            PrintLine(ActionSyncToString(msg.Action, "Action", 1, emptyActionSync));
        }

        public static void PrintQueryRankAlarmInfoMessage(QueryRankAlarmInfoMessage msg, object tag)
        {
            PrintLine("QueryRankAlarmInfoMessage: CID={0}", msg.CID);
        }

        public static void PrintQueryNpcTalkMessage(QueryNpcTalkMessage msg, object tag)
        {
            PrintLine("QueryNpcTalkMessage:");
            PrintLine("\tLocation={0}", msg.Location);
            PrintLine("\tNpcID={0}", msg.NpcID);
            PrintLine("\tStoryLine={0}", msg.StoryLine);
            PrintLine("\tCommand={0}", msg.Command);
        }

        public static void PrintQueryBattleInventoryInTownMessage(QueryBattleInventoryInTownMessage msg, object tag)
        {
            PrintLine("QueryBattleInventoryInTownMessage: []");
        }

        public static void PrintIdentify(ServiceCore.EndPointNetwork.Identify msg, object tag)
        {
            PrintLine("Identify: ID={0} Key={1}", msg.ID, msg.Key);
        }

        public static int TownID = 0;

        public static void PrintHotSpringRequestInfoMessage(HotSpringRequestInfoMessage msg, object tag)
        {
            if (msg == null)
            {
                return;
            }
            PrintLine("HotSpringRequestInfoMessage: Channel={0} TownID={1}", msg.Channel, msg.TownID);
            TownID = msg.TownID;
        }

        public static void PrintMovePartition(MovePartition msg, object tag)
        {
            PrintLine("MovePartition: TargetPartitionID={0}", msg.TargetPartitionID);
        }

        public static void PrintTradeItemClassListSearchMessage(TradeItemClassListSearchMessage msg, object tag)
        {
            PrintLine("TradeItemClassListSearchMessage:");
            PrintLine("\tuniqueNumber={0}", msg.uniqueNumber);
            PrintLine("\tChunkPageNumber={0}", msg.ChunkPageNumber);
            PrintLine("\tOrder={0}", msg.Order);
            PrintLine("\tisDescending={0}", msg.isDescending);
            PrintLine(ListToString<string>(msg.ItemClassList, "ItemClassList", 1));
            PrintLine("\tDetailOptions:");
            foreach (DetailOption d in msg.DetailOptions)
            {
                PrintLine("\t\t{0}={1} SearchType={2}", d.Key, d.Value, d.SearchType);
            }
        }

        private static UpdateAction lastUpdateAction = null;

        public static void PrintUpdateAction(UpdateAction msg, object tag)
        {
            ActionSync last = lastUpdateAction == null ? emptyActionSync : lastUpdateAction.Data;
            PrintLine(ActionSyncToString(msg.Data, "UpdateAction", 0, last));
            lastUpdateAction = msg;
        }

        public static void PrintTradeCategorySearchMessage(TradeCategorySearchMessage msg, object tag)
        {
            PrintLine("TradeCategorySearchMessage:");
            PrintLine("\ttradeCategory={0}", msg.tradeCategory);
            PrintLine("\ttradeCategorySub={0}", msg.tradeCategorySub);
            PrintLine("\tminLevel={0}", msg.minLevel);
            PrintLine("\tmaxLevel={0}", msg.maxLevel);
            PrintLine("\tuniqueNumber={0}", msg.uniqueNumber);
            PrintLine("\tChunkPageNumber={0}", msg.ChunkPageNumber);
            PrintLine("\tOrder={0}", msg.Order);
            PrintLine("\tisDescending={0}", msg.isDescending);
            PrintLine("\tDetailOptions:");
            foreach (DetailOption d in msg.DetailOptions)
            {
                PrintLine("\t\t{0}={1} SearchType={2}", d.Key, d.Value, d.SearchType);
            }
        }

        public static void PrintCreateCharacterMessage(CreateCharacterMessage msg, object tag)
        {
            PrintLine("CreateCharacterMessage:");
            PrintLine("\tName={0}", msg.Name);
            PrintLine("\tTemplace:");
            CharacterTemplate t = msg.Template;
            PrintLine("\t\tCharacterClass={0}", BaseCharacterToString((BaseCharacter)t.CharacterClass));
            PrintLine("\t\tSkinColor={0}", IntToRGB(t.SkinColor));
            PrintLine("\t\tShineness={0}", t.Shineness);
            PrintLine("\t\tEyeColor={0}", IntToRGB(t.EyeColor));
            PrintLine("\t\tHeight={0}", t.Height);
            PrintLine("\t\tBust={0}", t.Bust);
            PrintLine("\t\tPaintingPosX={0}", t.PaintingPosX);
            PrintLine("\t\tPaintingPosY={0}", t.PaintingPosY);
            PrintLine("\t\tPaintingRotation={0}", t.PaintingRotation);
            PrintLine("\t\tPaintingSize={0}", t.PaintingSize);
            PrintLine(BodyShapeInfoToString(t.BodyShapeInfo, 2, null));
        }
        public static void PrintCheckCharacterNameMessage(CheckCharacterNameMessage msg, object tag)
        {
            PrintLine("CheckCharacterNameMessage:");
            PrintLine("\tName={0}", msg.Name);
            PrintLine("\tIsNameChange={0}", msg.IsNameChange);
        }

        public static void PrintQueryReservedInfoMessage(QueryReservedInfoMessage msg, object tag)
        {
            PrintLine("QueryReservedInfoMessage: []");
        }

        public static void Print_UpdatePlayState(_UpdatePlayState msg, object tag)
        {
            PrintLine("_UpdatePlayState: State={0}", msg.State);
        }

        public static void PrintLogOutMessage(LogOutMessage msg, object tag)
        {
            PrintLine("LogOutMessage: []");
        }

        public static void PrintSyncFeatureMatrixMessage(SyncFeatureMatrixMessage msg, object tag)
        {
            PrintLine(DictToString<String, String>(msg.FeatureDic, "SyncFeatureMatrixMessage", 0));
        }
        public static void PrintGiveAPMessage(GiveAPMessage msg, object tag)
        {
            PrintLine("GiveAPMessage: AP={0}", msg.AP);
        }

        public static void PrintUserIDMessage(UserIDMessage msg, object tag)
        {
            PrintLine("UserIDMessage: {0}", msg.UserID);
        }

        public static void PrintAnswerFinishQuestMessage(AnswerFinishQuestMessage msg, object tag)
        {
            PrintLine("AnswerFinishQuestMessage: FollowHost={0}", msg.FollowHost);
        }

        public static void PrintExchangeMileageResultMessage(ExchangeMileageResultMessage msg, object tag)
        {
            bool IsSuccess = GetPrivateProperty<bool>(msg, "IsSuccess");
            PrintLine("ExchangeMileageResultMessage: IsSuccess={0}", IsSuccess);
        }
        public static void PrintSecuredOperationMessage(SecuredOperationMessage msg, object tag)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SecuredOperationMessage: Operation=");
            sb.Append(msg.Operation);
            sb.Append("LockedTime=");
            sb.Append(msg.LockedTimeInSeconds);
            PrintLine(sb.ToString());
        }
        public static void PrintUseInventoryItemWithCountMessage(UseInventoryItemWithCountMessage msg, object tag)
        {
            PrintLine("UseInventoryItemWithCountMessage:");
            PrintLine("\tItemID={0}", msg.ItemID);
            PrintLine("\tTargetItemID={0}", msg.TargetItemID);
            PrintLine("\tTargetItemCount={0}", msg.TargetItemCount);

        }
        public static void PrintAllUserJoinCompleteMessage(AllUserJoinCompleteMessage msg, object tag)
        {
            PrintLine("AllUserJoinCompleteMessage: []");
        }

        public static void PrintRequestMarbleProcessNodeMessage(RequestMarbleProcessNodeMessage msg, object tag)
        {
            PrintLine("RequestMarbleProcessNodeMessage: CurrentIndex={0}", msg.CurrentIndex);
        }
        public static void PrintQuerySharedInventoryMessage(QuerySharedInventoryMessage msg, object tag)
        {
            PrintLine("QuerySharedInventoryMessage: []");
        }

        public static void PrintUpdateHistoryBookMessage(UpdateHistoryBookMessage msg, object tag)
        {
            PrintLine("UpdateHistoryBookMessage:");
            PrintLine("\tType={0}", msg.Type);

            String arr = msg.HistoryBooks != null ? String.Join(",", msg.HistoryBooks) : "";
            PrintLine("\tHistoryBooks=[{0}]", arr);
        }

        public static void PrintRequestItemCombinationMessage(RequestItemCombinationMessage msg, object tag)
        {
            PrintLine("RequestItemCombinationMessage:");
            PrintLine("\tcombinedEquipItemClass={0}", msg.combinedEquipItemClass);
            PrintLine("\tpartsIDList=[{0}]", String.Join(",", msg.partsIDList));

        }
        public static void PrintGiveCashShopDiscountCouponResultMessage(GiveCashShopDiscountCouponResultMessage msg, object tag)
        {
            bool IsSuccess = GetPrivateProperty<bool>(msg, "IsSuccess");
            PrintLine("GiveCashShopDiscountCouponResultMessage: result={0}", IsSuccess);
        }

        public static void PrintOpenCustomDialogUIMessage(OpenCustomDialogUIMessage msg, object tag)
        {
            PrintLine("OpenCustomDialogUIMessage:");
            PrintLine("\tDialogType={0}", msg.DialogType); //System.Int32 has a toString()
            PrintLine("\tArg=[{0}]",String.Join(",",msg.Arg)); //System.Collections.Generic.List`1[System.String]
        }

        public static void PrintQuestListMessage(QuestListMessage msg, object tag)
        {
            PrintLine("QuestListMessage:");
            PrintLine("\tQuestList=[{0}]",String.Join(",",msg.QuestList)); //System.Collections.Generic.ICollection`1[System.String]
        }

        public static void PrintSaveHousingPropsMessage(SaveHousingPropsMessage msg, object tag)
        {
            PrintLine("SaveHousingPropsMessage:");
            foreach (HousingPropInfo info in msg.PropList)
            {
                if (info != null)
                {
                    PrintLine("\t{0}", info.ToString());
                }
            }
        }

        public static void PrintUpdateHousingPropsMessage(UpdateHousingPropsMessage msg, object tag)
        {
            PrintLine("UpdateHousingPropsMessage:");
            foreach (HousingPropInfo info in msg.PropList)
            {
                if (info != null)
                {
                    PrintLine("\t{0}", info.ToString());
                }
            }
        }

        public static String BaseCharacterToString(BaseCharacter m)
        {
            //TODO: add new characters
            String c = "";
            switch ((BaseCharacter)m)
            {
                case BaseCharacter.Lethita:
                    c = "Lann";//TODO huh?
                    break;
                case BaseCharacter.Fiona:
                    c = "Fiona";
                    break;
                case BaseCharacter.Evy:
                    c = "Evie";
                    break;
                case BaseCharacter.Kalok:
                    c = "Karok";
                    break;
                case BaseCharacter.Kay:
                    c = "Kai";
                    break;
                case BaseCharacter.Vella:
                    c = "Vella";
                    break;
                case BaseCharacter.Hurk:
                    c = "Hurk";
                    break;
                case BaseCharacter.Lynn:
                    c = "Lynn";
                    break;
                case BaseCharacter.Arisha:
                    c = "Arisha";
                    break;
                case BaseCharacter.Hagie:
                    c = "Sylas";
                    break;
                case BaseCharacter.CHARACTER_COUNT:
                    c = "CHARACTER_COUNT";
                    break;
                case BaseCharacter.ALL_CHARACTER:
                    c = "ALL_CHARACTER";
                    break;
            }
            return c;
        }

        private static string PetStatusInfoToString(PetStatusInfo p, string name, int numTabs)
        {
            string t = "";
            if (numTabs != 0)
            {
                t = new string('\t', numTabs);
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(t);
            sb.Append(name);
            sb.Append(":");
            t = "\n" + new string('\t', numTabs + 1);
            sb.Append(t);
            sb.Append("PetID=");
            sb.Append(p.PetID);
            sb.Append(t);
            sb.Append("PetName=");
            sb.Append(p.PetName);
            sb.Append(t);
            sb.Append("PetType=");
            sb.Append(p.PetType);
            sb.Append(t);
            sb.Append("Slot=");
            sb.Append(p.Slot);
            sb.Append(t);
            sb.Append("Level=");
            sb.Append(p.Level);
            sb.Append(t);
            sb.Append("Exp=");
            sb.Append(p.Exp);
            sb.Append(t);
            sb.Append("Desire=");
            sb.Append(p.Desire);
            sb.Append(t);
            sb.Append("PetStatus=");
            sb.Append(p.PetStatus);
            sb.Append(t);
            sb.Append("RemainActiveTime=");
            sb.Append(p.RemainActiveTime);
            sb.Append(t);
            sb.Append("RemainExpiredTime=");
            sb.Append(p.RemainExpiredTime);
            sb.Append(t);
            sb.Append("Skills:");
            String t2 = "\n" + new string('\t', numTabs + 2);
            foreach (PetSkillElement e in p.Skills)
            {
                sb.Append(t2);
                sb.Append("SkillID=");
                sb.Append(e.SkillID);
                sb.Append(" SlotOrder=");
                sb.Append(e.SlotOrder);
                sb.Append(" OpenLevel=");
                sb.Append(e.OpenLevel);
                sb.Append(" HasExpireDateTimeInfo=");
                sb.Append(e.HasExpireDateTimeInfo);
                sb.Append(" ExpireDateTimeDiff=");
                sb.Append(e.ExpireDateTimeDiff);
            }
            sb.Append(t);
            sb.Append("Accessories:");
            foreach (PetAccessoryElement e in p.Accessories)
            {
                sb.Append(t2);
                sb.Append("ItemClass=");
                sb.Append(e.ItemClass);
                sb.Append(" SlotOrder=");
                sb.Append(e.SlotOrder);
                sb.Append(" AccessorySize=");
                sb.Append(e.AccessorySize);
                sb.Append(" RemainingTime=");
                sb.Append(e.RemainingTime);
            }
            sb.Append(t);
            sb.Append("Stat:");
            PetStatElement el = p.Stat;
            sb.Append(t2);
            sb.Append("RequiredExp=");
            sb.Append(el.RequiredExp);
            sb.Append(t2);
            sb.Append("MaxExp=");
            sb.Append(el.MaxExp);
            sb.Append(t2);
            sb.Append("Hp=");
            sb.Append(el.Hp);
            sb.Append(t2);
            sb.Append("ResDamage=");
            sb.Append(el.ResDamage);
            sb.Append(t2);
            sb.Append("HpRecovery=");
            sb.Append(el.HpRecovery);
            sb.Append(t2);
            sb.Append("DefBreak=");
            sb.Append(el.DefBreak);
            sb.Append(t2);
            sb.Append("AtkBalance=");
            sb.Append(el.AtkBalance);
            sb.Append(t2);
            sb.Append("Atk=");
            sb.Append(el.Atk);
            sb.Append(t2);
            sb.Append("Def=");
            sb.Append(el.Def);
            sb.Append(t2);
            sb.Append("Critical=");
            sb.Append(el.Critical);
            sb.Append(t2);
            sb.Append("ResCritical=");
            sb.Append(el.ResCritical);
            return sb.ToString();
        }

        private static void AppendNotNull(StringBuilder sb, String s)
        {
            if (s != null && s.Length != 0)
            {
                sb.Append("\n");
                sb.Append(s);
            }
        }

        private static string GameJoinMemberInfoToString(GameJoinMemberInfo m, int tabs)
        {
            if (m == null)
            {
                return "";
            }
            String t = "";
            if (tabs != 0)
            {
                t = new string('\t', tabs);
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(t);
            sb.Append("Order=");
            sb.Append(m.Order);
            t = "\n" + t;
            sb.Append(t);
            sb.Append("Tag=");
            sb.Append(m.Tag);
            sb.Append(t);
            sb.Append("Key=");
            sb.Append(m.Key);
            sb.Append(t);
            sb.Append("Name=");
            sb.Append(m.Name);
            String c = BaseCharacterToString((BaseCharacter)m.BaseClass);
            sb.Append(t);
            sb.Append("Class=");
            sb.Append(c);
            sb.Append(t);
            sb.Append("Level=");
            sb.Append(m.Level);
            sb.Append(t);
            sb.Append("Exp=");
            sb.Append(m.Exp);
            sb.Append(t);
            sb.Append("LevelUpExp=");
            sb.Append(m.LevelUpExp);
            sb.Append(t);
            sb.Append("TitleID");//translate using heroes.db3 and translation xml
            sb.Append(m.TitleID);
            sb.Append(t);
            sb.Append("TitleCount=");
            sb.Append(m.TitleCount);
            AppendNotNull(sb, DictToString<string, int>(m.Stats, "Stats", tabs));
            AppendNotNull(sb, BattleInventoryToString(m.BattleInventory, "BattleInventory", tabs));
            AppendNotNull(sb, CostumeInfoToString(m.CostumeInfo, tabs, m.Name, "CostumeInfo"));

            if (m.Pet != null)
            {
                AppendNotNull(sb, PetStatusInfoToString(m.Pet, "Pet", 1));
            }

            AppendNotNull(sb, DictToString<string, int>(m.SkillList, "SkillList", tabs));
            AppendNotNull(sb, DictToString<int, string>(m.SpSkills, "SpSkills", tabs));
            AppendNotNull(sb, DictToString<string, int>(m.VocationSkills, "VocationSkills", tabs));

            sb.Append(t);
            sb.Append("TransformCoolDown=");
            sb.Append(m.TransformCoolDown);
            String t2 = t + "\t";
            if (m.SkillEnhanceDic != null && m.SkillEnhanceDic.Count != 0)
            {
                sb.Append(t);
                sb.Append("SkillEnhanceDic:");
                foreach (KeyValuePair<string, BriefSkillEnhance> e in m.SkillEnhanceDic)
                {
                    sb.Append(t2);
                    sb.Append(e.Key);
                    sb.Append("=(GroupKey=");
                    sb.Append(e.Value.GroupKey);
                    sb.Append(" IndexKey=");
                    sb.Append(e.Value.IndexKey);
                    sb.Append(" Type=");
                    sb.Append(e.Value.Type);
                    sb.Append(" ReduceDurability=");
                    sb.Append(e.Value.ReduceDurability);
                    sb.Append(" MaxDurabilityBonus=");
                    sb.Append(e.Value.MaxDurabilityBonus);
                    sb.Append(")");
                }
            }



            AppendNotNull(sb, DictToString<int, int>(m.DefMap, "DefMap", tabs));
            AppendNotNull(sb, DictToString<int, int>(m.ArmorHPMap, "ArmorHPMap", tabs));
            AppendNotNull(sb, DictToString<int, string>(m.EquippedItems, "EquippedItems", tabs));
            sb.Append(t);
            sb.Append("AbilityList=[");
            sb.Append(String.Join(",", m.AbilityList));
            sb.Append("]");
            AppendNotNull(sb, DictToString<string, int>(m.StatusEffectDict, "StatusEffectDict", tabs));
            AppendNotNull(sb, StatusEffectListToString(m.StatusEffects, "StatusEffects", m.Name, tabs));

            sb.Append(t);
            sb.Append("DestroyedDef=");
            sb.Append(m.DestroyedDef);
            sb.Append(t);
            sb.Append("IsTeacher");
            sb.Append(m.IsTeacher);
            sb.Append(t);
            sb.Append("IsAssist");
            sb.Append(m.IsAssist);

            AppendNotNull(sb, DictToString<string, int>(m.GuildLevelBonus, "GuildLevelBonus", tabs));

            sb.Append(t);
            sb.Append("IsAlive");
            sb.Append(m.IsAlive);

            return sb.ToString();
        }

        public static void PrintHousingMemberInfoMessage(HousingMemberInfoMessage msg, object tag)
        {
            //TODO: connect to database to collect avatar info
            PrintLine("HousingMemberInfoMessage:");
            PrintLine(GameJoinMemberInfoToString(msg.MemberInfo, 1));
        }

        public static void PrintHousingKickMessage(HousingKickMessage msg, object tag)
        {
            PrintLine("HousingKickMessage: Slot={0}, NexonSN={1}", msg.Slot, msg.NexonSN);
        }

        public static void PrintHousingInvitedMessage(HousingInvitedMessage msg, object tag)
        {
            PrintLine("HousingInvitedMessage: HostName={0} HousingID={1}", msg.HostName, msg.HousingID);
        }

        public static void PrintHousingHostRestartingMessage(HousingHostRestartingMessage msg, object tag)
        {
            PrintLine("HousingHostRestartingMessage: []");
        }

        public static void PrintEnterHousingMessage(EnterHousingMessage msg, object tag)
        {
            PrintLine("EnterHousingMessage: CharacterName={0} HousingIndex={1} EnterType={2} HousingPlayID={3}", msg.CharacterName, msg.HousingIndex, msg.EnterType, msg.HousingPlayID);
        }
        public static void PrintEndPendingDialogMessage(EndPendingDialogMessage msg, object tag)
        {
            PrintLine("EndPendingDialogMessage []");
        }

        public static void PrintCreateHousingMessage(CreateHousingMessage msg, object tag)
        {
            PrintLine("CreateHousingMessage: OpenLevel={0} Desc={1}", msg.OpenLevel, msg.Desc);
        }

        public static void PrintHotSpringRequestPotionEffectMessage(HotSpringRequestPotionEffectMessage msg, object tag)
        {
            PrintLine("HotSpringRequestPotionEffectMessage: Channel={0} TownID={1} PotionItemClass={2}", msg.Channel, msg.TownID, msg.PotionItemClass);
        }

        public static void PrintHotSpringAddPotionMessage(HotSpringAddPotionMessage msg, object tag)
        {
            PrintLine("HotSpringAddPotionMessage: Channel={0} TownID={1} ItemID={2}", msg.Channel, msg.TownID, msg.ItemID);
        }

        public static void PrintBurnItemsMessage(BurnItemsMessage msg, object tag)
        {
            PrintLine("BurnItemsMessage:");
            foreach (BurnItemInfo info in msg.BurnItemList)
            {
                PrintLine("\tItemID={0} Count={1}", info.ItemID, info.Count);
            }
        }

        public static void PrintFreeTitleNameCheckMessage(FreeTitleNameCheckMessage msg, object tag)
        {
            PrintLine("FreeTitleNameCheckMessage: ItemID={0} FreeTitleName={1}", msg.ItemID, msg.FreeTitleName);
        }

        public static void PrintBurnRewardItemsMessage(BurnRewardItemsMessage msg, object tag)
        {
            PrintLine("BurnRewardItemsMessage:");
            PrintLine(DictToString<string, int>(msg.RewardItems, "RewardItems", 1));
            PrintLine(DictToString<string, int>(msg.RewardMailItems, "RewardMailItems", 1));
        }

        public static void PrintAllUserGoalEventModifyMessage(AllUserGoalEventModifyMessage msg, object tag)
        {
            PrintLine("AllUserGoalEventModifyMessage: GoalID={0} Count={1}", msg.GoalID, msg.Count);
        }

        public static void PrintAvatarSynthesisItemMessage(AvatarSynthesisItemMessage msg, object tag)
        {
            PrintLine("AvatarSynthesisItemMessage: Material1ID={0} Material2ID={1} Material3ID={2}", msg.Material1ID, msg.Material2ID, msg.Material3ID);
        }

        public static void PrintGetFriendshipPointMessage(GetFriendshipPointMessage msg, object tag)
        {
            PrintLine("GetFriendshipPointMessage []");
        }

        public static void PrintExchangeMileageMessage(ExchangeMileageMessage msg, object tag)
        {
            PrintLine("ExchangeMileageMessage []");
        }

        public static void PrintCaptchaResponseMessage(CaptchaResponseMessage msg, object tag)
        {
            PrintLine("CaptchaResponseMessage: AuthCode={0} Response={1}", msg.AuthCode, msg.Response);
        }

        public static void PrintGuildChatMessage(GuildChatMessage msg, object tag)
        {
            PrintLine("GuildChatMessage:");
            PrintLine("\tSender={0}", msg.Sender);
            PrintLine("\tMessage={0}", msg.Message);
        }

        public static void PrintChangeMasterMessage(ChangeMasterMessage msg, object tag)
        {
            PrintLine("ChangeMasterMessage:");
            PrintLine("\tNewMasterName={0}", msg.NewMasterName); //System.String has a toString()
        }

        public static void PrintGuildGainGPMessage(GuildGainGPMessage msg, object tag)
        {
            PrintLine("GuildGainGPMessage:");
            PrintLine("\tGuildPoint={0}", msg.GuildPoint); //System.Int64 has a toString()
            PrintLine(DictToString<byte,int>(msg.DailyGainGP,"DailyGainGP",1)); //System.Collections.Generic.Dictionary`2[System.Byte,System.Int32]
        }

        public static void PrintGuildLevelUpMessage(GuildLevelUpMessage msg, object tag)
        {
            PrintLine("GuildLevelUpMessage:");
            PrintLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
        }

        private static string ListToString<T>(ICollection<T> list, String name, int numTabs)
        {
            StringBuilder sb = new StringBuilder();
            String t = "";
            if (numTabs > 0)
            {
                t = new string('\t', numTabs);
            }

            if (name != null && name.Length != 0)
            {
                sb.Append(t);
                sb.Append(name);
                sb.Append(":");
            }
            if (list == null || list.Count == 0)
            {
                return sb.ToString();
            }
            t = "\n" + new string('\t', numTabs + 1);
            foreach (T element in list)
            {
                if (element == null)
                {
                    continue;
                }
                sb.Append(t);
                sb.Append(element);
            }
            return sb.ToString();
        }

        public static void PrintHousingRoomListMessage(HousingRoomListMessage msg, object tag)
        {
            PrintLine("HousingRoomListMessage:");
            PrintLine(ListToString<HousingRoomInfo>(msg.HousingRoomList, "HousingRoomList", 1));
        }

        public static void PrintHotSpringRequestInfoResultMessage(HotSpringRequestInfoResultMessage msg, object tag)
        {
            PrintLine("HotSpringRequestInfoResultMessage: TownID={0}", msg.TownID);
            foreach (HotSpringPotionEffectInfo info in msg.HotSpringPotionEffectInfos)
            {
                if (info != null) {
                    PrintLine("\tPotionItemClass={0} CharacterName={1} GuildName={2} ExpiredTime={3} OtherPotionUsableTime={4}", info.PotionItemClass, info.CharacterName, info.GuildName, info.ExpiredTime, info.OtherPotionUsableTime);
                }
            }
        }
        public static void PrintBurnJackpotMessage(BurnJackpotMessage msg, object tag)
        {
            PrintLine("BurnJackpotMessage: CID={0}", msg.CID);
        }

        public static void PrintRequestMarbleCastDiceMessage(RequestMarbleCastDiceMessage msg, object tag)
        {
            PrintLine("RequestMarbleCastDiceMessage: DiceID={0}", msg.DiceID);
        }

        public static void PrintUpdateHousingItemsMessage(UpdateHousingItemsMessage msg, object tag)
        {
            PrintLine("UpdateHousingItemsMessage: ClearInven={0}", msg.ClearInven);
            foreach (HousingItemInfo info in msg.ItemList)
            {
                PrintLine("\t{0}", info);
            }

        }
        public static void PrintHousingPartyInfoMessage(HousingPartyInfoMessage msg, object tag)
        {
            PrintLine("HousingPartyInfoMessage:");
            PrintLine("\tHousingID={0}", msg.HousingID); //System.Int64 has a toString()
            PrintLine("\tPartySize={0}", msg.PartySize); //System.Int32 has a toString()
            PrintLine("\tMembers:");
            foreach (HousingPartyMemberInfo m in msg.Members) {
                PrintLine("\t\tHousingPartyMemberInfo:");
                PrintLine("\t\t\tNexonSN={0}", m.NexonSN); //System.Int32 has a toString()
                PrintLine("\t\t\tCharacter={0}", m.Character); //ServiceCore.CharacterServiceOperations.BaseCharacter
                PrintLine("\t\t\tCharacterName={0}", m.CharacterName); //System.String has a toString()
                PrintLine("\t\t\tSlotNumber={0}", m.SlotNumber); //System.Int32 has a toString()
                PrintLine("\t\t\tLevel={0}", m.Level); //System.Int32 has a toString()
            }
            PrintLine("\tMembers={0}",msg.Members); //System.Collections.Generic.ICollection`1[ServiceCore.EndPointNetwork.Housing.HousingPartyMemberInfo]
        }

        public static void PrintAddFriendShipResultMessage(AddFriendShipResultMessage msg, object tag)
        {
            String result = ((AddFriendShipResultMessage.AddFriendShipResult)msg.Result).ToString();
            PrintLine("AddFriendShipResultMessage: friendName={0} Result={1}", msg.friendName, result);
        }

        public static void PrintHousingInvitationRejectMessage(HousingInvitationRejectMessage msg, object tag)
        {
            PrintLine("HousingInvitationRejectMessage: HousingID={0}",msg.HousingID);
        }

        public static void PrintEnhanceSuccessRatioDebugMessage(EnhanceSuccessRatioDebugMessage msg, object tag)
        {
            PrintLine("EnhanceSuccessRatioDebugMessage:");
            PrintLine("\tSuccessRatio={0}", msg.SuccessRatio); //System.Single has a toString()
            PrintLine("\tBonusRatio={0}", msg.BonusRatio); //System.Single has a toString()
            PrintLine("\tFeaturebonusratio={0}", msg.Featurebonusratio); //System.Single has a toString()
        }

        public static void PrintHasSecondPasswordMessage(HasSecondPasswordMessage msg, object tag)
        {
            PrintLine("HasSecondPasswordMessage:");
            PrintLine("\tIsFirstQuery={0}", msg.IsFirstQuery);
            PrintLine("\tHasSecondPassword={0}", msg.HasSecondPassword);
            PrintLine("\tIsPassed={0}", msg.IsPassed);
            PrintLine("\tFailCount={0}", msg.FailCount);
            PrintLine("\tRetryLockedSec={0}", msg.RetryLockedSec);
        }

        public static void PrintGetUserIDMessage(GetUserIDMessage msg, object type)
        {
            PrintLine("GetUserIDMessage: []");
        }

        public static void PrintMakeNamedRingMessage(MakeNamedRingMessage msg, object type)
        {
            PrintLine("MakeNamedRingMessage: ItemID={0} UserName={1}", msg.ItemID, msg.UserName);
        }

        public static void PrintMarbleInfoResultMessage(MarbleInfoResultMessage msg, object type)
        {
            PrintLine("MarbleInfoResultMessage:");
            PrintLine("\tMarbleID={0}", msg.MarbleID);
            PrintLine("\tCurrentIndex={0}", msg.CurrentIndex);
            PrintLine("\tNodeList:");
            foreach (MarbleNode node in msg.NodeList)
            {
                PrintLine("\t\tMarbleNode:");
                PrintLine("\t\t\tNodeIndex={0}", node.NodeIndex);
                PrintLine("\t\t\tNodeType={0}", node.NodeType);
                PrintLine("\t\t\tNodeGrade={0}", node.NodeGrade);
                PrintLine("\t\t\tArg=[{0}]", String.Join(",", node.Arg));
                PrintLine("\t\t\tDesc={0}", node.Desc);
            }
            PrintLine("\tIsFirst={0}", msg.IsFirst);
            PrintLine("\tIsProcessed={0}", msg.IsProcessed);
        }

        public static void PrintRequestPartChangingMessage(RequestPartChangingMessage msg, object type)
        {
            PrintLine("RequestPartChangingMessage: combinedEquipItemID={0} targetIndex={1} partID={2}", msg.combinedEquipItemID, msg.targetIndex, msg.partID);
        }

        public static void PrintCaptchaResponseResultMessage(CaptchaResponseResultMessage msg, object tag)
        {
            int Result = GetPrivateProperty<int>(msg, "Result");
            PrintLine("CaptchaResponseResultMessage: Result={0}", Result);
        }

        public static void PrintJoinGuildChatRoomMessage(JoinGuildChatRoomMessage msg, object tag)
        {
            PrintLine("JoinGuildChatRoomMessage: GuildKey={0}", msg.GuildKey);
        }

        public static void PrintHousingListMessage(HousingListMessage msg, object tag)
        {
            PrintLine("HousingListMessage: HousingList=[{0}]", String.Join(",", msg.HousingList));
        }

        public static void PrintAvatarSynthesisMaterialRecipesMessage(AvatarSynthesisMaterialRecipesMessage msg, object tag)
        {
            PrintLine("AvatarSynthesisMaterialRecipesMessage: MaterialRecipies=[{0}]", String.Join(",", msg.MaterialRecipes));
        }

        public static void PrintAvatarSynthesisRequestMessage(AvatarSynthesisRequestMessage msg, object tag)
        {
            PrintLine("AvatarSynthesisRequestMessage:");
            PrintLine("\tFirstItemID={0}", msg.FirstItemID); //System.Int64 has a toString()
            PrintLine("\tSecondItemID={0}", msg.SecondItemID); //System.Int64 has a toString()
        }

        public static void PrintGameResourceRespondMessage(GameResourceRespondMessage msg, object tag)
        {
            PrintLine("GameResourceRespondMessage: ResourceRespond={0}", msg.ResourceRespond);
        }

        public static void PrintAllUserGoalEventMessage(AllUserGoalEventMessage msg, object tag)
        {
            PrintLine("AllUserGoalEventMessage:");
            PrintLine(DictToString<int, int>(msg.AllUserGoalInfo, "AllUserGoalInfo", 1));
        }

        public static void PrintLeaveHousingMessage(LeaveHousingMessage msg, object tag)
        {
            PrintLine("LeaveHousingMessage []");
        }

        public static void PrintDecomposeItemResultMessage(DecomposeItemResultMessage msg, object tag)
        {
            PrintLine("DecomposeItemResultMessage: ResultEXP={0}", msg.ResultEXP);
            PrintLine(ListToString<string>(msg.GiveItemClassList, "GiveItemClassList", 1));

        }

        public static void PrintQueryAvatarSynthesisMaterialRecipesMessage(QueryAvatarSynthesisMaterialRecipesMessage msg, object tag)
        {
            PrintLine("QueryAvatarSynthesisMaterialRecipesMessage: []");
        }

        public static void PrintSearchHousingRoomMessage(SearchHousingRoomMessage msg, object tag)
        {
            PrintLine("SearchHousingRoomMessage: Option={0} Keyword={1}", msg.Option, msg.Keyword);
        }

        public static void PrintMarbleSetTimerMessage(MarbleSetTimerMessage msg, object tag)
        {
            PrintLine("MarbleSetTimerMessage: Time={0}", msg.Time);
        }

        public static void PrintFreeTitleNameCheckResultMessage(FreeTitleNameCheckResultMessage msg, object tag)
        {
            PrintLine("FreeTitleNameCheckResultMessage:");
            PrintLine("\tItemID={0}", msg.ItemID);
            PrintLine("\tFreeTitleName={0}", msg.FreeTitleName);
            PrintLine("\tIsSuccess={0}", msg.IsSuccess);
            PrintLine("\tHasFreeTitle={0}", msg.HasFreeTitle);
        }

        public static void PrintInsertBlessStoneCompleteMessage(InsertBlessStoneCompleteMessage msg, object tag)
        {
            PrintLine("InsertBlessStoneCompleteMessage:");
            PrintLine("\tSlot={0}", msg.Slot);
            PrintLine("\tOwnerList=[{0}]", String.Join(",", msg.OwnerList));
            PrintLine("\tTypeList=[{0}]", String.Join(",", msg.TypeList));
            /*StringBuilder sb = new StringBuilder();
            if (msg.TypeList != null && msg.TypeList.Count != 0) {
                foreach (BlessStoneType t in msg.TypeList)
                {
                    sb.Append(BlessStoneToString(t));
                    sb.Append(",");
                }
                sb.Remove(sb.Length - 1, 1);
                PrintLine("\tTypeList=[{0}]", sb.ToString());
            }*/
        }

        public static void PrintEnchantLimitlessMessage(EnchantLimitlessMessage msg, object tag)
        {
            PrintLine("EnchantLimitlessMessage:");
            PrintLine("\tenchantID={0}", msg.enchantID); //System.Int64 has a toString()
            PrintLine("\tenchantLimitlessID={0}", msg.enchantLimitlessID); //System.Int64 has a toString()
        }

        public static void PrintRequestBraceletCombinationMessage(RequestBraceletCombinationMessage msg, object tag)
        {
            PrintLine("RequestBraceletCombinationMessage:");
            PrintLine("\tBreaceletItemID={0}", msg.BreaceletItemID); //System.Int64 has a toString()
            PrintLine("\tGemstoneItemID={0}", msg.GemstoneItemID); //System.Int64 has a toString()
            PrintLine("\tGemstoneIndex={0}", msg.GemstoneIndex); //System.Int32 has a toString()
            PrintLine("\tIsChanging={0}", msg.IsChanging); //System.Boolean has a toString()
        }

        public static void PrintSwapHousingItemMessage(SwapHousingItemMessage msg, object tag)
        {
            PrintLine("SwapHousingItemMessage:");
            PrintLine("\tFrom={0}", msg.From); //System.Int32 has a toString()
            PrintLine("\tTo={0}", msg.To); //System.Int32 has a toString()
        }

        public static void PrintMarbleProcessNodeResultMessage(MarbleProcessNodeResultMessage msg, object tag)
        {
            PrintLine("MarbleProcessNodeResultMessage: Type={0} IsChance={1}", msg.Type, msg.IsChance);
        }

        public static void PrintBurnGaugeRequestMessage(BurnGaugeRequestMessage msg, object tag)
        {
            PrintLine("BurnGaugeRequestMessage: []");
        }

        public static void PrintSetQuoteMessage(SetQuoteMessage msg, object tag)
        {
            PrintLine("SetQuoteMessage:");
            PrintLine("\tQuote={0}", msg.Quote); //System.String has a toString()
        }

        public static void PrintRequestAttendanceRewardMessage(RequestAttendanceRewardMessage msg, object tag)
        {
            PrintLine("RequestAttendanceRewardMessage:");
            PrintLine("\tEventType={0}", msg.EventType); //System.Int32 has a toString()
            PrintLine("\tIsBonus={0}", msg.IsBonus); //System.Boolean has a toString()
        }

        public static void PrintHousingGameHostedMessage(HousingGameHostedMessage msg, object tag)
        {
            PrintLine("HousingGameHostedMessage: Map={0} IsOwner={1}", msg.Map, msg.IsOwner);
            PrintLine(ListToString<HousingPropInfo>(msg.HousingProps, "HousingProps", 1));
            PrintLine("\tHostInfo:");
            PrintLine(GameJoinMemberInfoToString(msg.HostInfo, 2));
        }

        public static void PrintHousingKickedMessage(HousingKickedMessage msg, object tag)
        {
            PrintLine("HousingKickedMessage: []");
        }

        public static void PrintBuyIngameCashshopUseTirMessage(BuyIngameCashshopUseTirMessage msg, object tag)
        {
            PrintLine("BuyIngameCashshopUseTirMessage: Products=[{0}]", String.Join(",", msg.Products));
        }

        public static void PrintRequestAddPeerMessage(RequestAddPeerMessage msg, object tag)
        {
            PrintLine("RequestAddPeerMessage: PingEntityIDs=[{0}]", String.Join(",", msg.PingEntityIDs));
        }

        public static void PrintMaxDurabilityRepairItemMessage(MaxDurabilityRepairItemMessage msg, object tag)
        {
            PrintLine("MaxArmorRepairItemMessage: TargetItemID={0} SourceItemID={1}", msg.TargetItemID, msg.SourceItemID);
        }

        public static void PrintSecondPasswordResultMessage(SecondPasswordResultMessage msg, object tag)
        {
            PrintLine("SecondPasswordResultMessage:");
            PrintLine("\tOperationType={0}", msg.OperationType); //ServiceCore.EndPointNetwork.SecondPasswordResultMessage+ProcessType
            PrintLine("\tPassed={0}", msg.Passed); //System.Boolean has a toString()
            PrintLine("\tFailCount={0}", msg.FailCount); //System.Int32 has a toString()
            PrintLine("\tRetryLockedSec={0}", msg.RetryLockedSec); //System.Int32 has a toString()
        }

        public static string IntToDecorationSlot(int key)
        {
            switch (key)
            {
                case 1:
                    return "Makeup";
                case 2:
                    return "Face Tattoo";
                case 3:
                    return "Scar";
                case 6:
                    return "Tattoo";
                default:
                    return key.ToString();
            }
        }

        private static string IntToSlot(int key)
        {
            switch (key)
            {
                case 1:
                    return "Inner";
                case 3:
                    return "Hair";
                case 4:
                    return "Chest";
                case 5:
                    return "Pants";
                case 6:
                    return "Helmet";
                case 7:
                    return "Boots";
                case 8:
                    return "Gloves";
                case 100:
                    return "Weapon";
                case 101:
                    return "Shield/Focus/Book";
                default:
                    return key.ToString();
            }
        }

        public static string IntToDecorationColorSlot(int key)
        {
            switch (key)
            {
                default:
                    return key.ToString();
            }
        }

        public static String CharacterDictToString<T>(IDictionary<int, T> dict, String name, int numTabs, IDictionary<int, T> otherDict)
        {
            String t = new string('\t', numTabs);
            StringBuilder sb = new StringBuilder();
            sb.Append(t);
            t = new string('\t', numTabs + 1);
            sb.Append(name);
            sb.Append(":");
            if (dict == null)
            {
                return sb.ToString();
            }
            int startLen = sb.Length;
            foreach (KeyValuePair<int, T> entry in dict)
            {
                if (otherDict != null && otherDict.TryGetValue(entry.Key, out T val) && Comparer<T>.Default.Compare(val, entry.Value) == 0)
                {
                    continue;
                }
                sb.Append("\n");
                sb.Append(t);
                sb.Append(IntToSlot(entry.Key));
                sb.Append("=");
                sb.Append(entry.Value);
            }
            if (sb.Length == startLen)
            {
                return "";
            }
            else
            {
                return sb.ToString();
            }
        }

        private static String DictToString<T1, T2>(IDictionary<T1, T2> dict, String name, int numTabs, IDictionary<T1, T2> lastDict)
        {
            StringBuilder sb = new StringBuilder();
            String t = "";
            if (numTabs > 0)
            {
                t = new string('\t', numTabs);
                sb.Append(t);
            }
            sb.Append(name);
            sb.Append(":");
            if (dict == null || dict.Count == 0)
            {
                return sb.ToString();
            }
            t = new string('\t', numTabs + 1);
            int startLen = sb.Length;
            foreach (KeyValuePair<T1, T2> entry in dict)
            {
                if (lastDict != null && lastDict.TryGetValue(entry.Key, out T2 val) && Comparer<T2>.Default.Compare(val, entry.Value) == 0)
                {
                    continue;
                }
                sb.Append("\n");
                sb.Append(t);
                sb.Append(entry.Key);
                sb.Append("=");
                sb.Append(entry.Value);
            }
            if (sb.Length == startLen)
            {
                return "";
            }
            else
            {
                return sb.ToString();
            }
        }

        private static String DictToString<T1, T2>(IDictionary<T1, T2> dict, String name, int numTabs)
        {
            StringBuilder sb = new StringBuilder();
            String t = "";
            if (numTabs > 0)
            {
                t = new string('\t', numTabs);
                sb.Append(t);
            }
            t = new string('\t', numTabs + 1);
            sb.Append(name);
            sb.Append(":\n");
            if (dict == null)
            {
                return sb.ToString();
            }
            foreach (KeyValuePair<T1, T2> entry in dict)
            {
                sb.Append(t);
                sb.Append(entry.Key);
                sb.Append("=");
                sb.Append(entry.Value);
                sb.Append("\n");
            }
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        public static void PrintCharacterCommonInfoMessage(CharacterCommonInfoMessage msg, object tag)
        {
            if (MongoDBConnect.connection != null)
            {
                MongoDBConnect.connection.InsertCharacterSummary(msg.Info);
            }
            PrintLine(CharacterSummaryToString(msg.Info, "CharacterCommonInfoMessage", 0));
        }

        public static long channel = 0;

        public static void PrintChannelServerAddress(ChannelServerAddress msg, object tag)
        {
            PrintLine("ChannelServerAddress: ChannelID={0} Address={1} Port={2} Key={3}", msg.ChannelID, msg.Address, msg.Port, msg.Key);
            channel = msg.ChannelID;
        }

        public static void PrintSystemMessage(SystemMessage msg, object tag)
        {
            PrintLine("SystemMessage:");
            PrintLine("\tCategory={0}", msg.Category); //System.Byte has a toString()
            PrintLine("\tMessage={0}", msg.Message); //ServiceCore.EndPointNetwork.HeroesString has a toString()
        }

        public static void PrintNpcTalkMessage(NpcTalkMessage msg, object tag)
        {
            PrintLine(ListToString<NpcTalkEntity>(msg.Content, "NpcTalkMessage", 0));
        }
        public static void PrintHousingStartGrantedMessage(HousingStartGrantedMessage msg, object tag)
        {
            if (msg == null)
            {
                PrintLine("HousingStartGrantedMessage: [null]", msg.NewSlot, msg.NewKey);
            }
            else
            {
                PrintLine("HousingStartGrantedMessage: [NewSlot={0} NewKey={1}]", msg.NewSlot, msg.NewKey);
            }
        }

        public static void PrintUpdateStoryGuideMessage(UpdateStoryGuideMessage msg, object tag)
        {
            PrintLine("UpdateStoryGuideMessage:");
            PrintLine("\tTargetLandMark={0}", msg.TargetLandMark); //System.String has a toString()
            PrintLine("\tGuideMessage={0}", msg.GuideMessage); //System.String has a toString()
        }

        public static void PrintAddFriendshipInfoMessage(AddFriendshipInfoMessage msg, object tag)
        {
            PrintLine("AddFriendshipInfoMessage: FriendID={0} FriendLimitCount={1}", msg.FriendID, msg.FriendLimitCount);
        }

        public static void PrintSkillListMessage(SkillListMessage msg, object tag)
        {
            PrintLine("SkillListMessage:");
            PrintLine("\tLearningSkillID={0}",msg.LearningSkillID);
            PrintLine("\tCurrentAp={0}",msg.CurrentAp);
            PrintLine("\tIsResetVocation={0}",msg.IsResetVocation);

            PrintLine("\tSkillList:");
            int i = 0;
            foreach(BriefSkillInfo b in msg.SkillList) {
                PrintLine("\tBriefSkillInfo[{0}]:",i++);
                PrintLine("\t\tSkillID={0}", b.SkillID); //System.String has a toString()
                PrintLine("\t\tBaseRank={0}", b.BaseRank); //System.Int32 has a toString()
                PrintLine("\t\tFinalRank={0}", b.FinalRank); //System.Int32 has a toString()
                PrintLine("\t\tRequiredAP={0}", b.RequiredAP); //System.Int32 has a toString()
                PrintLine("\t\tIsLocked={0}", b.IsLocked); //System.Byte has a toString()
                PrintLine("\t\tCanStartTraining={0}", b.CanStartTraining); //System.Byte has a toString()
                PrintLine("\t\tCurrentAP={0}", b.CurrentAP); //System.Int32 has a toString()
                PrintLine("\t\tResetSkillAP={0}", b.ResetSkillAP); //System.Int32 has a toString()
                PrintLine("\t\tUntrainSkillAP={0}", b.UntrainSkillAP); //System.Int32 has a toString()
                PrintLine("\t\tEnhances:");
                foreach (BriefSkillEnhance e in b.Enhances) {
                    PrintLine("\tBriefSkillEnhance: GroupKey={0} IndexKey={1} Type={2} ReduceDurability={3} MaxDurabilityBonus={4}", e.GroupKey, e.IndexKey, e.Type, e.ReduceDurability, e.MaxDurabilityBonus);
                }
            }
        }

        public static void PrintLoginOkMessage(LoginOkMessage msg, object tag)
        {
            PrintLine("LoginOkMessage:");
            PrintLine("\tRegionCode={0}",msg.RegionCode);
            PrintLine("\tTime={0}",msg.Time);
            PrintLine("\tLimited={0}",msg.Limited);
            PrintLine("\tFacebookToken={0}",msg.FacebookToken);
            PrintLine("\tUserCareType={0}",msg.UserCareType);
            PrintLine("\tUserCareNextState={0}",msg.UserCareNextState);
            PrintLine("\tMapStateInfo={0}",msg.MapStateInfo);
        }

        public static void PrintTodayMissionInitializeMessage(TodayMissionInitializeMessage msg, object tag)
        {
            PrintLine("TodayMissionInitializeMessage:");

            Dictionary<int, TodayMissinState> missionStates = GetPrivateProperty<Dictionary<int, TodayMissinState>>(msg, "MissionState");
            int remainResetMinute = GetPrivateProperty<int>(msg, "RemainResetMinute");

            PrintLine("\tRemainResetMinute={0}", remainResetMinute);
            if (missionStates != null && missionStates.Count != 0)
            {
                PrintLine("\tMissionStates:");
                foreach (KeyValuePair<int, TodayMissinState> t in missionStates)
                {
                    if (t.Value != null)
                    {
                        PrintLine("\t\t{0}=(ID={1} CurrentCount={2} IsFinished={3}", t.Key, t.Value.ID, t.Value.CurrentCount, t.Value.IsFinished);
                    }

                }
            }
        }

        public static void PrintAPMessage(APMessage msg, object tag)
        {
            PrintLine("APMessage:");
            PrintLine("\tAP={0}", msg.AP);
            PrintLine("\tMaxAP={0}", msg.MaxAP);
            PrintLine("\tNextBonusTimeTicks={0}", msg.NextBonusTimeTicks);
            PrintLine("\tAPBonusInterval={0}", msg.APBonusInterval);
        }

        public static void PrintGuildResultMessage(GuildResultMessage msg, object tag)
        {
            PrintLine("GuildResultMessage:");
            PrintLine("\tResult={0}",msg.Result);
            PrintLine("\tArg={0}",msg.Arg);
            PrintLine("\tGuildID={0}",msg.GuildID);
        }

        public static void PrintCostumeUpdateMessage(CostumeUpdateMessage msg, object tag)
        {
            //TODO: db connect
            if (msg.CostumeInfo == null)
            {
                PrintLine("CostumeUpdateMessage:");
            }
            else
            {
                string s = CostumeInfoToString(msg.CostumeInfo, 0, character?.CharacterID, "CostumeUpdateMessage");
                if (s == null || s.Length == 0) {
                    PrintLine("CostumeUpdateMessage:");
                }
                else
                {
                    PrintLine(s);
                }
            }
        }

        private static IDictionary<int, long> lastEquipInfos = null;

        public static void PrintEquipmentInfoMessage(EquipmentInfoMessage msg, object tag)
        {
            PrintLine(DictToString<int, long>(msg.EquipInfos, "EquipmentInfoMessage", 0, lastEquipInfos));
            lastEquipInfos = msg.EquipInfos;
        }

        private static IDictionary<string, int> lastStats = null;
        public static void PrintUpdateStatMessage(UpdateStatMessage msg, object tag)
        {
            string s = DictToString<string, int>(msg.Stat, "UpdateStatMessage", 0, lastStats);
            if (s.Length == 0)
            {
                PrintLine("UpdateStatMessage:");
            }
            else
            {
                PrintLine(s);
            }

            lastStats = msg.Stat;
        }

        private static string SlotInfoToString(SlotInfo i, string name, int numTabs)
        {
            StringBuilder sb = new StringBuilder();
            String t = "";
            if (numTabs != 0)
            {
                t = new string('\t', numTabs);
            }
            sb.Append(t);
            sb.Append(name);
            sb.Append(":");
            t = "\n" + new string('\t', numTabs + 1);

            sb.Append(t);
            sb.Append("ItemClass=");
            sb.Append(i.ItemClass);
            sb.Append(t);
            sb.Append("Quantity=");
            sb.Append(i.Num);
            sb.Append(t);
            sb.Append("Tab=");
            sb.Append(i.Tab);
            sb.Append(" Slot=");
            sb.Append(i.Slot);
            sb.Append(t);
            if (i.IsExpireable)
            {
                //both are -1 if not expirable
                sb.Append("IsExpireable=");
                sb.Append(i.IsExpireable);
                sb.Append(t);
                sb.Append("ExpireDateTimeDiff=");
                sb.Append(i.ExpireDateTimeDiff);
                sb.Append(t);
                sb.Append("ExpireDateTimeTick=");
                sb.Append(i.ExpireDateTimeTick);
                sb.Append(t);
            }
            sb.Append("Durability=");
            sb.Append(i.Durability);
            sb.Append(" ");
            sb.Append("MaxDurability=");
            sb.Append(i.MaxDurability);
            sb.Append(" ");
            sb.Append("MaxDurabilityBonus=");
            sb.Append(i.MaxDurabilityBonus);
            sb.Append(t);
            sb.Append("Tradable=");
            sb.Append(i.Tradable);

            if (i.ExpanderName != null && i.ExpanderName.Length != 0)
            {
                sb.Append(t);
                sb.Append("ExpanderName=");
                sb.Append(i.ExpanderName);
            }

            sb.Append(t);
            sb.Append("Price=");
            sb.Append(i.MinPrice);
            sb.Append("/");
            sb.Append(i.AvgPrice);
            sb.Append("/");
            sb.Append(i.MaxPrice);

            sb.Append(t);
            sb.Append("Colors=(");
            sb.Append(IntToRGB(i.Color1));
            sb.Append(",");
            sb.Append(IntToRGB(i.Color2));
            sb.Append(",");
            sb.Append(IntToRGB(i.Color3));
            sb.Append(")");

            sb.Append(t);
            sb.Append("ItemID=");
            sb.Append(i.ItemID);

            String t2 = t + "\t";
            if (i.Attributes != null && i.Attributes.Count != 0)
            {
                sb.Append(t);
                sb.Append("Attributes:");
                foreach (ItemAttributeElement attr in i.Attributes)
                {
                    sb.Append(t2);
                    sb.Append(attr.AttributeName);
                    sb.Append("=");
                    sb.Append(attr.Value);
                    sb.Append(" Arg=");
                    sb.Append(attr.Arg);
                    sb.Append(" Arg2=");
                    sb.Append(attr.Arg2);
                }
            }
            if (i.PrefixEnchantStatus.Count != 0)
            {
                AppendNotNull(sb, DictToString<int, bool>(i.PrefixEnchantStatus, "PrefixEnchantStatus", numTabs + 1));
            }
            if (i.SuffixEnchantStatus.Count != 0)
            {
                AppendNotNull(sb, DictToString<int, bool>(i.SuffixEnchantStatus, "SuffixEnchantStatus", numTabs + 1));
            }
            return sb.ToString();
        }

        public static bool PropertiesEqual<T>(T self, T to, params string[] ignore) where T : class
        {
            if (self != null && to != null)
            {
                Type type = typeof(T);
                List<string> ignoreList = new List<string>(ignore);
                foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!ignoreList.Contains(pi.Name))
                    {
                        object selfValue = type.GetProperty(pi.Name).GetValue(self, null);
                        object toValue = type.GetProperty(pi.Name).GetValue(to, null);

                        if (selfValue != toValue && (selfValue == null || !selfValue.Equals(toValue)))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return self == to;
        }

        private static bool CompareDictionaries<T1, T2>(IDictionary<T1, T2> d1, IDictionary<T1, T2> d2)
        {
            if (d1 == null || d2 == null)
            {
                return d1 == d2;
            }
            if (d1.Count != d2.Count)
            {
                return false;
            }
            return !d1.Except(d2).Any();
        }

        private static Dictionary<string, SlotInfo> inventoryItems = new Dictionary<string, SlotInfo>();
        public static void PrintUpdateInventoryInfoMessage(UpdateInventoryInfoMessage msg, object tag)
        {
            ICollection<SlotInfo> slotInfos = GetPrivateProperty<ICollection<SlotInfo>>(msg, "slotInfos");
            PrintLine("UpdateInventoryInfoMessage:");

            Dictionary<string, SlotInfo> newInventoryItems = new Dictionary<string, SlotInfo>();
            foreach (SlotInfo slot in slotInfos)
            {
                string slotStr = slot.Slot != -1 ? slot.Slot.ToString() : slot.ItemClass;
                string infoStr = SlotInfoToString(slot, String.Format("SlotInfo[T={0},S={1}]", slot.Tab, slotStr), 1);
                string concated = Regex.Replace(infoStr, @"\s+", "");
                newInventoryItems[concated] = slot;
                if (inventoryItems.ContainsKey(concated))
                {
                    inventoryItems.Remove(concated);
                }
                else
                {
                    //found a new item
                    PrintLine(infoStr);
                }
            }

            inventoryItems = newInventoryItems;
        }

        public static void PrintFriendshipInfoListMessage(FriendshipInfoListMessage msg, object tag)
        {
            PrintLine("FriendshipInfoListMessage: FriendList=[{0}]", String.Join(",", msg.FriendList));
        }

        public static void PrintNpcListMessage(NpcListMessage msg, object tag)
        {
            PrintLine("NpcListMessage:");
            if (msg.Buildings == null)
            {
                return;
            }
            foreach (BuildingInfo b in msg.Buildings)
            {
                PrintLine("\tBuildingID={0} Npcs=[{1}]", b.BuildingID, String.Join(",", b.Npcs));
            }
        }

        public static DateTime CloseDateToDateTime(int closeDate)
        {
            return DateTime.UtcNow.AddSeconds(closeDate);
        }

        public static string DateTimeToString(DateTime d)
        {
            return d.ToString("yyyy-MM-dd hh:mm:ss.fff");
        }

        public static void PrintUseCrateItemResultMessage(UseCrateItemResultMessage msg, object tag)
        {
            PrintLine("UseCrateItemResultMessage:");
            PrintLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.UseCrateItemResultMessage+UseCrateItemResult
            String CrateItem = GetPrivateProperty<String>(msg, "CrateItem");
            PrintLine("\tCrateItem={0}", CrateItem); //System.String has a toString()
            List<string> KeyItems = GetPrivateProperty<List<string>>(msg, "KeyItems");
            PrintLine("\tKeyItems=[{0}]", String.Join(",", KeyItems)); //System.Collections.Generic.List`1[System.String]
        }

        public static void PrintUnregisterNewRecipesMessage(UnregisterNewRecipesMessage msg, object tag)
        {
            PrintLine("UnregisterNewRecipesMessage:");
            PrintLine("\tRecipeList={0}", String.Join(",", msg.RecipeList)); //System.Collections.Generic.ICollection`1[System.String]
        }

        public static string MissionMessageToString(MissionMessage msg, string name, int numTabs)
        {
            StringBuilder sb = new StringBuilder();
            string t = "";
            if (numTabs != 0) {
                t = new string('\t', numTabs);
                sb.Append(t);
            }
            t = "\n\t" + t;

            sb.Append("MissionMessage:");
            sb.Append(t);
            sb.Append("MID");
            sb.Append(msg.MID); //System.String has a toString()
            sb.Append(t);
            sb.Append("ID");
            sb.Append(msg.ID); //System.Int64 has a toString()
            sb.Append(t);
            sb.Append("Category");
            sb.Append(msg.Category); //System.String has a toString()
            sb.Append(t);
            sb.Append("Title");
            sb.Append(msg.Title); //System.String has a toString()
            sb.Append(t);
            sb.Append("Location");
            sb.Append(msg.Location); //System.String has a toString()
            sb.Append(t);
            sb.Append("Description");
            sb.Append(msg.Description); //System.String has a toString()
            sb.Append(t);
            sb.Append("RequiredLevel");
            sb.Append(msg.RequiredLevel); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("RewardAP");
            sb.Append(msg.RewardAP); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("RewardEXP");
            sb.Append(msg.RewardEXP); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("RewardGold");
            sb.Append(msg.RewardGold); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("RewardItemIDs");
            sb.Append(String.Join(",", msg.RewardItemIDs)); //System.Collections.Generic.List`1[System.String]
            sb.Append(t);
            sb.Append("RewardItemNums");
            sb.Append(String.Join(",", msg.RewardItemNums)); //System.Collections.Generic.List`1[System.Int32]
            sb.Append(t);
            sb.Append("ModifiedExpirationTime");
            sb.Append(msg.ModifiedExpirationTime); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("ExpirationTime");
            sb.Append(msg.ExpirationTime); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("ExpirationPeriod");
            sb.Append(msg.ExpirationPeriod); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("Complete");
            sb.Append(msg.Complete); //System.Boolean has a toString()
            sb.Append(t);
            sb.Append("QuestTitle0");
            sb.Append(msg.QuestTitle0); //System.String has a toString()
            sb.Append(t);
            sb.Append("Progress0");
            sb.Append(msg.Progress0); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("TotalProgress0");
            sb.Append(msg.TotalProgress0); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("QuestTitle1");
            sb.Append(msg.QuestTitle1); //System.String has a toString()
            sb.Append(t);
            sb.Append("Progress1");
            sb.Append(msg.Progress1); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("TotalProgress1");
            sb.Append(msg.TotalProgress1); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("QuestTitle2");
            sb.Append(msg.QuestTitle2); //System.String has a toString()
            sb.Append(t);
            sb.Append("Progress2");
            sb.Append(msg.Progress2); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("TotalProgress2");
            sb.Append(msg.TotalProgress2); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("QuestTitle3");
            sb.Append(msg.QuestTitle3); //System.String has a toString()
            sb.Append(t);
            sb.Append("Progress3");
            sb.Append(msg.Progress3); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("TotalProgress3");
            sb.Append(msg.TotalProgress3); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("QuestTitle4");
            sb.Append(msg.QuestTitle4); //System.String has a toString()
            sb.Append(t);
            sb.Append("Progress4");
            sb.Append(msg.Progress4); //System.Int32 has a toString()
            sb.Append(t);
            sb.Append("TotalProgress4");
            sb.Append(msg.TotalProgress4); //System.Int32 has a toString()
            return sb.ToString();
        }

        public static void PrintMissionMessage(MissionMessage msg, object tag)
        {
            PrintLine("MissionMessage:");
            PrintLine("\tMID={0}", msg.MID); //System.String has a toString()
            PrintLine("\tID={0}", msg.ID); //System.Int64 has a toString()
            PrintLine("\tCategory={0}", msg.Category); //System.String has a toString()
            PrintLine("\tTitle={0}", msg.Title); //System.String has a toString()
            PrintLine("\tLocation={0}", msg.Location); //System.String has a toString()
            PrintLine("\tDescription={0}", msg.Description); //System.String has a toString()
            PrintLine("\tRequiredLevel={0}", msg.RequiredLevel); //System.Int32 has a toString()
            PrintLine("\tRewardAP={0}", msg.RewardAP); //System.Int32 has a toString()
            PrintLine("\tRewardEXP={0}", msg.RewardEXP); //System.Int32 has a toString()
            PrintLine("\tRewardGold={0}", msg.RewardGold); //System.Int32 has a toString()
            PrintLine("\tRewardItemIDs=[{0}]",String.Join(",",msg.RewardItemIDs)); //System.Collections.Generic.List`1[System.String]
            PrintLine("\tRewardItemNums=[{0}]", String.Join(",", msg.RewardItemNums)); //System.Collections.Generic.List`1[System.Int32]
            PrintLine("\tModifiedExpirationTime={0}", msg.ModifiedExpirationTime); //System.Int32 has a toString()
            PrintLine("\tExpirationTime={0}", msg.ExpirationTime); //System.Int32 has a toString()
            PrintLine("\tExpirationPeriod={0}", msg.ExpirationPeriod); //System.Int32 has a toString()
            PrintLine("\tComplete={0}", msg.Complete); //System.Boolean has a toString()
            PrintLine("\tQuestTitle0={0}", msg.QuestTitle0); //System.String has a toString()
            PrintLine("\tProgress0={0}", msg.Progress0); //System.Int32 has a toString()
            PrintLine("\tTotalProgress0={0}", msg.TotalProgress0); //System.Int32 has a toString()
            PrintLine("\tQuestTitle1={0}", msg.QuestTitle1); //System.String has a toString()
            PrintLine("\tProgress1={0}", msg.Progress1); //System.Int32 has a toString()
            PrintLine("\tTotalProgress1={0}", msg.TotalProgress1); //System.Int32 has a toString()
            PrintLine("\tQuestTitle2={0}", msg.QuestTitle2); //System.String has a toString()
            PrintLine("\tProgress2={0}", msg.Progress2); //System.Int32 has a toString()
            PrintLine("\tTotalProgress2={0}", msg.TotalProgress2); //System.Int32 has a toString()
            PrintLine("\tQuestTitle3={0}", msg.QuestTitle3); //System.String has a toString()
            PrintLine("\tProgress3={0}", msg.Progress3); //System.Int32 has a toString()
            PrintLine("\tTotalProgress3={0}", msg.TotalProgress3); //System.Int32 has a toString()
            PrintLine("\tQuestTitle4={0}", msg.QuestTitle4); //System.String has a toString()
            PrintLine("\tProgress4={0}", msg.Progress4); //System.Int32 has a toString()
            PrintLine("\tTotalProgress4={0}", msg.TotalProgress4); //System.Int32 has a toString()
        }

        public static void PrintTradeMyItemInfos(TradeMyItemInfos msg, object tag)
        {
            PrintLine("TradeMyItemInfos:");
            int i = 0;
            foreach (TradeItemInfo item in msg.TradeItemList) {
                PrintLine("TradeItemList[{0}]",i++);
                PrintLine(TradeItemInfoToString(item, 1));
            }
            PrintLine("\tresult={0}", msg.result); //System.Int32 has a toString()
        }

        public static string TradeItemInfoToString(TradeItemInfo i, int numTabs)
        {
            StringBuilder sb = new StringBuilder();

            string closeDate = DateTimeToString(CloseDateToDateTime(i.CloseDate));
            if (numTabs != 0)
            {
                sb.Append(new String('\t', numTabs));
            }
            sb.Append("TID=");
            sb.Append(i.TID);
            sb.Append(" CharacterName=");
            sb.Append(i.ChracterName);
            sb.Append(" ItemClass=");
            sb.Append(i.ItemClass);
            sb.Append(" Quantity=");
            sb.Append(i.ItemCount);
            sb.Append(" Price=");
            sb.Append(i.ItemPrice);
            sb.Append(" CloseDate=");
            sb.Append(closeDate);
            return sb.ToString();
        }

        public static void PrintTradeSearchResult(TradeSearchResult msg, object tag)
        {
            //TODO: send to database
            PrintLine("TradeSearchResult:");
            PrintLine("\tUniqueNumber={0}", msg.UniqueNumber);
            PrintLine("\tIsMoreResult={0}", msg.IsMoreResult);
            PrintLine("\tresult={0}", msg.result);
            PrintLine("\tTradeItemList:");
            if (msg.TradeItemList == null)
            {
                return;
            }
            foreach (TradeItemInfo i in msg.TradeItemList)
            {
                string closeDate = DateTimeToString(CloseDateToDateTime(i.CloseDate));
                PrintLine("\t\tTID={0} CID={1} ChracterName={2} ItemClass={3} ItemCount={4} ItemPrice={5} CloseDate={6} HasAttribute={7} MaxArmorCondition={8} color1={9} color2={10} color3={11}", i.TID, i.CID, i.ChracterName, i.ItemClass, i.ItemCount, i.ItemPrice, closeDate, i.HasAttribute, i.MaxArmorCondition, i.color1, i.color2, i.color3);
            }
        }

        public static void PrintAskSecondPasswordMessage(AskSecondPasswordMessage msg, object tag)
        {
            PrintLine("AskSecondPasswordMessage: []");
        }

        public static void PrintNoticeGameEnvironmentMessage(NoticeGameEnvironmentMessage msg, object tag)
        {
            PrintLine("NoticeGameEnvironmentMessage: CafeType={0} IsOTP={1}", msg.CafeType, msg.IsOTP);
        }

        public static void PrintSpSkillMessage(SpSkillMessage msg, object tag)
        {
            PrintLine(DictToString<int, string>(msg.SpSkills, "SpSkillMessage", 0));
        }

        public static void PrintVocationSkillListMessage(VocationSkillListMessage msg, object tag)
        {
            PrintLine(DictToString<string, int>(msg.SkillList, "VocationSkillListMessage", 0));
        }

        public static void PrintWhisperFilterListMessage(WhisperFilterListMessage msg, object tag)
        {
            PrintLine(DictToString<string, int>(msg.Filter, "WhisperFilterListMessage", 0));
        }

        public static void PrintGetCharacterMissionStatusMessage(GetCharacterMissionStatusMessage msg, object tag)
        {
            PrintLine("GetCharacterMissionStatusMessage:");
            PrintLine("\tMissionCompletionCount={0}", msg.MissionCompletionCount);
            PrintLine("\tRemainTimeToCleanMissionCompletionCount={0}", msg.RemainTimeToCleanMissionCompletionCount);
            PrintLine("\tMissionList:");
            foreach (MissionMessage m in msg.MissionList)
            {
                PrintLine("\t\tMID={0} Title={1} Location={2} Description={3}", m.MID, m.Title, m.Location, m.Description);
            }
        }

        public static void PrintSelectPatternMessage(SelectPatternMessage msg, object tag)
        {
            int pattern = GetPrivateProperty<int>(msg, "pattern");
            PrintLine("SelectPatternMessage: pattern={0}", pattern);
        }

        public static void PrintQueryHousingItemsMessage(QueryHousingItemsMessage msg, object tag)
        {
            PrintLine("QueryHousingItemsMessage: []");
        }

        public static void PrintFishingResultMessage(FishingResultMessage msg, object tag)
        {
            PrintLine(ListToString<FishingResultInfo>(msg.FishingResult, "FishingResultMessage", 0));
        }

        public static void PrintPetListMessage(PetListMessage msg, object tag)
        {
            PrintLine("PetListMessage:");
            PrintLine("\tIsTotalPetList={0}", msg.IsTotalPetList);
            PrintLine(ListToString<PetStatusInfo>(msg.PetList, "PetList", 1));
        }

        private static PetFeedListMessage lastPetFeedMsg = null;

        public static void PrintPetFeedListMessage(PetFeedListMessage msg, object tag)
        {
            //check for identical messages before printing again
            if (lastPetFeedMsg != null && msg.IsTotalPetList == lastPetFeedMsg.IsTotalPetList && msg.PetFeedList.Count == lastPetFeedMsg.PetFeedList.Count)
            {
                Dictionary<byte, int> dict = new Dictionary<byte, int>();
                foreach (PetFeedElement e in msg.PetFeedList)
                {
                    dict[e.Type] = e.Count;
                }
                bool exit = true;
                foreach (PetFeedElement e in lastPetFeedMsg.PetFeedList)
                {
                    if (!dict.TryGetValue(e.Type, out int val) || val != e.Count)
                    {
                        exit = false;
                    }
                }
                if (exit)
                {
                    return;
                }
            }
            PrintLine("PetFeedListMessage:");
            PrintLine("\tIsTotalPetList={0}", msg.IsTotalPetList);
            PrintLine(ListToString(msg.PetFeedList, "PetFeedList", 1));
            lastPetFeedMsg = msg;
        }

        public static void PrintUpdateStorageInfoMessage(UpdateStorageInfoMessage msg, object tag)
        {
            PrintLine("UpdateStorageInfoMessage:");
            foreach (StorageInfo info in msg.StorageInfos)
            {
                PrintLine("\tstorageID={0} isAvailable={1} storageName={2} storageTag={3}", info.StorageID, info.IsAvailable, info.StorageName, info.StorageTag);
            }
        }

        public static void PrintReservedInfoMessage(ReservedInfoMessage msg, object tag)
        {
            PrintLine("ReservedInfoMessage:");
            PrintLine("\tReservedName=[{0}]",string.Join(",",msg.ReservedName)); //System.Collections.Generic.ICollection`1[System.String]
            PrintLine("\tReservedTitle=[{0}]",string.Join(",",msg.ReservedTitle)); //System.Collections.Generic.ICollection`1[System.Int32]
        }

        public static void PrintPvpGameHostedMessage(PvpGameHostedMessage msg, object tag)
        {
            PrintLine("PvpGameHostedMessage:");
            PrintLine("\tGameInfo={0}", msg.GameInfo); //ServiceCore.EndPointNetwork.PvpGameInfo has a toString()
            PrintLine("\tHostInfo={0}", msg.HostInfo); //ServiceCore.EndPointNetwork.GameJoinMemberInfo has a toString()
            PrintLine("\tTeamID={0}", msg.TeamID); //System.Int32 has a toString()
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            PrintLine(DictToString(msg.Config, "Config", 1)); //System.Collections.Generic.Dictionary`2[System.String,System.String]
        }

        public static void PrintRepairItemMessage(RepairItemMessage msg, object tag)
        {
            PrintLine("RepairItemMessage:");
            PrintLine("\tItemIDs=[{0}]",String.Join(",",msg.ItemIDs)); //System.Collections.Generic.ICollection`1[System.Int64]
            PrintLine("\tAddAllEquippedItems={0}", msg.AddAllEquippedItems); //System.Boolean has a toString()
            PrintLine("\tAddAllBrokenItems={0}", msg.AddAllBrokenItems); //System.Boolean has a toString()
            PrintLine("\tAddAllRepairableItems={0}", msg.AddAllRepairableItems); //System.Boolean has a toString()
            PrintLine("\tPrice={0}", msg.Price); //System.Int32 has a toString()
        }

        public static void PrintRegisterNewRecipesMessage(RegisterNewRecipesMessage msg, object tag)
        {
            PrintLine(DictToString<string, long>(msg.RecipeList, "RegisterNewRecipesMessage", 0));
        }

        public static void PrintNewRecipesMessage(NewRecipesMessage msg, object tag)
        {
            PrintLine(DictToString<string, long>(msg.RecipeList, "NewRecipesMessage", 0));
        }

        public static void PrintGoddessProtectionMessage(GoddessProtectionMessage msg, object tag)
        {
            PrintLine("GoddessProtectionMessage:");
            PrintLine("\tCaster={0}", msg.Caster); //System.Int32 has a toString()
            PrintLine("\tRevived={0}",String.Join(",",msg.Revived)); //System.Collections.Generic.List`1[System.Int32]
        }

        public static void PrintRewardMissionSuccessMessage(RewardMissionSuccessMessage msg, object tag)
        {
            PrintLine("RewardMissionSuccessMessage:");
            PrintLine("\tID={0}", msg.ID); //System.Int64 has a toString()
            PrintLine("\tRewardAP={0}", msg.RewardAP); //System.Int32 has a toString()
            PrintLine("\tRewardEXP={0}", msg.RewardEXP); //System.Int32 has a toString()
            PrintLine("\tRewardGold={0}", msg.RewardGold); //System.Int32 has a toString()
            PrintLine("\tRewardItemIDs=[{0}]",string.Join(",",msg.RewardItemIDs)); //System.Collections.Generic.List`1[System.String]
            PrintLine("\tRewardItemNums=[{0}]",string.Join(",",msg.RewardItemNums)); //System.Collections.Generic.List`1[System.Int32]
        }

        public static void PrintSharedInventoryInfoMessage(SharedInventoryInfoMessage msg, object tag)
        {
            PrintLine("SharedInventoryInfoMessage:");
            if (msg.StorageInfos != null && msg.StorageInfos.Count != 0)
            {
                PrintLine("\tStorageInfos:");
                foreach (StorageInfo info in msg.StorageInfos)
                {
                    PrintLine("\t\tstorageID={0} isAvailable={1} storageName={2} storageTag={3}", info.StorageID, info.IsAvailable, info.StorageName, info.StorageTag);
                }
            }
            if (msg.SlotInfos != null && msg.SlotInfos.Count != 0)
            {
                int i = 0;
                foreach (SlotInfo info in msg.SlotInfos)
                {
                    PrintLine(SlotInfoToString(info, String.Format("SlotInfos[{0}]", i++), 1));
                }
            }
        }

        public static void PrintTirCoinInfoMessage(TirCoinInfoMessage msg, object tag)
        {
			if(msg.TirCoinInfo == null || msg.TirCoinInfo.Count == 0) {
				PrintLine("TirCoinInfoMessage: []");
			}
            else if (msg.TirCoinInfo.ContainsKey(1))
            {
                PrintLine("TirCoinInfoMessage: Quantity={0}", msg.TirCoinInfo[1]);
            }
            else
            {
                PrintLine(DictToString<byte, int>(msg.TirCoinInfo, "TirCoinInfoMessage", 0));
            }
        }

        public static void PrintRankAlarmInfoMessage(RankAlarmInfoMessage msg, object tag)
        {
            PrintLine(ListToString<RankAlarmInfo>(msg.RankAlarm, "RankAlarmInfoMessage", 0));
        }

        private static string ConsumablesInfoToString(ConsumablesInfo c)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ItemClass={0} BringNum={1} UsedNum={2} DraftNum={3} innerConsumables=[", c.ItemClass, c.BringNum, c.Usednum, c.DraftNum);
            foreach (ConsumablesInfo info in c.InnerConsumables)
            {
                sb.Append(info.ItemClass);
                sb.Append(",");
            }
            if (c.InnerConsumables.Count != 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static void PrintUpdateBattleInventoryMessage(UpdateBattleInventoryMessage msg, object tag)
        {
            PrintLine("UpdateBattleInventoryMessage:");
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            PrintLine("\tUID={0}", msg.UID); //System.Int32 has a toString()
            PrintLine(BattleInventoryToString(msg.BattleInventory, "BattleInventory", 1)); //ServiceCore.MicroPlayServiceOperations.BattleInventory
        }

        private static string BattleInventoryToString(BattleInventory b, string name, int numTabs)
        {
            StringBuilder sb = new StringBuilder();
            String t = "";
            if (numTabs != 0)
            {
                t = new string('\t', numTabs);
            }
            sb.Append(t);
            sb.Append(name);
            sb.Append(":");
            t = "\n" + new string('\t', numTabs);

            if (b.UsedList != null && b.UsedList.Count != 0)
            {
                sb.Append(t);
                sb.Append("UsedList:");
                foreach (KeyValuePair<string, ConsumablesInfo> entry in b.UsedList)
                {
                    sb.Append(t);
                    sb.Append("\t");
                    sb.Append(entry.Key);
                    sb.Append("=");
                    sb.Append(ConsumablesInfoToString(entry.Value));
                }
            }
            if (b.Consumables != null && b.Consumables.Count != 0)
            {
                sb.Append(t);
                sb.Append("Consumables:");
                foreach (KeyValuePair<int, ConsumablesInfo> entry in b.Consumables)
                {
                    sb.Append(t);
                    sb.Append("\t");
                    sb.Append(entry.Key);
                    sb.Append("=");
                    sb.Append(ConsumablesInfoToString(entry.Value));
                }
            }
            return sb.ToString();
        }

        public static void PrintUpdateBattleInventoryInTownMessage(UpdateBattleInventoryInTownMessage msg, object tag)
        {
            PrintLine(BattleInventoryToString(msg.BattleInventory, "UpdateBattleInventoryInTownMessage", 0));
        }

        public static void PrintBingoBoardResultMessage(BingoBoardResultMessage msg, object tag)
        {
            PrintLine("BingoBoardResultMessage:");
            PrintLine("\tResult={0}", ((BingoBoardResultMessage.Bingo_Result)msg.Result).ToString());
            PrintLine("\tBingoBoardNumbers=[{0}]", String.Join(",", msg.BingoBoardNumbers));
        }

        public static void PrintJoinHousingMessage(JoinHousingMessage msg, object tag)
        {
            PrintLine("JoinHousingMessage: [TargetID={0}]", msg.TargetID);
        }

        public static void PrintAttendanceInfoMessage(AttendanceInfoMessage msg, object tag)
        {
            //TODO: db connect
            PrintLine("AttendanceInfoMessage:");
            PrintLine("\tEventType={0}", msg.EventType);
            PrintLine("\tCurrentVersion={0}", msg.CurrentVersion);
            PrintLine("\tPeriodText={0}", msg.PeriodText);
            if (msg.AttendanceInfo != null && msg.AttendanceInfo.Count != 0)
            {
                PrintLine("\tAttendanceInfo:");
                foreach (AttendanceDayInfo info in msg.AttendanceInfo)
                {
                    PrintLine("\t\tday={0} isCompleted={1} completedRewardsIndex={2}", info.day, info.isCompleted, info.completedRewardIndex);
                }
            }

            if (msg.BonusRewardInfo != null && msg.BonusRewardInfo.Count != 0)
            {
                PrintLine("\tBonusRewardInfo:");
                foreach (AttendanceDayInfo info in msg.BonusRewardInfo)
                {
                    PrintLine("\t\tday={0} isCompleted={1} completedRewardsIndex={2}", info.day, info.isCompleted, info.completedRewardIndex);
                }
            }

        }
        public static void PrintUpdateTitleMessage(UpdateTitleMessage msg, object tag)
        {
            PrintLine(ListToString<TitleSlotInfo>(msg.Titles, "UpdateTitleMessage", 0));
        }

        public static void PrintGuildInventoryInfoMessage(GuildInventoryInfoMessage msg, object tag)
        {
            PrintLine("GuildInventoryInfoMessage:");
            PrintLine("\tIsEnabled={0}", msg.IsEnabled);
            PrintLine("\tStorageCount={0}", msg.StorageCount);
            PrintLine("\tGoldLimit={0}", msg.GoldLimit);
            PrintLine("\tAccessLimtiTag={0}", msg.AccessLimtiTag);
            int i = 0;
            foreach (SlotInfo slot in msg.SlotInfos)
            {
                PrintLine(SlotInfoToString(slot, String.Format("SlotInfo[{0}]", i++), 1));
            }
        }

        public static void PrintCashshopTirCoinResultMessage(CashshopTirCoinResultMessage msg, object tag)
        {
            PrintLine("CashshopTirCoinResultMessage:");
            PrintLine("\tIsSuccess={0}", msg.isSuccess);
            PrintLine("\tisBeautyShop={0}", msg.isBeautyShop);
            PrintLine("\tsuccessCount={0}", msg.successCount);
            PrintLine("\tIgnoreItems:");
            foreach (TirCoinIgnoreItemInfo item in msg.IgnoreItems)
            {
                PrintLine("\t\tItemClass={0} Amount={1} Duration={2} Price={3}", item.ItemClass, item.Amount, item.Duration, item.Price);
            }
        }

        public static void PrintGiveCashShopDiscountCouponMessage(GiveCashShopDiscountCouponMessage msg, object tag)
        {
            PrintLine("GiveCashShopDiscountCouponMessage: CouponCode={0}", msg.CouponCode);
        }

        public static void PrintNextSectorMessage(NextSectorMessage msg, object tag)
        {
            PrintLine("NextSectorMessage: OnSectorStart={0}", msg.OnSectorStart);
        }

        public static void PrintBurnGauge(BurnGauge msg, object tag)
        {
            //TODO: add db connect
            PrintLine("BurnGauge:");
            PrintLine("\tGauge={0}", msg.Gauge);
            PrintLine("\tJackpotStartGauge={0}", msg.JackpotStartGauge);
            PrintLine("\tJackpotMaxGauge={0}", msg.JackpotMaxGauge);
        }

        public static void PrintStoryLinesMessage(StoryLinesMessage msg, object tag)
        {
            PrintLine("StoryLinesMessage:");
            foreach (BriefStoryLineInfo info in msg.StoryStatus)
            {
                PrintLine("\tStoryLine={0} Phase={1} Status={2} PhaseText={3}", info.StoryLine, info.Phase, info.Status, info.PhaseText);
            }
        }

        public static void PrintQuickSlotInfoMessage(QuickSlotInfoMessage msg, object tag)
        {
            QuickSlotInfo info = GetPrivateProperty<QuickSlotInfo>(msg, "info");
            PrintLine(QuickSlotInfoToString(info, "QuickSlotInfoMessage", 0));
        }

        public static void PrintGuildInfoMessage(GuildInfoMessage msg, object tag)
        {
            PrintLine("GuildInfoMessage:");
            if (msg.GuildInfo != null) {
                InGameGuildInfo g = msg.GuildInfo;
                PrintLine("\tGuildSN={0}", g.GuildSN); //System.Int32 has a toString()
                PrintLine("\tGuildName={0}", g.GuildName); //System.String has a toString()
                PrintLine("\tGuildLevel={0}", g.GuildLevel); //System.Int32 has a toString()
                PrintLine("\tMemberCount={0}", g.MemberCount); //System.Int32 has a toString()
                PrintLine("\tMasterName={0}", g.MasterName); //System.String has a toString()
                PrintLine("\tMaxMemberCount={0}", g.MaxMemberCount); //System.Int32 has a toString()
                PrintLine("\tIsNewbieRecommend={0}", g.IsNewbieRecommend); //System.Boolean has a toString()
                PrintLine("\tGuildPoint={0}", g.GuildPoint); //System.Int64 has a toString()
                PrintLine("\tGuildNotice={0}", g.GuildNotice); //System.String has a toString()
                PrintLine(DictToString<byte, int>(g.DailyGainGP, "DailyGainGP", 1)); //System.Collections.Generic.Dictionary`2[System.Byte,System.Int32]
            }
            //TODO: db connect
        }

        public static void PrintNotifyLook(NotifyLook msg, object tag)
        {
            //TODO: db connect
            PrintLine("NotifyLook:");
            PrintLine("\tID={0}", msg.ID);
            if (MongoDBConnect.connection != null)
            {
                MongoDBConnect.connection.InsertCharacterSummary(msg.Look);
            }
            PrintLine(CharacterSummaryToString(msg.Look, "Look", 1));
        }

        public static void PrintQueryCashShopProductListMessage(QueryCashShopProductListMessage msg, object tag)
        {
            PrintLine("QueryCashShopProductListMessage: []");
        }

        public static void PrintQueryCashShopBalanceMessage(QueryCashShopBalanceMessage msg, object tag)
        {
            PrintLine("QueryCashShopBalanceMessage: []");
        }

        public static void PrintSecondPasswordMessage(SecondPasswordMessage msg, object tag)
        {
            //TODO: hide
            PrintLine("SecondPasswordMessage: Password={0}", msg.Password);
        }

        private static CharacterSummary character = null;

        public static void PrintSelectCharacterMessage(SelectCharacterMessage msg, object tag)
        {
            character = characters[msg.Index];
            PrintLine("SelectCharacterMessage: index={0}",msg.Index);
        }

        private static string GameInfoToString(GameInfo g, string name, int numTabs) {
            StringBuilder sb = new StringBuilder();
            string t = "";
            if (numTabs > 0) {
                t = new string('\t', numTabs);
                sb.Append(t);
            }
            sb.Append(name);
            sb.Append(":");
            t = "\n" + new string('\t', numTabs + 1);
            sb.Append("Name=");
            sb.Append(g.Name);
            sb.Append(t);
            sb.Append("Map=");
            sb.Append(g.Map);
            sb.Append(t);
            sb.Append("GameDir=");
            sb.Append(g.GameDir);
            sb.Append(t);
            sb.Append("Description=");
            sb.Append(g.Description);
            sb.Append(t);
            sb.Append("HostID=");
            sb.Append(g.HostID);
            sb.Append("DSIP=");
            sb.Append(g.DSIP);
            sb.Append(t);
            sb.Append("DSPort=");
            sb.Append(g.DSPort);
            return sb.ToString();
        }

        public static void PrintRegisterServerMessage(RegisterServerMessage msg, object tag)
        {
            //TODO: proudnet stuff from this message
            PrintLine(GameInfoToString(msg.TheInfo, "RegisterServerMessage", 0));
        }
        public static void PrintQueryQuestProgressMessage(QueryQuestProgressMessage msg, object tag)
        {
            PrintLine("QueryQuestProgressMessage: []");
        }

        public static void PrintHousingHostInfoMessage(HousingHostInfoMessage msg, object tag)
        {
            PrintLine("HousingHostInfoMessage:");
            PrintLine(GameInfoToString(msg.GameInfo, "GameInfo", 1)); //ServiceCore.EndPointNetwork.GameInfo has a toString()
            PrintLine("\tMemberInfo:");
            PrintLine(GameJoinMemberInfoToString(msg.MemberInfo, 2)); //ServiceCore.EndPointNetwork.GameJoinMemberInfo has a toString()
        }


        public static void PrintRequestGemstoneRollbackMessage(RequestGemstoneRollbackMessage msg, object tag)
        {
            PrintLine("RequestGemstoneRollbackMessage:");
            PrintLine("\tBraceletItemID={0}", msg.BraceletItemID); //System.Int64 has a toString()
        }

        public static void PrintRequestMarbleInfoMessage(RequestMarbleInfoMessage msg, object tag)
        {
            PrintLine("RequestMarbleInfoMessage:");
        }

        public static void PrintMarbleCastDiceResultMessage(MarbleCastDiceResultMessage msg, object tag)
        {
            PrintLine("MarbleCastDiceResultMessage:");
            PrintLine("\tFaceNumber={0}", msg.FaceNumber); //System.Int32 has a toString()
            PrintLine("\tNextNodeIndex={0}", msg.NextNodeIndex); //System.Int32 has a toString()
            PrintLine("\tIsChance={0}", msg.IsChance); //System.Boolean has a toString()
        }

        public static void PrintRequestMarbleProcessChanceRoadMessage(RequestMarbleProcessChanceRoadMessage msg, object tag)
        {
            PrintLine("RequestMarbleProcessChanceRoadMessage:");
        }

        public static void PrintMarbleResultMessage(MarbleResultMessage msg, object tag)
        {
            PrintLine("MarbleResultMessage:");
            PrintLine("\tResultType={0}", msg.ResultType); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.String has a toString()
        }

        public static void PrintMarbleGetItemMessage(MarbleGetItemMessage msg, object tag)
        {
            PrintLine("MarbleGetItemMessage:");
            PrintLine("\tItemClassEx={0}", msg.ItemClassEx); //System.String has a toString()
            PrintLine("\tItemCount={0}", msg.ItemCount); //System.Int32 has a toString()
            PrintLine("\tType={0}", msg.Type); //System.String has a toString()
        }

        public static void PrintInsertBlessStoneMessage(InsertBlessStoneMessage msg, object tag)
        {
            PrintLine("InsertBlessStoneMessage:");
            PrintLine("\tStoneType={0}", msg.StoneType); //ServiceCore.EndPointNetwork.BlessStoneType
            PrintLine("\tIsInsert={0}", msg.IsInsert); //System.Boolean has a toString()
            PrintLine("\tRemainFatigue={0}", msg.RemainFatigue); //System.Int32 has a toString()
        }

        public static void PrintAddSharedItemMessage(AddSharedItemMessage msg, object tag)
        {
            PrintLine("AddSharedItemMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tAmount={0}", msg.Amount); //System.Int32 has a toString()
            PrintLine("\tTargetTab={0}", msg.TargetTab); //System.Byte has a toString()
            PrintLine("\tTargetSlot={0}", msg.TargetSlot); //System.Int32 has a toString()
        }

        public static void PrintAllUserGoalRewardMessage(AllUserGoalRewardMessage msg, object tag)
        {
            PrintLine("AllUserGoalRewardMessage:");
            PrintLine("\tGoalID={0}", msg.GoalID); //System.Int32 has a toString()
        }

        public static void PrintAltarStatusEffectMessage(AltarStatusEffectMessage msg, object tag)
        {
            PrintLine("AltarStatusEffectMessage:");
            PrintLine("\tType={0}", msg.Type); //System.Int32 has a toString()
        }

        public static void PrintAvatarSynthesisResultMessage(AvatarSynthesisResultMessage msg, object tag)
        {
            PrintLine("AvatarSynthesisResultMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tResultFlag={0}", msg.ResultFlag); //System.Int32 has a toString()
            PrintLine("\tAttribute={0}", msg.Attribute); //System.String has a toString()
        }

        public static void PrintCIDByNameMessage(CIDByNameMessage msg, object tag)
        {
            PrintLine("CIDByNameMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tName={0}", msg.Name); //System.String has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
            PrintLine("\tIsEqualAccount={0}", msg.IsEqualAccount); //System.Boolean has a toString()
            PrintLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            PrintLine("\tCharacterClass={0}", msg.CharacterClass); //System.Int32 has a toString()
        }

        public static void PrintCounterEventMessage(CounterEventMessage msg, object tag)
        {
            PrintLine("CounterEventMessage:");
            PrintLine("\tTotalCount={0}", msg.TotalCount); //System.Int32 has a toString()
            PrintLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            PrintLine("\tEventName={0}", msg.EventName); //System.String has a toString()
        }

        public static void PrintDecomposeItemMessage(DecomposeItemMessage msg, object tag)
        {
            PrintLine("DecomposeItemMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
        }

        public static void PrintUseFreeTitleMessage(UseFreeTitleMessage msg, object tag)
        {
            PrintLine("UseFreeTitleMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tFreeTitleName={0}", msg.FreeTitleName); //System.String has a toString()
        }

        public static void PrintGetFriendshipInfoMessage(GetFriendshipInfoMessage msg, object tag)
        {
            PrintLine("GetFriendshipInfoMessage:");
        }

        public static void PrintDeleteFriendshipInfoMessage(DeleteFriendshipInfoMessage msg, object tag)
        {
            PrintLine("DeleteFriendshipInfoMessage:");
            PrintLine("\tFriendID={0}", msg.FriendID); //System.Int32 has a toString()
        }

        public static void PrintUpdateFriendshipPointMessage(UpdateFriendshipPointMessage msg, object tag)
        {
            PrintLine("UpdateFriendshipPointMessage:");
            PrintLine("\tPoint={0}", msg.Point); //System.Int32 has a toString()
        }

        public static void PrintAddFriendShipMessage(AddFriendShipMessage msg, object tag)
        {
            PrintLine("AddFriendShipMessage:");
            PrintLine("\tCharacterName={0}", msg.CharacterName); //System.String has a toString()
        }

        public static void PrintGatheringMessage(GatheringMessage msg, object tag)
        {
            PrintLine("GatheringMessage:");
            PrintLine("\tEntityName={0}", msg.EntityName); //System.String has a toString()
            PrintLine("\tGatherTag={0}", msg.GatherTag); //System.Int32 has a toString()
        }

        public static void PrintGameResourceRequestMessage(GameResourceRequestMessage msg, object tag)
        {
            PrintLine("GameResourceRequestMessage:");
            PrintLine("\tRequestType={0}", msg.RequestType); //System.Int32 has a toString()
            PrintLine("\tRequestParam={0}", msg.RequestParam); //System.String has a toString()
        }

        public static void PrintDirectPurchaseGuildItemMessage(DirectPurchaseGuildItemMessage msg, object tag)
        {
            PrintLine("DirectPurchaseGuildItemMessage:");
            PrintLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
            PrintLine("\tIsCredit={0}", msg.IsCredit); //System.Boolean has a toString()
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
        }

        public static void PrintDirectPurchaseGuildItemResultMessage(DirectPurchaseGuildItemResultMessage msg, object tag)
        {
            PrintLine("DirectPurchaseGuildItemResultMessage:");
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
        }

        public static void PrintFindNewbieRecommendGuildResultMessage(FindNewbieRecommendGuildResultMessage msg, object tag)
        {
            PrintLine("FindNewbieRecommendGuildResultMessage:");
            PrintLine("\tinfo={0}", msg.info); //ServiceCore.EndPointNetwork.GuildService.InGameGuildInfo has a toString()
        }

        public static void PrintGuildChatMemberInfoMessage(GuildChatMemberInfoMessage msg, object tag)
        {
            PrintLine("GuildChatMemberInfoMessage:");
            PrintLine("\tSender={0}", msg.Sender); //System.String has a toString()
            PrintLine("\tIsOnline={0}", msg.IsOnline); //System.Boolean has a toString()
        }

        public static void PrintAddGuildStorageItemMessage(AddGuildStorageItemMessage msg, object tag)
        {
            PrintLine("AddGuildStorageItemMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            PrintLine("\tAmount={0}", msg.Amount); //System.Int32 has a toString()
            PrintLine("\tTargetTab={0}", msg.TargetTab); //System.Byte has a toString()
            PrintLine("\tTargetSlot={0}", msg.TargetSlot); //System.Int32 has a toString()
        }

        public static void PrintPickGuildStorageItemMessage(PickGuildStorageItemMessage msg, object tag)
        {
            PrintLine("PickGuildStorageItemMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            PrintLine("\tAmount={0}", msg.Amount); //System.Int32 has a toString()
            PrintLine("\tTargetTab={0}", msg.TargetTab); //System.Byte has a toString()
            PrintLine("\tTargetSlot={0}", msg.TargetSlot); //System.Int32 has a toString()
        }

        public static void PrintSetFacebookAccessTokenMessage(SetFacebookAccessTokenMessage msg, object tag)
        {
            PrintLine("SetFacebookAccessTokenMessage:");
            PrintLine("\tAccessToken={0}", msg.AccessToken); //System.String has a toString()
        }

        public static void PrintNoticeNewbieRecommendMessage(NoticeNewbieRecommendMessage msg, object tag)
        {
            PrintLine("NoticeNewbieRecommendMessage:");
            PrintLine("\tRequestUserName={0}", msg.RequestUserName); //System.String has a toString()
            PrintLine("\tShipID={0}", msg.ShipID); //System.Int64 has a toString()
        }

        public static void PrintRequestNewbieRecommendGuildMessage(RequestNewbieRecommendGuildMessage msg, object tag)
        {
            PrintLine("RequestNewbieRecommendGuildMessage:");
            PrintLine("\tGuildSN={0}", msg.GuildSN); //System.Int32 has a toString()
        }

        public static void PrintQueryHousingListMessage(QueryHousingListMessage msg, object tag)
        {
            PrintLine("QueryHousingListMessage:");
        }

        public static void PrintQueryHousingPropsMessage(QueryHousingPropsMessage msg, object tag)
        {
            PrintLine("QueryHousingPropsMessage:");
        }

        public static void PrintPurchaseGuildStorageMessage(PurchaseGuildStorageMessage msg, object tag)
        {
            PrintLine("PurchaseGuildStorageMessage:");
            PrintLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
            PrintLine("\tIsCredit={0}", msg.IsCredit); //System.Boolean has a toString()
        }

        public static void PrintArrangeGuildStorageItemMessage(ArrangeGuildStorageItemMessage msg, object tag)
        {
            PrintLine("ArrangeGuildStorageItemMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tSlot={0}", msg.Slot); //System.Int32 has a toString()
        }

        public static void PrintHandleGuildStorageSessionMessage(HandleGuildStorageSessionMessage msg, object tag)
        {
            PrintLine("HandleGuildStorageSessionMessage:");
            PrintLine("\tIsStarted={0}", msg.IsStarted); //System.Boolean has a toString()
        }

        public static void PrintUpdateGuildStorageSettingMessage(UpdateGuildStorageSettingMessage msg, object tag)
        {
            PrintLine("UpdateGuildStorageSettingMessage:");
            PrintLine("\tGoldLimit={0}", msg.GoldLimit); //System.Int32 has a toString()
            PrintLine("\tAccessLimtiTag={0}", msg.AccessLimtiTag); //System.Int64 has a toString()
        }

        public static void PrintNotifyEnhanceMessage(NotifyEnhanceMessage msg, object tag)
        {
            PrintLine("NotifyEnhanceMessage:");
            PrintLine("\tcharacterName={0}", msg.characterName); //System.String has a toString()
            PrintLine("\tisSuccess={0}", msg.isSuccess); //System.Boolean has a toString()
            PrintLine("\tnextEnhanceLevel={0}", msg.nextEnhanceLevel); //System.Int32 has a toString()
            PrintLine("\titem={0}", msg.item); //ServiceCore.EndPointNetwork.TooltipItemInfo has a toString()
        }

        public static void PrintItemCombinationResultMessage(ItemCombinationResultMessage msg, object tag)
        {
            PrintLine("ItemCombinationResultMessage:");
            PrintLine("\tResultCode={0}", msg.ResultCode); //System.Int32 has a toString()
            PrintLine("\tResultMessage={0}", msg.ResultMessage); //System.String has a toString()
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            PrintLine("\tColor1={0}", msg.Color1); //System.Int32 has a toString()
            PrintLine("\tColor2={0}", msg.Color2); //System.Int32 has a toString()
            PrintLine("\tColor3={0}", msg.Color3); //System.Int32 has a toString()
        }

        public static void PrintHotSpringRequestInfoResultChannelMessage(HotSpringRequestInfoResultChannelMessage msg, object tag)
        {
            PrintLine("HotSpringRequestInfoResultChannelMessage:");
            PrintLine(ListToString<HotSpringPotionEffectInfo>(msg.HotSpringPotionEffectInfos, "HotSpringPotionEffectInfos",1)); //System.Collections.Generic.ICollection`1[ServiceCore.EndPointNetwork.HotSpringPotionEffectInfo]
            PrintLine("\tTownID={0}", msg.TownID); //System.Int32 has a toString()
        }

        public static void PrintUpdateFlexibleMapLoadingInfoMessage(UpdateFlexibleMapLoadingInfoMessage msg, object tag)
        {
            byte mapState = GetPrivateProperty<byte>(msg, "mapState");
            string regionName = GetPrivateProperty<string>(msg, "regionName");
            PrintLine("UpdateFlexibleMapLoadingInfoMessage:");
            PrintLine("\tMapState={0}",mapState);
            PrintLine("\tregionName={0}",regionName);
        }

        public static void PrintHotSpringPotionEffectInfo(HotSpringPotionEffectInfo msg, object tag)
        {
            PrintLine("HotSpringPotionEffectInfo:");
            PrintLine("\tPotionItemClass={0}", msg.PotionItemClass); //System.String has a toString()
            PrintLine("\tCharacterName={0}", msg.CharacterName); //System.String has a toString()
            PrintLine("\tGuildName={0}", msg.GuildName); //System.String has a toString()
            PrintLine("\tExpiredTime={0}", msg.ExpiredTime); //System.Int32 has a toString()
            PrintLine("\tOtherPotionUsableTime={0}", msg.OtherPotionUsableTime); //System.Int32 has a toString()
        }

        public static void PrintChangedCashShopMessage(ChangedCashShopMessage msg, object tag)
        {
            PrintLine("ChangedCashShopMessage: []");
        }

        public static void PrintNotifyBurnMessage(NotifyBurnMessage msg, object tag)
        {
            PrintLine("NotifyBurnMessage:");
            PrintLine("\tMessage={0}", msg.Message); //ServiceCore.EndPointNetwork.HeroesString has a toString()
            PrintLine("\tIsTelepathyEnable={0}", msg.IsTelepathyEnable); //System.Boolean has a toString()
            PrintLine("\tIsUIEnable={0}", msg.IsUIEnable); //System.Boolean has a toString()
        }

        public static void PrintNotifyRandomItemMessage(NotifyRandomItemMessage msg, object tag)
        {
            PrintLine("NotifyRandomItemMessage:");
            PrintLine("\tMessage={0}", msg.Message); //ServiceCore.EndPointNetwork.HeroesString has a toString()
            PrintLine("\tIsTelepathyEnable={0}", msg.IsTelepathyEnable); //System.Boolean has a toString()
            PrintLine("\tIsUIEnable={0}", msg.IsUIEnable); //System.Boolean has a toString()
        }

        public static void PrintNGSecurityMessage(NGSecurityMessage msg, object tag)
        {
            PrintLine("NGSecurityMessage:");
            PrintLine("\tmessage={0}",BitConverter.ToString(msg.message)); //System.Byte[]
            PrintLine("\tcheckSum={0}", msg.checkSum); //System.UInt64 has a toString()
        }

        public static void PrintQueryInventoryMessage(QueryInventoryMessage msg, object tag)
        {
            PrintLine("QueryInventoryMessage: []");
        }

        public static void PrintReturnToTownMessage(ReturnToTownMessage msg, object tag)
        {
            PrintLine("ReturnToTownMessage: []");
        }

        public static void PrintLeavePartyMessage(LeavePartyMessage msg, object tag)
        {
            PrintLine("LeavePartyMessage: []");
        }

        public static void PrintFindNewbieRecommendGuildMessage(FindNewbieRecommendGuildMessage msg, object tag)
        {
            PrintLine("FindNewbieRecommendGuildMessage: []");
        }

        public static void PrintPropBrokenMessage(PropBrokenMessage msg, object tag)
        {
            PrintLine("PropBrokenMessage:");
            PrintLine("\tBrokenProp={0}",msg.BrokenProp);
            PrintLine("\tEntityName={0}",msg.EntityName);
            PrintLine("\tAttacker={0}",msg.Attacker);
        }

        public static void PrintSectorPropListMessage(SectorPropListMessage msg, object tag)
        {
            if (msg == null || msg.Props.Count == 0)
            {
                PrintLine("SectorPropListMessage: []");
                return;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("SectorPropListMessage: [");
            foreach (KeyValuePair<int, int> entry in msg.Props)
            {
                sb.Append(entry.Key);
                sb.Append("=");
                sb.Append(entry.Value);
                sb.Append(",");
            }

            sb.Remove(sb.Length - 1, 1);
            sb.Append("]");
            PrintLine(sb.ToString());
        }

        public static void PrintMoveToNextSectorMessage(MoveToNextSectorMessage msg, object tag)
        {
            PrintLine("MoveToNextSectorMessage:");
            PrintLine("\tTriggerName={0}", msg.TriggerName);
            PrintLine("\tTargetGroup={0}", msg.TargetGroup);
            PrintLine("\tHolyProps=[{0}]", String.Join(",", msg.HolyProps));
        }
        public static void PrintStartGameMessage(StartGameMessage msg, object tag)
        {
            PrintLine("StartGameMessage: []");
        }


        private static string ShipOptionInfoToString(ShipOptionInfo s, int numTabs)
        {
            string t = "";
            if (numTabs > 0) {
                t = new string('\t', numTabs);
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(t);
            t = "\n" + t;
            sb.Append("MaxMemberCount=");
            sb.Append(s.MaxMemberCount);
            sb.Append(t);
            sb.Append("SwearMemberLimit=");
            sb.Append(s.SwearMemberLimit);
            sb.Append(t);
            sb.Append("UntilForceStart=");
            sb.Append(s.UntilForceStart);
            sb.Append(t);
            sb.Append("MinLevel=");
            sb.Append(s.MinLevel);
            sb.Append(t);
            sb.Append("MaxLevel=");
            sb.Append(s.MaxLevel);
            sb.Append(t);
            sb.Append("IsPartyOnly=");
            sb.Append(s.IsPartyOnly);
            sb.Append(t);
            sb.Append("Over18Only=");
            sb.Append(s.Over18Only);
            sb.Append(t);
            sb.Append("Difficulty=");
            sb.Append(s.Difficulty);
            sb.Append(t);
            sb.Append("IsSeason2=");
            sb.Append(s.IsSeason2);
            sb.Append(t);
            sb.Append("SelectedBossQuestIDInfos=");
            sb.Append(String.Join(",", s.SelectedBossQuestIDInfos));
            sb.Append(t);
            sb.Append("IsPracticeMode=");
            sb.Append(s.IsPracticeMode);
            sb.Append(t);
            sb.Append("UserDSMode=");
            sb.Append(s.UserDSMode);
            return sb.ToString();
        }

        public static void PrintAcceptQuestMessage(AcceptQuestMessage msg, object tag)
        {
            PrintLine("AcceptQuestMessage:");
            PrintLine("\tQuestID={0}", msg.QuestID);
            PrintLine("\tTitle={0}", msg.Title);
            PrintLine("\tSwearID={0}", msg.SwearID);
            PrintLine(ShipOptionInfoToString(msg.Option, 1));
        }

        public static void PrintQueryRecommendShipMessage(QueryRecommendShipMessage msg, object tag)
        {
            PrintLine("QueryRecommendShipMessage:");
            if (msg.Restriction == null) {
                return;
            }
            RecommendShipRestriction r = msg.Restriction;
            PrintLine("\tQuestSet={0}",r.QuestSet);
            PrintLine("\tTargetQuestID={0}",r.TargetQuestID);
            PrintLine("\tDifficulty={0}",r.Difficulty);
            PrintLine("\tIsSeason2={0}",r.IsSeason2);
            PrintLine("\tSelectedBossQuestIDInfos=[{0}]",String.Join(",",r.SelectedBossQuestIDInfos));
        }

        public static void PrintEquipItemMessage(EquipItemMessage msg, object tag)
        {
            PrintLine("EquipItemMessage:");
            PrintLine("\tItemID={0}",msg.ItemID);
            PrintLine("\tPartID={0}",msg.PartID);
        }

        public static void PrintEquipBundleMessage(EquipBundleMessage msg, object tag)
        {
            PrintLine("EquipBundleMessage:");
            PrintLine("\tItemClass={0}",msg.ItemClass);
            PrintLine("\tQuickSlotID={0}",msg.QuickSlotID);
        }

        public static void PrintMoveInventoryItemMessage(MoveInventoryItemMessage msg, object tag)
        {
            PrintLine("MoveInventoryItemMessage:");
            PrintLine("\tItemID={0}",msg.ItemID);
            PrintLine("\tStorage={0}",msg.Storage);
            PrintLine("\tTarget={0}",msg.Target);
        }

        private static BeautyShopCustomizeMessage lastBeautyShopMsg = null;

        public static void PrintBeautyShopCustomizeMessage(BeautyShopCustomizeMessage msg, object tag)
        {
            PrintLine("BeautyShopCustomizeMessage:");
            PrintLine("CustomizeItems:");
            foreach (CustomizeItemRequestInfo info in msg.CustomizeItems)
            {
                String color = String.Format("({0},{1},{2})", IntToRGB(info.Color1), IntToRGB(info.Color2), IntToRGB(info.Color3));
                PrintLine("\tItemClass={0} Category={1} Color={2} Duration={3} Price={4} CouponItemID={5}", info.ItemClass, info.Category, color, info.Duration, info.Price, info.CouponItemID);
            }
            BeautyShopCustomizeMessage l = lastBeautyShopMsg;
            BeautyShopCustomizeMessage c = msg;
            if (l == null || c.PaintingPosX != l.PaintingPosX)
            {
                PrintLine("\tPaintingPosX={0}", c.PaintingPosX);
            }
            if (l == null || c.PaintingPosX != l.PaintingPosX)
            {
                PrintLine("\tPaintingPosY={0}", c.PaintingPosY);
            }
            if (l == null || c.PaintingSize != l.PaintingSize)
            {
                PrintLine("\tPaintingSize={0}", c.PaintingSize);
            }
            if (l == null || c.PaintingRotation != l.PaintingRotation)
            {
                PrintLine("\tPaintingRotation={0}", c.PaintingRotation);
            }
            if (l == null || c.payment != l.payment)
            {
                PrintLine("\tPaintingRotation={0}", c.payment);
            }
            if (l == null || c.BodyPaintingPosX != l.BodyPaintingPosX)
            {
                PrintLine("\tBodyPaintingPosX={0}", c.BodyPaintingPosX);
            }
            if (l == null || c.BodyPaintingPosX != l.BodyPaintingPosX)
            {
                PrintLine("\tBodyPaintingPosY={0}", c.BodyPaintingPosY);
            }
            if (l == null || c.BodyPaintingSize != l.BodyPaintingSize)
            {
                PrintLine("\tBodyPaintingSize={0}", c.BodyPaintingSize);
            }
            if (l == null || c.BodyPaintingRotation != l.BodyPaintingRotation)
            {
                PrintLine("\tBodyPaintingRotation={0}", c.BodyPaintingRotation);
            }
            if (l == null || c.BodyPaintingSide != l.BodyPaintingSide)
            {
                PrintLine("\tBodyPaintingSide={0}", c.BodyPaintingSide);
            }
            if (l == null || c.BodyPaintingClip != l.BodyPaintingClip)
            {
                PrintLine("\tBodyPaintingClip={0}", c.BodyPaintingClip);
            }
            if (l == null || c.BodyPaintingMode != l.BodyPaintingMode)
            {
                PrintLine("\tBodyPaintingMode={0}", c.BodyPaintingMode);
            }
            if (l == null || c.HeightValue != l.HeightValue)
            {
                PrintLine("\tHeight={0}", c.HeightValue);
            }
            if (l == null || c.BustValue != l.BustValue)
            {
                PrintLine("\tBust={0}", c.BustValue);
            }
            if (l == null || c.ShinenessValue != l.ShinenessValue)
            {
                PrintLine("\tShineness={0}", c.ShinenessValue);
            }
            if (l == null || c.SkinColor != l.SkinColor)
            {
                PrintLine("\tSkinColor={0}", IntToRGB(c.SkinColor));
            }
            if (l == null || c.EyeColor != l.EyeColor)
            {
                PrintLine("\tEyeColor={0}", IntToRGB(c.EyeColor));
            }
            if (l == null || c.EyebrowItemClass != l.EyebrowItemClass)
            {
                PrintLine("\tEyebrowItemClass={0}", c.EyebrowItemClass);
            }
            if (l == null || c.EyebrowColor != l.EyebrowColor)
            {
                PrintLine("\tEyebrowColor={0}", IntToRGB(c.EyebrowColor));
            }
            if (l == null || c.LookChangeItemClass != l.LookChangeItemClass)
            {
                PrintLine("\tLookChangeItemClass={0}", c.LookChangeItemClass);
            }

            if (l == null || c.LookChangeDuration != l.LookChangeDuration)
            {
                PrintLine("\tLookChangeDuration={0}", c.LookChangeDuration);
            }
            if (l == null || c.LookChangePrice != l.LookChangePrice)
            {
                PrintLine("\tLookChangePrice={0}", c.LookChangePrice);
            }
            PrintLine(BodyShapeInfoToString(c.BodyShapeInfo, 1, l.BodyShapeInfo));
            lastBeautyShopMsg = msg;
        }

        public static void PrintHideCostumeMessage(HideCostumeMessage msg, object tag)
        {
            PrintLine("HideCostumeMessage: HideHead={0} AvatarPart={1} AvatarState={2}", msg.HideHead, msg.AvatarPart, msg.AvatarState);
        }

        public static void PrintUseInventoryItemMessage(UseInventoryItemMessage msg, object tag)
        {
            PrintLine("UseInventoryItemMessage: ItemID={0} TargetItemID={1}", msg.ItemID, msg.TargetItemID);
        }

        public static void PrintSharingStartMessage(SharingStartMessage msg, object tag)
        {
            PrintLine("SharingStartMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tTargetsCID=[{0}]",String.Join(",",msg.TargetsCID)); //System.Collections.Generic.List`1[System.Int64]
        }

        private static string RankResultInfoToString(RankResultInfo r, int numTabs) {
            StringBuilder sb = new StringBuilder();
            if (numTabs != 0) {
                sb.Append(new string('\t',numTabs));
            }
            sb.Append("RankResultInfo: EventID=");
            sb.Append(r.EventID);
            sb.Append(" Rank=");
            sb.Append(r.Rank);
            sb.Append(" RankPrev=");
            sb.Append(r.RankPrev);
            sb.Append(" Score=");
            sb.Append(r.Score);
            sb.Append(" CharacterName=");
            sb.Append(r.CharacterName);
            return sb.ToString();
        }

        public static void PrintRankOtherCharacterInfoMessage(RankOtherCharacterInfoMessage msg, object tag)
        {
            PrintLine("RankOtherCharacterInfoMessage:");
            PrintLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            PrintLine("\tBaseCharacter={0}", msg.BaseCharacter); //System.Int32 has a toString()
            PrintLine("\tCharacterName={0}", msg.CharacterName); //System.String has a toString()
            if (msg.RankResult != null && msg.RankResult.Count != 0)
            {
                PrintLine("\tRankResult:");
                foreach (RankResultInfo rank in msg.RankResult)
                {
                    PrintLine(RankResultInfoToString(rank, 2));
                }
            }
            if (msg.RequesterRankResult != null && msg.RequesterRankResult.Count != 0)
            {
                PrintLine("\tRequesterRankResult:");
                foreach (RankResultInfo rank in msg.RequesterRankResult)
                {
                    PrintLine(RankResultInfoToString(rank, 2));
                }
            }
            PrintLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintXignCodeSecureDataMessage(XignCodeSecureDataMessage msg, object tag)
        {
            PrintLine("XignCodeSecureDataMessage:");
            PrintLine("\tSecureData={0}",BitConverter.ToString(msg.SecureData)); //System.Byte[]
            PrintLine("\tHackCode={0}", msg.HackCode); //System.Int32 has a toString()
            PrintLine("\tHackParam={0}", msg.HackParam); //System.String has a toString()
            PrintLine("\tCheckSum={0}", msg.CheckSum); //System.UInt64 has a toString()
        }

        public static void PrintRankGoalInfoMessage(RankGoalInfoMessage msg, object tag)
        {
            PrintLine("RankGoalInfoMessage:");
            PrintLine("\tEventID={0}", msg.EventID); //System.Int32 has a toString()
            PrintLine("\tRivalName={0}", msg.RivalName); //System.String has a toString()
            PrintLine("\tRivalScore={0}", msg.RivalScore); //System.Int64 has a toString()
            PrintLine("\tRank={0}", msg.Rank); //System.Int32 has a toString()
            PrintLine("\tRankPrev={0}", msg.RankPrev); //System.Int32 has a toString()
        }

        public static void PrintRankFavoritesInfoMessage(RankFavoritesInfoMessage msg, object tag)
        {
            PrintLine("RankFavoritesInfoMessage:");
            if (msg.RankResult != null && msg.RankResult.Count != 0)
            {
                PrintLine("\tRankResult:");
                foreach (RankResultInfo rank in msg.RankResult)
                {
                    PrintLine(RankResultInfoToString(rank, 2));
                }
            }
            PrintLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintRankCharacterInfoMessage(RankCharacterInfoMessage msg, object tag)
        {
            PrintLine("RankCharacterInfoMessage:");
            if (msg.RankResult != null && msg.RankResult.Count != 0)
            {
                PrintLine("\tRankResult:");
                foreach (RankResultInfo rank in msg.RankResult)
                {
                    PrintLine(RankResultInfoToString(rank, 2));
                }
            }
            PrintLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintRankHeavyFavoritesInfoMessage(RankHeavyFavoritesInfoMessage msg, object tag)
        {
            PrintLine("RankHeavyFavoritesInfoMessage:");
            if (msg.RankResult != null && msg.RankResult.Count != 0)
            {
                PrintLine("\tRankResult:");
                foreach (RankResultInfo rank in msg.RankResult)
                {
                    PrintLine(RankResultInfoToString(rank, 2));
                }
            }
        }

        public static void PrintRankAllInfoMessage(RankAllInfoMessage msg, object tag)
        {
            PrintLine("RankAllInfoMessage:");
            if (msg.RankResult != null && msg.RankResult.Count != 0)
            {
                PrintLine("\tRankResult:");
                foreach (RankResultInfo rank in msg.RankResult)
                {
                    PrintLine(RankResultInfoToString(rank, 2));
                }
            }
            PrintLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintRankInfoMessage(RankInfoMessage msg, object tag)
        {
            PrintLine("RankInfoMessage:");
            PrintLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
            if (msg.RankResult != null && msg.RankResult.Count != 0) {
                PrintLine("\tRankResult:");
                foreach (RankResultInfo rank in msg.RankResult) {
                    PrintLine(RankResultInfoToString(rank,2));
                }
            }
        }

        public static void PrintApplyMicroPlayEffectsMessage(ApplyMicroPlayEffectsMessage msg, object tag)
        {
            PrintLine("ApplyMicroPlayEffectsMessage:");
            PrintLine("\tCaster={0}", msg.Caster); //System.Int32 has a toString()
            PrintLine("\tEffectName={0}", msg.EffectName); //System.String has a toString()
            if (msg.EffectList != null && msg.EffectList.Count != 0) {
                PrintLine("\tEffectList:");
                foreach (MicroPlayEffect effect in msg.EffectList)
                {
                    PrintLine("\t\tMicroPlayEffect: PlayerSlotNo={0} Effect={1} Amount={2} Argument={3}",effect.PlayerSlotNo,effect.Effect,effect.Amount,effect.Argument);
                }
            }
        }

        public static void PrintSendMailMessage(SendMailMessage msg, object tag)
        {
            PrintLine("SendMailMessage:");
            PrintLine("\tMailType={0}", msg.MailType); //System.Byte has a toString()
            PrintLine("\tToName={0}", msg.ToName); //System.String has a toString()
            PrintLine("\tTitle={0}", msg.Title); //System.String has a toString()
            PrintLine("\tContent={0}", msg.Content); //System.String has a toString()
            if (msg.ItemList != null && msg.ItemList.Count != 0) {
                PrintLine("\tItemList:");
                foreach (AttachedTransferItemInfo item in msg.ItemList)
                {
                    PrintLine("\t\tAttachedTransferItemInfo: ItemID={0} ItemClass={1} ItemCount={2}",item.ItemID,item.ItemClass,item.ItemCount);
                }
            }
            PrintLine("\tGold={0}", msg.Gold); //System.Int32 has a toString()
            PrintLine("\tExpress={0}", msg.Express); //System.Boolean has a toString()
            PrintLine("\tChargedGold={0}", msg.ChargedGold); //System.Int32 has a toString()
        }

        public static void PrintMailInfoMessage(MailInfoMessage msg, object tag)
        {
            PrintLine("MailInfoMessage:");
            PrintLine("\tMailID={0}", msg.MailID); //System.Int64 has a toString()
            PrintLine(ListToString<MailItemInfo>(msg.Items, "Items", 1));
            PrintLine("\tMailType={0}", msg.MailType); //System.Byte has a toString()
            PrintLine("\tFromName={0}", msg.FromName); //System.String has a toString()
            PrintLine("\tTitle={0}", msg.Title); //System.String has a toString()
            PrintLine("\tContent={0}", msg.Content); //System.String has a toString()
            PrintLine("\tChargedGold={0}", msg.ChargedGold); //System.Int32 has a toString()
        }

        public static void PrintGetMailItemMessage(GetMailItemMessage msg, object tag)
        {
            PrintLine("GetMailItemMessage: MailID={0}", msg.MailID);
        }

        public static void PrintQueryMailInfoMessage(QueryMailInfoMessage msg, object tag)
        {
            PrintLine("QueryMailInfoMessage: MailID={0}", msg.MailID);
        }

        public static void PrintDyeAmpleRequestMessage(DyeAmpleRequestMessage msg, object tag)
        {
            PrintLine("DyeAmpleRequestMessage:");
            PrintLine("\tX={0}", msg.X); //System.Int32 has a toString()
            PrintLine("\tY={0}", msg.Y); //System.Int32 has a toString()
            PrintLine("\tIsAvatar={0}", msg.IsAvatar); //System.Boolean has a toString()
            if (msg.ColorValue != null && msg.ColorValue.Count != 0) {
                PrintLine("\tColorValue:");
                foreach (int color in msg.ColorValue) {
                    PrintLine("\t\t{0}",IntToRGB(color));
                }
            }
        }

        public static void PrintRarityCoreListMessage(RarityCoreListMessage msg, object tag)
        {
            PrintLine("RarityCoreListMessage:");
            if (msg.RareCores != null && msg.RareCores.Count != 0) {
                foreach (RareCoreInfo r in msg.RareCores) {
                    PrintLine("\tRareCore: PlayerTag={0} CoreEntityName={1} CoreType={2}",r.PlayerTag,r.CoreEntityName,r.CoreType);
                }
            }
        }

        public static void PrintRandomItemRewardListMessage(RandomItemRewardListMessage msg, object tag)
        {
            PrintLine("RandomItemRewardListMessage:");
            if (msg.ItemClasses != null && msg.ItemClasses.Count != 0) {
                PrintLine("\tItemClasses:");
                foreach (ColoredEquipment e in msg.ItemClasses)
                {
                    PrintLine("\t\tColoredEquipment: ItemClass={0} Color1={1} Color2={2} Color3={3}",e.ItemClass,e.Color1,e.Color2,e.Color3);
                }
            }
            if (msg.ItemQuantities != null) {
                PrintLine("\tItemQuantities=[{0}]", String.Join(",", msg.ItemQuantities)); //System.Collections.Generic.ICollection`1[System.Int32]
            }
            PrintLine("\tIsUserCare={0}", msg.IsUserCare); //System.Boolean has a toString()
            PrintLine("\tKeyItemClass={0}", msg.KeyItemClass); //System.String has a toString()
            PrintLine("\tDisableShowPopUp={0}", msg.DisableShowPopUp); //System.Boolean has a toString()
        }

        public static void PrintManufactureCraftMessage(ManufactureCraftMessage msg, object tag)
        {
            PrintLine("ManufactureCraftMessage:");
            PrintLine("\tRecipeID={0}", msg.RecipeID); //System.String has a toString()
            PrintLine("\tPartsIDList=[{0}]",string.Join(",",msg.PartsIDList)); //System.Collections.Generic.List`1[System.Int64]
        }

        public static void PrintItemPriceInfoMessage(ItemPriceInfoMessage msg, object tag)
        {
            PrintLine("ItemPriceInfoMessage:");
            foreach (KeyValuePair<string,PriceRange> entry in msg.Prices) {
                PriceRange p = entry.Value;
                PrintLine("\t{0}=PriceRange: Price={1} Min={2} Max={3}",entry.Key,p.Price,p.Min,p.Max);
            }
            //PrintLine("\tPrices={0}",msg.Prices); //System.Collections.Generic.Dictionary`2[System.String,ServiceCore.EndPointNetwork.PriceRange]
        }

        public static void PrintEnhanceItemResultMessage(EnhanceItemResultMessage msg, object tag)
        {
            PrintLine("EnhanceItemResultMessage:");
            PrintLine("\tResult={0}", msg.Result); //System.String has a toString()
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tPrevItemClass={0}", msg.PrevItemClass); //System.String has a toString()
            PrintLine("\tEnhancedItemClass={0}", msg.EnhancedItemClass); //System.String has a toString()
            PrintLine(DictToString<string,int>(msg.FailReward,"FailReward",1)); //System.Collections.Generic.Dictionary`2[System.String,System.Int32]
        }

        public static void PrintRespawnMessage(RespawnMessage msg, object tag)
        {
            PrintLine("RespawnMessage:");
            PrintLine("\tSpawnerNames=[{0}]",String.Join(",",msg.SpawnerNames)); //System.Collections.Generic.List`1[System.String]
            PrintLine("\tSectorID={0}", msg.SectorID); //System.Int32 has a toString()
            if (msg.TrialFactorInfos != null && msg.TrialFactorInfos.Count != 0) {
                PrintLine("\tTrialFactorInfos:");
                foreach (TrialFactorInfo trial in msg.TrialFactorInfos) {
                    PrintLine("\t\tGroupNumber={0} TrialMod={1} TrialName={2} SectorGroupID={3}",trial.GroupNumber,trial.TrialMod,trial.TrialName,trial.SectorGroupID);
                }
            }
            PrintLine(DictToString<string,int>(msg.MonsterItemDrop,"MonsterItemDrop",1)); //System.Collections.Generic.Dictionary`2[System.String,System.Int32]
        }

        public static void PrintStoryVariablesMessage(StoryVariablesMessage msg, object tag)
        {
            PrintLine("StoryVariablesMessage:");
            foreach (StoryVariableInfo s in msg.StoryVariables) {
                PrintLine("StoryVariable: StoryLine={0} {1}={2}",s.StoryLine,s.Key,s.Value);
            }
        }

        public static void PrintSetMileagePointMessage(SetMileagePointMessage msg, object tag)
        {
            int MileagePoint = GetPrivateProperty<int>(msg, "MileagePoint");
            PrintLine("SetMileagePointMessage: MileagePoint={0}",MileagePoint);
        }

        public static void PrintSetFeverPointMessage(SetFeverPointMessage msg, object tag)
        {
            int FeverPoint = GetPrivateProperty<int>(msg, "FeverPoint");
            PrintLine("SetFeverPointMessage: FeverPoint={0}",FeverPoint);
        }

        public static void PrintRoulettePickSlotResultMessage(RoulettePickSlotResultMessage msg, object tag)
        {
            PrintLine("RoulettePickSlotResultMessage: PickedSlot={0}",msg.PickedSlot);
        }

        public static void PrintSelectTargetItemMessage(SelectTargetItemMessage msg, object tag)
        {
            PrintLine("SelectTargetItemMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tTargetItems=[{0}]",String.Join(",",msg.TargetItems)); //System.Collections.Generic.List`1[System.Int64]
            PrintLine("\tLocalTextKey={0}", msg.LocalTextKey); //System.String has a toString()
        }

        public static void PrintBraceletCombinationResultMessage(BraceletCombinationResultMessage msg, object tag)
        {
            PrintLine("BraceletCombinationResultMessage:");
            PrintLine("\tBraceletItemID={0}", msg.BraceletItemID); //System.Int64 has a toString()
            PrintLine("\tGemstoneItemID={0}", msg.GemstoneItemID); //System.Int64 has a toString()
            PrintLine("\tResultCode={0}", msg.ResultCode); //System.Int32 has a toString()
            PrintLine("\tResultMessage={0}", msg.ResultMessage); //System.String has a toString()
            PrintLine(DictToString<string,int>(msg.PreviousStat,"PreviousStat",1)); //System.Collections.Generic.Dictionary`2[System.String,System.Int32]
            PrintLine(DictToString<string, int>(msg.newStat, "NewStat", 1)); //System.Collections.Generic.Dictionary`2[System.String,System.Int32]
        }

        public static void PrintBuyIngameCashshopUseTirResultMessage(BuyIngameCashshopUseTirResultMessage msg, object tag)
        {
            PrintLine("BuyIngameCashshopUseTirResultMessage:");
            PrintLine("\tProducts=[{0}]",String.Join(",",msg.Products)); //System.Collections.Generic.List`1[System.Int32]
            PrintLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
        }

        public static void PrintRecommendFriendListMessage(RecommendFriendListMessage msg, object tag)
        {
            PrintLine("RecommendFriendListMessage:");
            PrintLine("\tfriendName={0}", msg.friendName); //System.String has a toString()
            PrintLine("\trecommenderList=[{0}]",String.Join(",",msg.recommenderList)); //System.Collections.Generic.List`1[System.String]
        }

        public static void PrintOpenTreasureBoxResultMessage(OpenTreasureBoxResultMessage msg, object tag)
        {
            PrintLine("OpenTreasureBoxResultMessage:");
            PrintLine("\tGroupID={0}", msg.GroupID); //System.Int32 has a toString()
            PrintLine("\tEntityName={0}", msg.EntityName); //System.String has a toString()
            PrintLine(DictToString<string,int>(msg.MonsterList,"MonsterList",1)); //System.Collections.Generic.Dictionary`2[System.String,System.Int32]
        }

        public static void PrintPvpTeamInfoMessage(PvpTeamInfoMessage msg, object tag)
        {
            PrintLine("PvpTeamInfoMessage:");
            PrintLine(DictToString<int,int>(msg.TeamInfo,"TeamInfo",1)); //System.Collections.Generic.Dictionary`2[System.Int32,System.Int32]
        }

        public static void PrintMonsterKilledMessage(MonsterKilledMessage msg, object tag)
        {
            PrintLine("MonsterKilledMessage:");
            PrintLine("\tAttacker={0}", msg.Attacker);
            PrintLine("\tTarget={0}", msg.Target);
            PrintLine("\tActionType={0}", msg.ActionType);
            PrintLine("\tHasEvilCore={0}", msg.HasEvilCore);
            PrintLine("\tDamage={0}", msg.Damage);
            PrintLine("\tDamagePositionX={0}", msg.DamagePositionX);
            PrintLine("\tDamagePositionY={0}", msg.DamagePositionY);
            PrintLine("\tDistance={0}", msg.Distance);
            PrintLine("\tActorIndex={0}", msg.ActorIndex);
        }

        public static void PrintExecuteNpcServerCommandMessage(ExecuteNpcServerCommandMessage msg, object tag)
        {
            PrintLine("ExecuteNpcServerCommandMessage: []");
        }

        public static void PrintRagdollKickedMessage(RagdollKickedMessage msg, object tag)
        {
            PrintLine("RagdollKickedMessage:");
            PrintLine("\tTag={0}", msg.Tag);
            PrintLine("\tTargetEntityName={0}", msg.TargetEntityName);
            PrintLine("\tEvilCoreType={0}", msg.EvilCoreType);
            PrintLine("\tIsRareCore={0}", msg.IsRareCore);
        }

        public static void PrintCombatRecordMessage(CombatRecordMessage msg, object tag)
        {
            PrintLine("CombatRecordMessage:");
            PrintLine("\tPlayerNumber={0}", msg.PlayerNumber);
            PrintLine("\tComboMax={0}", msg.ComboMax);
            PrintLine("\tHitMax={0}", msg.HitMax);
            PrintLine("\tStyleMax={0}", msg.StyleMax);
            PrintLine("\tDeath={0}", msg.Death);
            PrintLine("\tKill={0}", msg.Kill);
            PrintLine("\tBattleAchieve={0}", msg.BattleAchieve);
            PrintLine("\tHitTake={0}", msg.HitTake);
            PrintLine("\tStyleCount={0}", msg.StyleCount);
            PrintLine("\tRankStyle={0}", msg.RankStyle);
            PrintLine("\tRankBattle={0}", msg.RankBattle);
            PrintLine("\tRankTotal={0}", msg.RankTotal);
        }


        public static void PrintBurnItemInfo(BurnItemInfo msg, object tag)
        {
            PrintLine("BurnItemInfo:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tCount={0}", msg.Count); //System.Int32 has a toString()
        }

        public static void PrintBurnResultMessage(BurnResultMessage msg, object tag)
        {
            PrintLine("BurnResultMessage:");
            PrintLine("\tMessage={0}", msg.Message); //System.String has a toString()
            PrintLine("\tGauge={0}", msg.Gauge); //System.Int32 has a toString()
        }

        public static void PrintCaptchaRefreshMessage(CaptchaRefreshMessage msg, object tag)
        {
            PrintLine("CaptchaRefreshMessage:");
        }

        public static void PrintCaptchaRefreshResultMessage(CaptchaRefreshResultMessage msg, object tag)
        {
            PrintLine("CaptchaRefreshResultMessage:");
        }
        public static void PrintMicroPlayEventMessage(MicroPlayEventMessage msg, object tag)
        {
            PrintLine("MicroPlayEventMessage: Slot={0} EventString={1}", msg.Slot, msg.EventString);
        }

        public static void PrintMonsterDamageReportMessage(MonsterDamageReportMessage msg, object tag)
        {
            PrintLine("MonsterDamageReportMessage: Target={0}", msg.Target);
            foreach (MonsterTakeDamageInfo i in msg.TakeDamageList)
            {
                PrintLine("\tMonsterTakeDamageInfo: Attacker={0} AttackTime={1} ActionName={2} Damage={3}", i.Attacker, i.AttackTime, i.ActionName, i.Damage);
            }
        }

        private static Dictionary<int, PartyMemberInfo> lastPartyMember = new Dictionary<int, PartyMemberInfo>();

        private static string PartyMemberInfoToString(PartyMemberInfo m, String name, int numTabs)
        {
            String t = "";
            if (numTabs > 0)
            {
                t = new string('\t', numTabs);
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(t);
            sb.Append(name);
            sb.Append(":");
            t = "\n" + new string('\t', numTabs + 1);
            sb.Append(t);
            sb.Append("CharacterID=");
            sb.Append(m.CharacterID);

            PartyMemberInfo last = null;
            lastPartyMember.TryGetValue(m.NexonSN, out last);

            if (last == null || m.CharacterID != last.CharacterID)
            {
                sb.Append(t);
                sb.Append("Character=");
                sb.Append(BaseCharacterToString(m.Character));
                sb.Append(t);
                sb.Append("NexonSN=");
                sb.Append(m.NexonSN);
                sb.Append(t);
                sb.Append("Level=");
                sb.Append(m.Level);
            }
            if (last == null || m.SlotNum != last.SlotNum)
            {
                sb.Append(t);
                sb.Append("SlotNum=");
                sb.Append(m.SlotNum);
            }
            if (last == null || m.State != last.State)
            {
                sb.Append(t);
                sb.AppendFormat("State=");
                sb.Append(m.State);
            }
            if (last == null || m.LatestPing != last.LatestPing)
            {
                sb.Append(t);
                sb.Append("LatestPing=");
                sb.Append(m.LatestPing);
            }
            if (last == null || m.LatestFrameRate != last.LatestFrameRate)
            {
                sb.Append(t);
                sb.Append("LatestFrameRate=");
                sb.Append(m.LatestFrameRate);
            }
            if (last == null || m.IsReturn != last.IsReturn)
            {
                sb.Append(t);
                sb.Append("IsReturn=");
                sb.Append(m.IsReturn);
            }
            if (last == null || m.IsEventJumping != last.IsEventJumping)
            {
                sb.Append(t);
                sb.Append("IsEventJumping=");
                sb.Append(m.IsEventJumping);
            }
            lastPartyMember[m.NexonSN] = m;
            return sb.ToString();
        }

        private static PartyInfoMessage lastParty = null;

        public static void PrintPartyInfoMessage(PartyInfoMessage msg, object tag)
        {
            PrintLine("PartyInfoMessage:");

            //don't want to show a diff between two different parties
            if (lastParty != null && lastParty.PartyID != msg.PartyID)
            {
                lastParty = null;
            }
            PrintLine("\tPartyID={0}", msg.PartyID);
            if (lastParty != null && lastParty.PartySize != msg.PartySize)
            {
                PrintLine("\tPartySize={0}", msg.PartySize);
            }
            if (lastParty != null && msg.State != lastParty.State)
            {
                PrintLine("\tState={0}", msg.State);
            }

            int i = 0;
            if (lastParty?.Members != null && lastParty.Members.Count != 0)
            {
                PartyMemberInfo[] lastList = lastParty.Members.ToArray();
                foreach (PartyMemberInfo m in msg.Members)
                {
                    PrintLine(PartyMemberInfoToString(m, String.Format("Member[{0}]", i++), 1));
                }
            }
            lastParty = msg;
        }

        public static void PrintExpandExpirationDateResponseMessage(ExpandExpirationDateResponseMessage msg, object tag)
        {
            PrintLine("ExpandExpirationDateResponseMessage:");
            int type = GetPrivateProperty<int>(msg, "Type");
            long ItemID = GetPrivateProperty<long>(msg, "ItemID");
            PrintLine("\tType={0}",type);
            PrintLine("\tItemID={0}",ItemID);
        }

        public static T GetPrivateProperty<T>(Object msg, string propName)
        {
            Type t = msg.GetType();
            FieldInfo[] fields = t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo f in fields)
            {
                if (f.Name == propName)
                {
                    return (T)f.GetValue(msg);
                }
            }
            return default(T);
        }

        public static void PrintRecommendShipMessage(RecommendShipMessage msg, object tag)
        {
            PrintLine("RecommendShipMessage:");
            int i = 0;
            foreach (ShipInfo ship in msg.RecommendedShip) {
                PrintLine(ShipInfoToString(ship,String.Format("ShipInfo[{0}]",i++),1)); //System.Collections.Generic.ICollection`1[ServiceCore.EndPointNetwork.ShipInfo]
            }
        }

        public static void PrintTransferValuesListMessage(TransferValuesListMessage msg, object tag)
        {
            PrintLine("TransferValuesListMessage:");
            foreach(TransferValues values in msg.TransferInfos)
            {
                PrintLine("TransferValues:");
                PrintLine("\t\tCID={0}",values.CID);
                PrintLine(DictToString<string,string>(values.Values,"Values",2));
            }
        }

        public static void PrintTransferValuesSetMessage(TransferValuesSetMessage msg, object tag)
        {
            PrintLine("TransferValuesSetMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tCommand={0}", msg.Command); //System.String has a toString()
            PrintLine(DictToString<string, string>(msg.Values, "Values", 1)); //System.Collections.Generic.IDictionary`2[System.String,System.String]
        }

        public static void PrintTransferValuesResultMessage(TransferValuesResultMessage msg, object tag)
        {
            PrintLine("TransferValuesResultMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tCommandResult={0}", msg.CommandResult); //System.String has a toString()
            PrintLine(DictToString<string, string>(msg.ResultValues, "ResultValues", 1));
        }

        public static void PrintSelectOrdersChangeMessage(SelectOrdersChangeMessage msg, object tag)
        {
            PrintLine("SelectOrdersChangeMessage:");
            PrintLine("\tSelectOrders=[{0}]",String.Join(",",msg.SelectOrders)); //System.Collections.Generic.ICollection`1[System.Byte]
        }

        public static void PrintSingleCharacterMessage(SingleCharacterMessage msg, object tag)
        {
            PrintLine("SingleCharacterMessage:");
            PrintLine("\tLoginPartyState={0}", msg.LoginPartyState); //System.Collections.Generic.ICollection`1[System.Int32]
            int i = 0;
            foreach (CharacterSummary c in msg.Characters) {
                if (MongoDBConnect.connection != null)
                {
                    MongoDBConnect.connection.InsertCharacterSummary(c);
                }
                PrintLine(CharacterSummaryToString(c, String.Format("Characters[{0}]", i++), 1));
            }
            
        }

        public static void PrintLevelUpMessage(LevelUpMessage msg, object tag)
        {
            PrintLine("LevelUpMessage:");
            PrintLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            PrintLine(DictToString<string,int>(msg.StatIncreased,"StatIncreased",1)); //System.Collections.Generic.IDictionary`2[System.String,System.Int32]
        }

        public static void PrintHackShieldRequestMessage(HackShieldRequestMessage msg, object tag)
        {
            PrintLine("HackShieldRequestMessage:");
            if (msg.Request == null) {
                return;
            }
            ArraySegment<byte> r = msg.Request;
            PrintLine("\tRequest={0}",BitConverter.ToString(r.Array,r.Offset,r.Count)); //System.ArraySegment`1[System.Byte]
        }

        public static void PrintDeleteMailMessage(DeleteMailMessage msg, object tag)
        {
            PrintLine("DeleteMailMessage:");
            PrintLine("\tMailList=[{0}]",String.Join(",",msg.MailList)); //System.Collections.Generic.ICollection`1[System.Int64]
        }

        private static string ShipInfoToString(ShipInfo s, string name, int numTabs)
        {
            String t = "";
            StringBuilder sb = new StringBuilder();
            if (numTabs > 0)
            {
                t = new string('\t', numTabs);
                sb.Append(t);
            }
            sb.Append(name);
            sb.Append(":");
            t = "\n" + new string('\t', numTabs + 1);
            sb.Append(t);
            sb.Append("PartyID=");
            sb.Append(s.PartyID);
            sb.Append(t);
            sb.Append("ShipName=");
            sb.Append(s.ShipName);
            sb.Append(t);
            sb.Append("Password=");
            sb.Append(s.Password);
            sb.Append(t);
            sb.Append("MinLevelConstraint=");
            sb.Append(s.MinLevelConstraint);
            sb.Append(t);
            sb.Append("MaxLevelConstraint=");
            sb.Append(s.MaxLevelConstraint);
            sb.Append(t);
            sb.Append("MemberCount=");
            sb.Append(s.MemberCount);
            sb.Append(t);
            sb.Append("MaxShipMemberCount=");
            sb.Append(s.MaxShipMemberCount);
            sb.Append(t);
            sb.Append("QuestID=");
            sb.Append(s.QuestID);
            sb.Append(t);
            sb.Append("IsHuntingQuest=");
            sb.Append(s.IsHuntingQuest);
            sb.Append(t);
            sb.Append("IsGiantRaid=");
            sb.Append(s.IsGiantRaid);
            sb.Append(t);
            sb.Append("SwearID=");
            sb.Append(s.SwearID);
            sb.Append(t);
            sb.Append("RestTime=");
            sb.Append(s.RestTime);
            sb.Append(t);
            sb.Append("ReinforceAllowed=");
            sb.Append(s.ReinforceAllowed);
            sb.Append(t);
            sb.Append("AdultRule=");
            sb.Append(s.AdultRule);
            sb.Append(t);
            sb.Append("State=");
            sb.Append(s.State);
            sb.Append(t);
            sb.Append("PartyBonusCount=");
            sb.Append(s.PartyBonusCount);
            sb.Append(t);
            sb.Append("PartyBonusRatio=");
            sb.Append(s.PartyBonusRatio);
            sb.Append(t);
            sb.Append("IsPartyOnly=");
            sb.Append(s.IsPartyOnly);
            sb.Append(t);
            sb.Append("HostPing=");
            sb.Append(s.HostPing);
            sb.Append(t);
            sb.Append("HostFrameRate=");
            sb.Append(s.HostFrameRate);
            sb.Append(t);
            sb.Append("Difficulty=");
            sb.Append(s.Difficulty);
            sb.Append(t);
            sb.Append("IsReturn=");
            sb.Append(s.IsReturn);
            sb.Append(t);
            sb.Append("IsSeason2=");
            sb.Append(s.IsSeason2);
            sb.Append(t);
            sb.Append("IsPracticeMode=");
            sb.Append(s.IsPracticeMode);
            sb.Append(t);
            sb.Append("UserDSMode=");
            sb.Append(s.UserDSMode);
            sb.Append(t);
            sb.Append("SelectedBossQuestIDInfos=");
            sb.Append(String.Join(",", s.selectedBossQuestIDInfos));

            int i = 0;
            foreach (PartyMemberInfo m in s.Members)
            {
                sb.Append("\n");
                sb.Append(PartyMemberInfoToString(m, String.Format("Member[{0}]", i++), 1));
            }
            return sb.ToString();
        }

        public static void PrintUpdateShipMessage(UpdateShipMessage msg, object tag)
        {
            //need to use reflection to access the ShipInfo object
            ShipInfo s = GetPrivateProperty<ShipInfo>(msg, "info");
            PrintLine(ShipInfoToString(s, "UpdateShipMessage", 0));
        }

        public static void PrintPayCoinCompletedMessage(PayCoinCompletedMessage msg, object tag)
        {
            PrintLine("PayCoinCompletedMessage:");
            foreach (PaidCoinInfo p in msg.Coininfos)
            {
                PrintLine("\tSlot={0} SilverCoin=[{1}] PlatinumCoinOwner={2} PlatinumCoinType={3}", p.Slot, String.Join(",", p.SilverCoin), p.PlatinumCoinOwner, p.PlatinumCoinType);
            }
        }

        public static ConnectionRequestMessage lastConnectionRequestMsg = null;

        public static void PrintConnectionRequestMessage(ConnectionRequestMessage msg, object tag)
        {
            //TODO: use this info to decrypt relay messages
            PrintLine("ConnectionRequestMessage:");
            PrintLine("\tAddress={0}", msg.Address);
            PrintLine("\tPort={0}", msg.Port);
            PrintLine("\tPosixTime={0}", msg.PosixTime);
            PrintLine("\tKey={0}", msg.Key);
            PrintLine("\tCategory={0}", msg.Category);
            PrintLine("\tPingHostCID={0}", msg.PingHostCID);
            PrintLine("\tGroupID={0}", msg.GroupID);
            lastConnectionRequestMsg = msg;
        }

        public static void PrintLaunchShipGrantedMessage(LaunchShipGrantedMessage msg, object tag)
        {
            PrintLine("\tLaunchShipGrantedMessage:");
            PrintLine("\tQuestID={0}", msg.QuestID);
            PrintLine("\tAdultRule={0}", msg.AdultRule);
            PrintLine("\tIsPracticeMode={0}", msg.IsPracticeMode);
            PrintLine("\tHostInfo:");
            PrintLine(GameJoinMemberInfoToString(msg.HostInfo, 2));
        }

        public static void PrintTodayMissionUpdateMessage(TodayMissionUpdateMessage msg, object tag)
        {
            PrintLine("TodayMissionUpdateMessage:");
            PrintLine("\tID={0}", msg.ID);
            PrintLine("\tCurrentCount={0}", msg.CurrentCount);
            PrintLine("\tIsFinished={0}", msg.IsFinished);
        }
        public static void PrintReturnFromQuestMessage(ReturnFromQuestMessage msg, object tag)
        {
            PrintLine("ReturnFromQuestMessage:");
            PrintLine("\tOrder={0}", msg.Order);
            PrintLine("\tStorySectorBSP={0}", msg.StorySectorBSP);
            PrintLine("\tStorySectorEntity={0}", msg.StorySectorEntity);
        }

        public static void PrintKickedMessage(KickedMessage msg, object tag)
        {
            PrintLine("KickedMessage: []");
        }

        private static string SummaryRewardInfoListToString(List<SummaryRewardInfo> list, String name, int numTabs)
        {
            if (list.Count == 0)
            {
                return "";
            }
            String t = "";
            StringBuilder sb = new StringBuilder();
            if (numTabs > 0)
            {
                t = new string('\t', numTabs);
                sb.Append(t);
            }
            sb.Append(name);
            sb.Append(":");
            if (list == null || list.Count == 0)
            {
                return sb.ToString();
            }
            t = "\n" + new string('\t', numTabs + 1);

            foreach (SummaryRewardInfo s in list)
            {
                sb.Append(t);
                sb.Append("Desc=");
                sb.Append(s.Desc);
                sb.Append(" Arg1=");
                sb.Append(s.Arg1);
                sb.Append(" Arg2=");
                sb.Append(s.Arg2);
                sb.Append(" Value=");
                sb.Append(s.Value);
            }
            return sb.ToString();
        }

        public static void PrintFinishGameMessage(FinishGameMessage msg, object tag)
        {
            PrintLine("FinishGameMessage:");
            PrintLine("\tHasStorySector={0}", msg.HasStorySector);
            PrintLine("\tCanRestart={0}", msg.CanRestart);
            PrintLine("\tCanContinueFinishedQuest={0}", msg.CanContinueFinishedQuest);
            PrintLine("\tSummary:");
            QuestSummaryInfo q = msg.Summary;
            PrintLine("\t\tQuestID={0}", q.QuestID);
            PrintLine("\t\tSwearID={0}", q.SwearID);
            PrintLine("\t\tIsTodayQuest={0}", q.IsTodayQuest);
            PrintLine("\t\tResult={0}", q.Result);
            PrintLine("\t\tLastProgress={0}", q.LastProgress);
            PrintLine("\t\tCurrentProgress={0}", q.CurrentProgress);
            if (q.ClearedGoals.Count != 0)
            {
                PrintLine("\t\tClearedGoals:");
                foreach (KeyValuePair<int, SummaryGoalInfo> entry in q.ClearedGoals)
                {
                    PrintLine("\t\t\t{0}=(GoalID={1} Exp={2} Gold={3} Ap={4})", entry.Key, entry.Value.GoalID, entry.Value.Exp, entry.Value.Gold, entry.Value.Ap);
                }
            }
            PrintLine(SummaryRewardInfoListToString(q.BattleExp, "BattleExp", 2));
            PrintLine(SummaryRewardInfoListToString(q.EtcExp, "EtcExp", 2));
            PrintLine(SummaryRewardInfoListToString(q.MainGold, "MainGold", 2));
            PrintLine(SummaryRewardInfoListToString(q.EtcGold, "EtcGold", 2));
            PrintLine(SummaryRewardInfoListToString(q.QuestAp, "QuestAp", 2));
            PrintLine(SummaryRewardInfoListToString(q.EtcAp, "EtcAp", 2));
            PrintLine("\t\tExp={0}", q.Exp);
            PrintLine("\t\tGold={0}", q.Gold);
            PrintLine("\t\tAp={0}", q.Ap);
            PrintLine(DictToString<string, int>(q.RewardItem, "RewardItem", 2));
        }
        public static void PrintMicroStatusEffectUpdated(MicroStatusEffectUpdated msg, object tag)
        {
            PrintLine("MicroStatusEffectUpdated:");
            PrintLine("\tCharacterName={0}", msg.CharacterName);
            PrintLine("\tSlot={0}", msg.Slot);
            PrintLine(StatusEffectListToString(msg.StatusEffects, "StatusEffects", msg.CharacterName, 1));
        }

        public static void PrintQuestSuccessSceneStartMessage(QuestSuccessSceneStartMessage msg, object tag)
        {
            PrintLine("QuestSuccessSceneStartMessage: []");
        }

        public static void PrintQuestTargetResultMessage(QuestTargetResultMessage msg, object tag)
        {
            PrintLine("QuestTargetResultMessage:");
            PrintLine("\tCharacterName={0}", msg.CharacterName);
            PrintLine("\tGoalID={0}", msg.GoalID);
            PrintLine("\tIsGoalSuccess={0}", msg.IsGoalSuccess);
            PrintLine("\tIsQuestSuccess={0}", msg.IsQuestSuccess);
            PrintLine("\tExp={0}", msg.Exp);
            PrintLine("\tGold={0}", msg.Gold);
            PrintLine("\tAp={0}", msg.Ap);
        }

        public static void PrintDropErgMessage(DropErgMessage msg, object tag)
        {
            PrintLine("DropErgMessage:");
            PrintLine("\tBrokenProp={0}", msg.BrokenProp);
            PrintLine("\tMonsterEntityName={0}", msg.MonsterEntityName);
            PrintLine("\tDropErg:");
            foreach (ErgInfo e in msg.DropErg)
            {
                int winner = GetPrivateProperty<int>(e, "winner");
                int ergID = GetPrivateProperty<int>(e, "ergID");
                string ergClass = GetPrivateProperty<string>(e, "ergClass");
                string ergType = GetPrivateProperty<string>(e, "ergType");
                int amount = GetPrivateProperty<int>(e, "amount");
                PrintLine("\t\tWinner={0} ErgID={1} ErgClass={2} ErgType={3} Amount={4}", winner, ergID, ergClass, ergType, amount);
            }
        }
        public static void PrintGetItemMessage(GetItemMessage msg, object tag)
        {
            PrintLine("GetItemMessage:");
            PrintLine("\tPlayerName={0}", msg.PlayerName);
            PrintLine("\tItemClass={0}", msg.ItemClass);
            PrintLine("\tCount={0}", msg.Count);
            PrintLine("\tCoreType={0}", msg.CoreType);
            PrintLine("\tLucky={0}", msg.Lucky);
            PrintLine("\tLuckBonus={0}", msg.LuckBonus);
            PrintLine("\tGiveItemResult={0}", (GiveItem.ResultEnum)msg.GiveItemResult);
        }


        public static string QuestSectorInfosToString(QuestSectorInfos q, string name, int numTabs)
        {
            StringBuilder sb = new StringBuilder();
            String t = "";
            if (numTabs > 0)
            {
                t = new string('\t', numTabs);
                sb.Append(t);
            }
            sb.Append(name);
            sb.Append(":");
            t = "\n" + new string('\t', numTabs + 1);
            sb.Append(t);
            sb.Append("CurrentGroup=");
            sb.Append(q.CurrentGroup);
            sb.Append(t);
            sb.Append("StartingGroup=");
            sb.Append(q.StartingGroup);
            sb.Append(t);
            sb.Append("TreasureBoxMaxCount=");
            sb.Append(q.TreasureBoxMaxCount);
            sb.Append(t);
            sb.Append("AltarStatus=");
            sb.Append(String.Join(",", q.AltarStatus));
            sb.Append(t);
            sb.Append("BossKilledList=");
            sb.Append(String.Join(",", q.BossKilledList));


            int i = 0;
            foreach (QuestSectorInfo s in q.SectorInfos)
            {
                sb.Append(t);
                sb.Append("QuestSectorInfo[");
                sb.Append(i++);
                sb.Append("]:");
                sb.Append(t);
                sb.Append("\tGroupID=");
                sb.Append(s.GroupID);
                sb.Append(t);
                sb.Append("\tBsp=");
                sb.Append(s.Bsp);

                int j = 0;
                foreach (String entity in s.Entities)
                {
                    sb.Append(t);
                    sb.Append("\tEntities[");
                    sb.Append(j++);
                    sb.Append("]:");
                    String tabbedEntity = entity.Replace("\n", "\n\t\t\t");
                    sb.Append(tabbedEntity);
                }
                sb.Append(t);
                sb.Append("\tConnectionInfos:");
                foreach (QuestSectorConnectionInfo c in s.ConnectionInfos)
                {
                    sb.Append(t);
                    sb.Append("\t\tFrom=");
                    sb.Append(c.From);
                    sb.Append(" FromTrigger=");
                    sb.Append(c.FromTrigger);
                    sb.Append(" To=");
                    sb.Append(c.To);
                    sb.Append(" ToSpawn=");
                    sb.Append(c.ToSpawn);
                }
                sb.Append('\n');
                sb.Append(DictToString<string, int>(s.ItemDropEntity, "ItemDropEntity", 3));
                sb.Append('\n');
                sb.Append(DictToString<string, int>(s.WeakPointEntity, "WeakPointEntity", 3));
                sb.Append(t);
                sb.Append("\tTrialFactorInfos:");

                foreach (TrialFactorInfo tf in s.TrialFactorInfos)
                {
                    sb.Append(t);
                    sb.Append("\t\tGroupNumber=");
                    sb.Append(tf.GroupNumber);
                    sb.Append(" TrialMod=");
                    sb.Append(tf.TrialMod);
                    sb.Append(" TrialName=");
                    sb.Append(tf.TrialName);
                    sb.Append(" SectorGroupID=");
                    sb.Append(tf.SectorGroupID);
                }
                sb.Append(t);
                sb.Append("\tTreasureBoxInfos:");
                foreach (TreasureBoxInfo tb in s.TreasureBoxInfos)
                {
                    sb.Append(t);
                    sb.Append("\t\tGroupID=");
                    sb.Append(tb.GroupID);
                    sb.Append(" EntityName=");
                    sb.Append(tb.EntityName);
                    sb.Append(" IsVisible=");
                    sb.Append(tb.IsVisible);
                    sb.Append(" IsOpend=");
                    sb.Append(tb.IsOpend);
                }
                sb.Append(t);
                sb.Append("\tSaveRequired=");
                sb.Append(s.SaveRequired);
                sb.Append(t);
                sb.Append("\tLastUsedSpawnPoint=");
                sb.Append(s.LastUsedSpawnPoint);
            }
            return sb.ToString();
        }

        public static void PrintGameStartGrantedMessage(GameStartGrantedMessage msg, object tag)
        {
            //TODO: save this info to automate stuff
            PrintLine("GameStartGrantedMessage:");
            PrintLine("\tQuestLevel={0}", msg.QuestLevel);
            PrintLine("\tQuestTime={0}", msg.QuestTime);
            PrintLine("\tHardMode={0}", msg.HardMode);
            PrintLine("\tSoloSector={0}", msg.SoloSector);
            PrintLine("\tIsHuntingQuest={0}", msg.IsHuntingQuest);
            PrintLine("\tInitGameTime={0}", msg.InitGameTime);
            PrintLine("\tSectorMoveGameTime={0}", msg.SectorMoveGameTime);
            PrintLine("\tDifficulty={0}", msg.Difficulty);
            PrintLine("\tIsTimerDecreasing={0}", msg.IsTimerDecreasing);
            PrintLine("\tQuestStartedPlayerCount={0}", msg.QuestStartedPlayerCount);
            PrintLine("\tNewSlot={0}", msg.NewSlot);
            PrintLine("\tNewKey={0}", msg.NewKey);
            PrintLine("\tIsUserDedicated={0}", msg.IsUserDedicated);

            PrintLine(QuestSectorInfosToString(msg.QuestSectorInfos, "SectorInfo", 1));

        }
        public static void PrintShipListMessage(ShipListMessage msg, object tag)
        {
            PrintLine("ShipListMessage:");
            PrintLine("\tIgnored={0}", msg.Ignored);
            int i = 0;
            foreach (ShipInfo s in msg.ShipList)
            {
                PrintLine(ShipInfoToString(s, String.Format("ShipInfo[{0}]", i++), 1));
            }
        }

        public static void PrintLearnNewSkillResultMessage(LearnNewSkillResultMessage msg, object tag)
        {
            PrintLine("LearnNewSkillResultMessage: Result={0}", (LearnNewSkillResultMessage.LearnNewSkillResult)msg.result);
        }

        public static void PrintLearnSkillMessage(LearnSkillMessage msg, object tag)
        {
            PrintLine("LearnSkillMessage:");
            PrintLine("\tSkillID={0}", msg.SkillID);
            PrintLine("\tAP={0}", msg.AP);
        }
        public static void PrintLearnNewSkillMessage(LearnNewSkillMessage msg, object tag)
        {
            PrintLine("LearnNewSkillMessage: SkillID={0}", msg.SkillID);
        }

        public static void PrintTodayMissionCompleteMessage(TodayMissionCompleteMessage msg, object tag)
        {
            PrintLine("TodayMissionCompleteMessage: ID={0}", msg.ID);
        }

        public static void PrintEndTradeSessionMessage(EndTradeSessionMessage msg, object tag)
        {
            PrintLine("EndTradeSessionMessage: []");
        }

        public static void PrintPutErgMessage(PutErgMessage msg, object tag)
        {
            int ergID = GetPrivateProperty<int>(msg, "ergID");
            int ergTag = GetPrivateProperty<int>(msg, "tag");
            PrintLine("PutErgMessage: ergID={0} tag={1}", ergID, ergTag);
        }

        public static void PrintPickErgMessage(PickErgMessage msg, object tag)
        {
            int prop = GetPrivateProperty<int>(msg, "prop");
            PrintLine("PickErgMessage: prop={0}", prop);
        }

        public static void PrintUserDSHostTransferStartMessage(UserDSHostTransferStartMessage msg, object tag)
        {
            PrintLine("UserDSHostTransferStartMessage: []");
        }
        public static void PrintUserDSProcessStartMessage(UserDSProcessStartMessage msg, object tag)
        {
            PrintLine("UserDSProcessStartMessage:");
            PrintLine("\tServerAddress={0}", msg.ServerAddress);
            PrintLine("\tServerPort={0}", msg.ServerPort);
            PrintLine("\tEntityID={0}", msg.EntityID);
        }

        public static void PrintStartGameAckMessage(StartGameAckMessage msg, object tag)
        {
            PrintLine("StartGameAckMessage: []");
        }

        public static void PrintShipOptionMessage(ShipOptionMessage msg, object tag)
        {
            PrintLine("ShipOptionMessage:");
            PrintLine(ShipOptionInfoToString(msg.ShipOption,1)); //ServiceCore.EndPointNetwork.ShipOptionInfo
        }

        public static void PrintOpenPartyWithShipInfoMessage(OpenPartyWithShipInfoMessage msg, object tag)
        {
            PrintLine("OpenPartyWithShipInfoMessage:");
            PrintLine("\tQuestID={0}", msg.QuestID);
            PrintLine("\tTitle={0}", msg.Title);
            PrintLine("\tSwearID={0}", msg.SwearID);
            PrintLine(ShipOptionInfoToString(msg.Option,1));
        }

        public static void PrintPvpHostInfoMessage(PvpHostInfoMessage msg, object tag)
        {
            PrintLine("PvpHostInfoMessage:");
            PrintLine("\tGameInfo={0}", msg.GameInfo); //ServiceCore.EndPointNetwork.GameInfo has a toString()
            PrintLine(GameJoinMemberInfoToString(msg.MemberInfo,1)); //ServiceCore.EndPointNetwork.GameJoinMemberInfo has a toString()
            PrintLine(DictToString<string,string>(msg.Config,"Config",1)); //System.Collections.Generic.Dictionary`2[System.String,System.String]
        }

        public static void PrintSetStoryLineStatusMessage(SetStoryLineStatusMessage msg, object tag)
        {
            PrintLine("SetStoryLineStatusMessage:");
            PrintLine("\tStoryLineID={0}", msg.StoryLineID);
            PrintLine("\tStatus={0}", msg.Status);
        }

        public static void PrintQueryShopItemInfoMessage(QueryShopItemInfoMessage msg, object tag)
        {
            PrintLine("QueryShopItemInfoMessage: ShopID={0}", msg.ShopID);
        }

        public static void PrintSelectButtonMessage(SelectButtonMessage msg, object tag)
        {
            PrintLine("SelectButtonMessage: ButtonIndex={0}", msg.ButtonIndex);
        }

        public static void PrintDestroySlotItemMessage(DestroySlotItemMessage msg, object tag)
        {
            long itemID = GetPrivateProperty<long>(msg, "itemID");
            PrintLine("DestroySlotItemMessage: ItemID={0}", itemID);
        }

        public static void PrintCloseUserDS(CloseUserDS msg, object tag)
        {
            PrintLine("CloseUserDS:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintLaunchUserDS(LaunchUserDS msg, object tag)
        {
            PrintLine("LaunchUserDS:");
            PrintLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            PrintLine("\tMicroPlayID={0}", msg.MicroPlayID); //System.Int64 has a toString()
            PrintLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
            PrintLine("\tFrontendID={0}", msg.FrontendID); //System.Int64 has a toString()
            PrintLine("\tIsAdultMode={0}", msg.IsAdultMode); //System.Boolean has a toString()
            PrintLine("\tIsPracticeMode={0}", msg.IsPracticeMode); //System.Boolean has a toString()
            PrintLine("\tHostIP={0}", msg.HostIP); //System.Net.IPAddress has a toString()
            PrintLine("\tUserDSRealHostCID={0}", msg.UserDSRealHostCID); //System.Int64 has a toString()
            PrintLine("\tServerAddress={0}", msg.ServerAddress); //System.String has a toString()
            PrintLine("\tUserDSEntityID={0}", msg.UserDSEntityID); //System.Int64 has a toString()
            PrintLine("\tPort={0}", msg.Port); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintSendPacketUserDS(SendPacketUserDS msg, object tag)
        {
            PrintLine("SendPacketUserDS:");
            PrintLine("\tPacket={0}", msg.Packet); //Devcat.Core.Net.Message.Packet has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUserDSHostTransferStart(UserDSHostTransferStart msg, object tag)
        {
            PrintLine("UserDSHostTransferStart:");
            PrintLine("\tUserDSRealHostCID={0}", msg.UserDSRealHostCID); //System.Int64 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUpdateArenaWinCount(UpdateArenaWinCount msg, object tag)
        {
            PrintLine("UpdateArenaWinCount:");
            PrintLine("\tArenaWinCount={0}", msg.ArenaWinCount); //System.Int32 has a toString()
            PrintLine("\tArenaLoseCount={0}", msg.ArenaLoseCount); //System.Int32 has a toString()
            PrintLine("\tArenaSuccessiveWinCount={0}", msg.ArenaSuccessiveWinCount); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryDSServiceInfo(QueryDSServiceInfo msg, object tag)
        {
            PrintLine("QueryDSServiceInfo:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryPvpDSInfos(QueryPvpDSInfos msg, object tag)
        {
            PrintLine("QueryPvpDSInfos:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRegisterDSEntity(RegisterDSEntity msg, object tag)
        {
            PrintLine("RegisterDSEntity:");
            PrintLine("\tServiceID={0}", msg.ServiceID); //System.Int32 has a toString()
            PrintLine("\tCoreCount={0}", msg.CoreCount); //System.Int32 has a toString()
            PrintLine("\tGiantRaidMachine={0}", msg.GiantRaidMachine); //System.Boolean has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRemoveDSEntity(RemoveDSEntity msg, object tag)
        {
            PrintLine("RemoveDSEntity:");
            PrintLine("\tDSID={0}", msg.DSID); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintDSServiceInfoMessage(DSServiceInfoMessage msg, object tag)
        {
            PrintLine("DSServiceInfoMessage:");
        }

        public static void PrintQueryDSServiceInfoMessage(QueryDSServiceInfoMessage msg, object tag)
        {
            PrintLine("QueryDSServiceInfoMessage:");
        }

        public static void PrintArenaResultInfoMessage(ArenaResultInfoMessage msg, object tag)
        {
            PrintLine("ArenaResultInfoMessage:");
            PrintLine("\tArenaWinCount={0}", msg.ArenaWinCount); //System.Int32 has a toString()
            PrintLine("\tArenaLoseCount={0}", msg.ArenaLoseCount); //System.Int32 has a toString()
            PrintLine("\tArenaSuccessiveWinCount={0}", msg.ArenaSuccessiveWinCount); //System.Int32 has a toString()
        }

        public static void PrintPvpPenaltyMessage(PvpPenaltyMessage msg, object tag)
        {
            PrintLine("PvpPenaltyMessage:");
            PrintLine("\tPlayerIndex={0}", msg.PlayerIndex); //System.Int32 has a toString()
        }

        public static void PrintPvpRegisterGameWaitingQueueMessage(PvpRegisterGameWaitingQueueMessage msg, object tag)
        {
            PrintLine("PvpRegisterGameWaitingQueueMessage:");
            PrintLine("\tGameIndex={0}", msg.GameIndex); //System.Int32 has a toString()
        }

        public static void PrintPvpUnregisterGameWaitingQueueMessage(PvpUnregisterGameWaitingQueueMessage msg, object tag)
        {
            PrintLine("PvpUnregisterGameWaitingQueueMessage:");
        }

        public static void PrintSetStoryHintCategoryMessage(SetStoryHintCategoryMessage msg, object tag)
        {
            PrintLine("SetStoryHintCategoryMessage:");
            PrintLine("\tCategory={0}", msg.Category); //System.Int32 has a toString()
        }

        public static void PrintPvpUnregisterGameWaitingQueue(PvpUnregisterGameWaitingQueue msg, object tag)
        {
            PrintLine("PvpUnregisterGameWaitingQueue:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintPvpGivePanalty(PvpGivePanalty msg, object tag)
        {
            PrintLine("PvpGivePanalty:");
            PrintLine("\tPlayerIndex={0}", msg.PlayerIndex); //System.Int32 has a toString()
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintSetStoryHintCategory(SetStoryHintCategory msg, object tag)
        {
            PrintLine("SetStoryHintCategory:");
            PrintLine("\tCategory={0}", msg.Category); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintWhisperResultToGameClient(WhisperResultToGameClient msg, object tag)
        {
            PrintLine("WhisperResultToGameClient:");
            PrintLine("\tMyCID={0}", msg.MyCID); //System.Int64 has a toString()
            PrintLine("\tResultNo={0}", msg.ResultNo); //System.Int32 has a toString()
            PrintLine("\tReceiverName={0}", msg.ReceiverName); //System.String has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintWhisperToGameClient(WhisperToGameClient msg, object tag)
        {
            PrintLine("WhisperToGameClient:");
            PrintLine("\tFrom={0}", msg.From); //System.String has a toString()
            PrintLine("\tToCID={0}", msg.ToCID); //System.Int64 has a toString()
            PrintLine("\tMessage={0}", msg.Message); //System.String has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintStopStorytelling(StopStorytelling msg, object tag)
        {
            PrintLine("StopStorytelling:");
            PrintLine("\tTargetState={0}", msg.TargetState); //System.Boolean has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryNpcList(QueryNpcList msg, object tag)
        {
            PrintLine("QueryNpcList:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryStoryLog(QueryStoryLog msg, object tag)
        {
            PrintLine("QueryStoryLog:");
            PrintLine("\tStoryLineID={0}", msg.StoryLineID); //System.String has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryStoryProgress(QueryStoryProgress msg, object tag)
        {
            PrintLine("QueryStoryProgress:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintReloadNpcScript(ReloadNpcScript msg, object tag)
        {
            PrintLine("ReloadNpcScript:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintSelectButton(SelectButton msg, object tag)
        {
            PrintLine("SelectButton:");
            PrintLine("\tButtonIndex={0}", msg.ButtonIndex); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryNpcTalk(QueryNpcTalk msg, object tag)
        {
            PrintLine("QueryNpcTalk:");
            PrintLine("\tBuildingID={0}", msg.BuildingID); //System.String has a toString()
            PrintLine("\tNpcID={0}", msg.NpcID); //System.String has a toString()
            PrintLine("\tStoryLine={0}", msg.StoryLine); //System.String has a toString()
            PrintLine("\tCommand={0}", msg.Command); //System.String has a toString()
            PrintLine("\tCheatPermission={0}", msg.CheatPermission); //System.Boolean has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryStoryGuide(QueryStoryGuide msg, object tag)
        {
            PrintLine("QueryStoryGuide:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRankAlarmInfo(RankAlarmInfo msg, object tag)
        {
            PrintLine("RankAlarmInfo:");
            PrintLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
            PrintLine("\tEventID={0}", msg.EventID); //System.Int32 has a toString()
            PrintLine("\tRank={0}", msg.Rank); //System.Int32 has a toString()
        }

        public static void PrintRankResultInfo(RankResultInfo msg, object tag)
        {
            PrintLine("RankResultInfo:");
            PrintLine("\tEventID={0}", msg.EventID); //System.Int32 has a toString()
            PrintLine("\tRank={0}", msg.Rank); //System.Int32 has a toString()
            PrintLine("\tRankPrev={0}", msg.RankPrev); //System.Int32 has a toString()
            PrintLine("\tScore={0}", msg.Score); //System.Int64 has a toString()
            PrintLine("\tCharacterName={0}", msg.CharacterName); //System.String has a toString()
        }

        public static void PrintUpdateRankBasis(UpdateRankBasis msg, object tag)
        {
            PrintLine("UpdateRankBasis:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tRankID={0}", msg.RankID); //System.String has a toString()
            PrintLine("\tScore={0}", msg.Score); //System.Int64 has a toString()
            PrintLine("\tEntityID={0}", msg.EntityID); //System.Int64 has a toString()
            PrintLine("\tGuildName={0}", msg.GuildName); //System.String has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUpdateRankFavorite(UpdateRankFavorite msg, object tag)
        {
            PrintLine("UpdateRankFavorite:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tRankID={0}", msg.RankID); //System.String has a toString()
            PrintLine("\tIsAddition={0}", msg.IsAddition); //System.Boolean has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintCloseConnection(CloseConnection msg, object tag)
        {
            PrintLine("CloseConnection:");
            PrintLine("\tFrontendID={0}", msg.FrontendID); //System.Int64 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintJoinP2PGroup(JoinP2PGroup msg, object tag)
        {
            PrintLine("JoinP2PGroup:");
            PrintLine("\tFrontendID={0}", msg.FrontendID); //System.Int64 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintLeaveP2PGroup(LeaveP2PGroup msg, object tag)
        {
            PrintLine("LeaveP2PGroup:");
            PrintLine("\tFrontendID={0}", msg.FrontendID); //System.Int64 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintReserve(Reserve msg, object tag)
        {
            PrintLine("Reserve:");
            PrintLine("\tFrontendID={0}", msg.FrontendID); //System.Int64 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryRankGoal(QueryRankGoal msg, object tag)
        {
            PrintLine("QueryRankGoal:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tEventID={0}", msg.EventID); //System.Int32 has a toString()
            PrintLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
            PrintLine("\tRivalName={0}", msg.RivalName); //System.String has a toString()
            PrintLine("\tRivalScore={0}", msg.RivalScore); //System.Int64 has a toString()
            PrintLine("\tRivalRank={0}", msg.RivalRank); //System.Int32 has a toString()
            PrintLine("\tRivalRankPrev={0}", msg.RivalRankPrev); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUpdateQuestPlayCount(UpdateQuestPlayCount msg, object tag)
        {
            PrintLine("UpdateQuestPlayCount:");
            PrintLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            PrintLine("\tStartTime={0}", msg.StartTime); //System.DateTime has a toString()
            PrintLine("\tIgnoreTodayCount={0}", msg.IgnoreTodayCount); //System.Boolean has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRemoveUser(RemoveUser msg, object tag)
        {
            PrintLine("RemoveUser:");
            PrintLine("\tCharacterID={0}", msg.CharacterID); //System.Int64 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintInvitedPartyMember(InvitedPartyMember msg, object tag)
        {
            PrintLine("InvitedPartyMember:");
            PrintLine("\tSlotNum={0}", msg.SlotNum); //System.Int32 has a toString()
        }

        public static void PrintUseConsumable(UseConsumable msg, object tag)
        {
            PrintLine("UseConsumable:");
            PrintLine("\tHostCID={0}", msg.HostCID); //System.Int64 has a toString()
            PrintLine("\tUserTag={0}", msg.UserTag); //System.Int64 has a toString()
            PrintLine("\tUsedPart={0}", msg.UsedPart); //System.Int32 has a toString()
            PrintLine("\tInnerSlot={0}", msg.InnerSlot); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintSetStoryLineStatus(SetStoryLineStatus msg, object tag)
        {
            PrintLine("SetStoryLineStatus:");
            PrintLine("\tStoryLineID={0}", msg.StoryLineID); //System.String has a toString()
            PrintLine("\tStatus={0}", msg.Status); //ServiceCore.EndPointNetwork.StoryLineStatus
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintWhisper(Whisper msg, object tag)
        {
            PrintLine("Whisper:");
            PrintLine("\tFrom={0}", msg.From); //System.String has a toString()
            PrintLine("\tTo={0}", msg.To); //System.String has a toString()
            PrintLine("\tMessage={0}", msg.Message); //System.String has a toString()
            PrintLine("\tFailReason={0}", msg.FailReason); //ServiceCore.TalkServiceOperations.Whisper+WhisperResult
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintModifyStoryVariable(ModifyStoryVariable msg, object tag)
        {
            PrintLine("ModifyStoryVariable:");
            PrintLine("\tStoryLine={0}", msg.StoryLine); //System.String has a toString()
            PrintLine("\tKey={0}", msg.Key); //System.String has a toString()
            PrintLine("\tValue={0}", msg.Value); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintNotifyStoryEvent(NotifyStoryEvent msg, object tag)
        {
            PrintLine("NotifyStoryEvent:");
            PrintLine("\ttype={0}", msg.type); //ServiceCore.StoryServiceOperations.StoryEventType
            PrintLine("\tObject={0}", msg.Object); //System.String has a toString()
            PrintLine("\tIsSuccess={0}", msg.IsSuccess); //System.Boolean has a toString()
            PrintLine("\tStorySectorID={0}", msg.StorySectorID); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintDailyStoryProcess(DailyStoryProcess msg, object tag)
        {
            PrintLine("DailyStoryProcess:");
            PrintLine("\tNextOpTime={0}", msg.NextOpTime); //System.DateTime has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintExecuteServerCommand(ExecuteServerCommand msg, object tag)
        {
            PrintLine("ExecuteServerCommand:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintGiveStory(GiveStory msg, object tag)
        {
            PrintLine("GiveStory:");
            PrintLine("\tStoryLineID={0}", msg.StoryLineID); //System.String has a toString()
            PrintLine("\tFailReason={0}", msg.FailReason); //ServiceCore.StoryServiceOperations.GiveStory+FailReasonEnum
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintGiveQuest(GiveQuest msg, object tag)
        {
            PrintLine("GiveQuest:");
            PrintLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            PrintLine("\tReveal={0}", msg.Reveal); //System.Boolean has a toString()
            PrintLine("\tFailReason={0}", msg.FailReason); //ServiceCore.QuestOwnershipServiceOperations.GiveQuest+FailReasonEnum
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintJoinParty(JoinParty msg, object tag)
        {
            PrintLine("JoinParty:");
            PrintLine("\tCharacterID={0}", msg.CharacterID); //System.Int64 has a toString()
            PrintLine("\tFrontendID={0}", msg.FrontendID); //System.Int64 has a toString()
            PrintLine("\tNexonSN={0}", msg.NexonSN); //System.Int32 has a toString()
            PrintLine("\tJType={0}", msg.JType); //ServiceCore.PartyServiceOperations.JoinType
            PrintLine("\tPushMicroPlayInfo={0}", msg.PushMicroPlayInfo); //System.Boolean has a toString()
            PrintLine("\tSlot={0}", msg.Slot); //System.Int32 has a toString()
            PrintLine("\tPlayID={0}", msg.PlayID); //System.Int64 has a toString()
            PrintLine("\tIsEntranceProcessSkipped={0}", msg.IsEntranceProcessSkipped); //System.Boolean has a toString()
            PrintLine("\tReason={0}", msg.Reason); //System.Object has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintGoalTarget(GoalTarget msg, object tag)
        {
            PrintLine("GoalTarget:");
            PrintLine("\tGoalID={0}", msg.GoalID); //System.Int32 has a toString()
            PrintLine("\tWeight={0}", msg.Weight); //System.Int32 has a toString()
            PrintLine("\tRegex={0}", msg.Regex); //System.String has a toString()
            PrintLine("\tCount={0}", msg.Count); //System.Int32 has a toString()
            PrintLine("\tPositive={0}", msg.Positive); //System.Boolean has a toString()
            PrintLine("\tExp={0}", msg.Exp); //System.Int32 has a toString()
            PrintLine("\tBaseExp={0}", msg.BaseExp); //System.Int32 has a toString()
            PrintLine("\tGold={0}", msg.Gold); //System.Int32 has a toString()
            PrintLine("\tItemReward={0}", msg.ItemReward); //System.String has a toString()
            PrintLine("\tItemNum={0}", msg.ItemNum); //System.Int32 has a toString()
        }

        public static void PrintPartyChat(PartyChat msg, object tag)
        {
            PrintLine("PartyChat:");
            PrintLine("\tSenderSlot={0}", msg.SenderSlot); //System.Int32 has a toString()
            PrintLine("\tMessage={0}", msg.Message); //System.String has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }


        public static void PrintSinkShip(SinkShip msg, object tag)
        {
            PrintLine("SinkShip:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintStopShipping(StopShipping msg, object tag)
        {
            PrintLine("StopShipping:");
            PrintLine("\tTargetState={0}", msg.TargetState); //System.Boolean has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintTransferMaster(TransferMaster msg, object tag)
        {
            PrintLine("TransferMaster:");
            PrintLine("\tMasterCID={0}", msg.MasterCID); //System.Int64 has a toString()
            PrintLine("\tNewMasterCID={0}", msg.NewMasterCID); //System.Int64 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUpdateLatestPing(UpdateLatestPing msg, object tag)
        {
            PrintLine("UpdateLatestPing:");
            PrintLine("\tSlotNum={0}", msg.SlotNum); //System.Int32 has a toString()
            PrintLine("\tLatestPing={0}", msg.LatestPing); //System.Int32 has a toString()
            PrintLine("\tLatestFrameRate={0}", msg.LatestFrameRate); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintPvpReport(PvpReport msg, object tag)
        {
            PrintLine("PvpReport:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tEvent={0}", msg.Event); //ServiceCore.EndPointNetwork.PvpReportType
            PrintLine("\tSubject={0}", msg.Subject); //System.Int32 has a toString()
            PrintLine("\tObject={0}", msg.Object); //System.Int32 has a toString()
            PrintLine("\tArg={0}", msg.Arg); //System.String has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRegisterHostGameInfo(RegisterHostGameInfo msg, object tag)
        {
            PrintLine("RegisterHostGameInfo:");
            PrintLine("\tHostCID={0}", msg.HostCID); //System.Int64 has a toString()
            PrintLine("\tGameInfo={0}", msg.GameInfo); //ServiceCore.EndPointNetwork.GameInfo has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRegisterPvpPlayer(RegisterPvpPlayer msg, object tag)
        {
            PrintLine("RegisterPvpPlayer:");
            PrintLine("\tPvpMode={0}", msg.PvpMode); //System.String has a toString()
                                                             //PrintLine("\tIDList={0}",msg.IDList); //System.Collections.Generic.ICollection`1[ServiceCore.PartyServiceOperations.PvPPartyMemberInfo]
            PrintLine("\tMyCID={0}", msg.MyCID); //System.Int64 has a toString()
            PrintLine("\tCheat={0}", msg.Cheat); //ServiceCore.EndPointNetwork.PvpRegisterCheat
            PrintLine("\tChannelID={0}", msg.ChannelID); //System.Int32 has a toString()
            PrintLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUnregisterPvpPlayer(UnregisterPvpPlayer msg, object tag)
        {
            PrintLine("UnregisterPvpPlayer:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintDailyQuestProcess(DailyQuestProcess msg, object tag)
        {
            PrintLine("DailyQuestProcess:");
            PrintLine("\tNextOpTime={0}", msg.NextOpTime); //System.DateTime has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintNotifyLevelToQuest(NotifyLevelToQuest msg, object tag)
        {
            PrintLine("NotifyLevelToQuest:");
            PrintLine("\tCharacterLevel={0}", msg.CharacterLevel); //System.Int32 has a toString()
            PrintLine("\tCafeType={0}", msg.CafeType); //System.Nullable`1[System.Int32] has a toString()
            PrintLine("\tHasVIPBonusEffect={0}", msg.HasVIPBonusEffect); //System.Nullable`1[System.Boolean] has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintNotifyPermissionsToQuest(NotifyPermissionsToQuest msg, object tag)
        {
            PrintLine("NotifyPermissionsToQuest:");
            PrintLine("\tCharacterPermissions={0}", msg.CharacterPermissions); //ServiceCore.LoginServiceOperations.Permissions
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintNotifyStartQuest(NotifyStartQuest msg, object tag)
        {
            PrintLine("NotifyStartQuest:");
            PrintLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            PrintLine("\tStartTime={0}", msg.StartTime); //System.DateTime has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryAvailableQuestsByQuestSet(QueryAvailableQuestsByQuestSet msg, object tag)
        {
            PrintLine("QueryAvailableQuestsByQuestSet:");
            PrintLine("\tQuestSet={0}", msg.QuestSet); //System.Int32 has a toString()
            PrintLine("\tDifficulty={0}", msg.Difficulty); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryQuestDigest(QueryQuestDigest msg, object tag)
        {
            PrintLine("QueryQuestDigest:");
            PrintLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            PrintLine("\tSwearID={0}", msg.SwearID); //System.Int32 has a toString()
            PrintLine("\tCheckDifficulty={0}", msg.CheckDifficulty); //System.Int32 has a toString()
            PrintLine("\tCheckDisableUserShip={0}", msg.CheckDisableUserShip); //System.Boolean has a toString()
            PrintLine("\tIsPracticeMode={0}", msg.IsPracticeMode); //System.Boolean has a toString()
            PrintLine("\tIsUserDSMode={0}", msg.IsUserDSMode); //System.Boolean has a toString()
            PrintLine("\tIsDropTest={0}", msg.IsDropTest); //System.Boolean has a toString()
            PrintLine("\tQuestDigest={0}", msg.QuestDigest); //ServiceCore.QuestOwnershipServiceOperations.QuestDigest has a toString()
            PrintLine("\tQuestStatus={0}", msg.QuestStatus); //System.Int32 has a toString()
            PrintLine("\tFailReason={0}", msg.FailReason); //ServiceCore.QuestOwnershipServiceOperations.QuestConstraintResult
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryQuestDigestDS(QueryQuestDigestDS msg, object tag)
        {
            PrintLine("QueryQuestDigestDS:");
            PrintLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            PrintLine("\tQuestDigest={0}", msg.QuestDigest); //ServiceCore.QuestOwnershipServiceOperations.QuestDigest has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryQuestProgress(QueryQuestProgress msg, object tag)
        {
            PrintLine("QueryQuestProgress:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryQuestSettings(QueryQuestSettings msg, object tag)
        {
            PrintLine("QueryQuestSettings:");
            PrintLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            PrintLine("\tDifficulty={0}", msg.Difficulty); //System.Int32 has a toString()
                                                                   //PrintLine("\tQuestSettings={0}",msg.QuestSettings); //ServiceCore.QuestOwnershipServiceOperations.QuestSettings
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }


        public static void PrintPvPPartyMemberInfo(PvPPartyMemberInfo msg, object tag)
        {
            PrintLine("PvPPartyMemberInfo:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tFID={0}", msg.FID); //System.Int64 has a toString()
            PrintLine("\tNexonSN={0}", msg.NexonSN); //System.Int32 has a toString()
            PrintLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            PrintLine("\tBaseCharacter={0}", msg.BaseCharacter); //ServiceCore.CharacterServiceOperations.BaseCharacter
            PrintLine("\tName={0}", msg.Name); //System.String has a toString()
            PrintLine("\tGuildID={0}", msg.GuildID); //System.Int32 has a toString()
            PrintLine("\tGuildName={0}", msg.GuildName); //System.String has a toString()
            PrintLine("\tMMOLocation={0}", msg.MMOLocation); //System.Int64 has a toString()
            PrintLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
            PrintLine("\tRewardBonus={0}", msg.RewardBonus); //System.Int32 has a toString()
        }

        public static void PrintSharingResponse(SharingResponse msg, object tag)
        {
            PrintLine("SharingResponse:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tAccept={0}", msg.Accept); //System.Boolean has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintBlockEntering(BlockEntering msg, object tag)
        {
            PrintLine("BlockEntering:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintBreakSinglePlayerParty(BreakSinglePlayerParty msg, object tag)
        {
            PrintLine("BreakSinglePlayerParty:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintForceAllReady(ForceAllReady msg, object tag)
        {
            PrintLine("ForceAllReady:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintGameEnded(GameEnded msg, object tag)
        {
            PrintLine("GameEnded:");
            PrintLine("\tBreakParty={0}", msg.BreakParty); //System.Boolean has a toString()
            PrintLine("\tNoCountSuccess={0}", msg.NoCountSuccess); //System.Boolean has a toString()
            PrintLine("\tSuccessivePartyBonus={0}", msg.SuccessivePartyBonus); //System.Int32 has a toString()
            PrintLine("\tSuccessivePartyCount={0}", msg.SuccessivePartyCount); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintGameStarted(GameStarted msg, object tag)
        {
            PrintLine("GameStarted:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintGameStarting(GameStarting msg, object tag)
        {
            PrintLine("GameStarting:");
            PrintLine("\tFailReason={0}", msg.FailReason); //ServiceCore.PartyServiceOperations.GameStarting+FailReasonEnum
            PrintLine("\tMinPartyCount={0}", msg.MinPartyCount); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintGameStartingFailed(GameStartingFailed msg, object tag)
        {
            PrintLine("GameStartingFailed:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintInvitationRejected(InvitationRejected msg, object tag)
        {
            PrintLine("InvitationRejected:");
            PrintLine("\tInvitedCID={0}", msg.InvitedCID); //System.Int64 has a toString()
            PrintLine("\tInvitedName={0}", msg.InvitedName); //System.String has a toString()
            PrintLine("\tReason={0}", msg.Reason); //ServiceCore.EndPointNetwork.InvitationRejectReason
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintInvitePartyMember(InvitePartyMember msg, object tag)
        {
            PrintLine("InvitePartyMember:");
            PrintLine("\tInvitingCID={0}", msg.InvitingCID); //System.Int64 has a toString()
            PrintLine("\tInvitedCID={0}", msg.InvitedCID); //System.Int64 has a toString()
            PrintLine("\tInvitedName={0}", msg.InvitedName); //System.String has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintKickMember(KickMember msg, object tag)
        {
            PrintLine("KickMember:");
            PrintLine("\tMasterCID={0}", msg.MasterCID); //System.Int64 has a toString()
            PrintLine("\tMemberSlot={0}", msg.MemberSlot); //System.Int32 has a toString()
            PrintLine("\tNexonSN={0}", msg.NexonSN); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }
        public static void PrintQueryMicroPlayInfo(QueryMicroPlayInfo msg, object tag)
        {
            PrintLine("QueryMicroPlayInfo:");
            PrintLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            PrintLine(QuestSectorInfosToString(msg.QuestSectorInfo, "QuestSectorInfos", 1)); //ServiceCore.EndPointNetwork.QuestSectorInfos
            PrintLine("\tHardMode={0}", msg.HardMode); //System.Boolean has a toString()
            PrintLine("\tSoloSector={0}", msg.SoloSector); //System.Int32 has a toString()
            PrintLine("\tTimeLimit={0}", msg.TimeLimit); //System.Int32 has a toString()
            PrintLine("\tQuestLevel={0}", msg.QuestLevel); //System.Int32 has a toString()
            PrintLine("\tIsHuntingQuest={0}", msg.IsHuntingQuest); //System.Boolean has a toString()
            PrintLine("\tInitGameTime={0}", msg.InitGameTime); //System.Int32 has a toString()
            PrintLine("\tSectorMoveGameTime={0}", msg.SectorMoveGameTime); //System.Int32 has a toString()
            PrintLine("\tIsGiantRaid={0}", msg.IsGiantRaid); //System.Boolean has a toString()
            PrintLine("\tIsNoWaitingShip={0}", msg.IsNoWaitingShip); //System.Boolean has a toString()
            PrintLine("\tItemLimit={0}", msg.ItemLimit); //System.String has a toString()
            PrintLine("\tGearLimit={0}", msg.GearLimit); //System.String has a toString()
            PrintLine("\tDifficulty={0}", msg.Difficulty); //System.Int32 has a toString()
            PrintLine("\tIsTimerDecreasing={0}", msg.IsTimerDecreasing); //System.Boolean has a toString()
            PrintLine("\tQuestStartedPlayerCount={0}", msg.QuestStartedPlayerCount); //System.Int32 has a toString()
            PrintLine("\tFailReason={0}", msg.FailReason); //ServiceCore.MicroPlayServiceOperations.QueryMicroPlayInfo+FailReasonEnum
            PrintLine("\tInitItemDropEntities={0}", msg.InitItemDropEntities); //System.Boolean has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRemovePlayer(RemovePlayer msg, object tag)
        {
            PrintLine("RemovePlayer:");
            PrintLine("\tSlotNumber={0}", msg.SlotNumber); //System.Int32 has a toString()
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }


        public static void PrintStartAutoFishing(StartAutoFishing msg, object tag)
        {
            PrintLine("StartAutoFishing:");
            PrintLine("\tSerialNumber={0}", msg.SerialNumber); //System.Int32 has a toString()
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            PrintLine("\tArgument={0}", msg.Argument); //System.String has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintStartFailTimer(StartFailTimer msg, object tag)
        {
            PrintLine("StartFailTimer:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintTransferHostInPlay(TransferHostInPlay msg, object tag)
        {
            PrintLine("TransferHostInPlay:");
            PrintLine("\tNewHostCID={0}", msg.NewHostCID); //System.Int64 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUseInventoryItemInQuest(UseInventoryItemInQuest msg, object tag)
        {
            PrintLine("UseInventoryItemInQuest:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintChannelChat(ServiceCore.MMOChannelServiceOperations.ChannelChat msg, object tag)
        {
            PrintLine("ChannelChat:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tMessage={0}", msg.Message); //System.String has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }


        public static void PrintLeaveChannel(LeaveChannel msg, object tag)
        {
            PrintLine("LeaveChannel:");
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRecommendChannel(ServiceCore.MMOChannelServiceOperations.RecommendChannel msg, object tag)
        {
            PrintLine("RecommendChannel:");
            PrintLine("\tChannelID={0}", msg.ChannelID); //System.Int64 has a toString()
            PrintLine("\tServiceID={0}", msg.ServiceID); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintStartSharing(StartSharing msg, object tag)
        {
            PrintLine("StartSharing:");
            PrintLine("\tSharingInfo={0}", msg.SharingInfo); //ServiceCore.EndPointNetwork.SharingInfo has a toString()
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tTargetsCID=[{0}]", String.Join(",", msg.TargetsCID)); //System.Collections.Generic.List`1[System.Int64]
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryHostCID(QueryHostCID msg, object tag)
        {
            PrintLine("QueryHostCID:");
            PrintLine("\tAvailableHosts=[{0}]", String.Join(",", msg.AvailableHosts)); //System.Collections.Generic.List`1[System.Int64]
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryMemberState(QueryMemberState msg, object tag)
        {
            PrintLine("QueryMemberState:");
            PrintLine("\tSlot={0}", msg.Slot); //System.Int32 has a toString()
            PrintLine("\tMemberState={0}", msg.MemberState); //ServiceCore.EndPointNetwork.ReadyState
            PrintLine("\tPartyState={0}", msg.PartyState); //ServiceCore.EndPointNetwork.PartyInfoState
            PrintLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            PrintLine("\tIsInGameJoinAllowed={0}", msg.IsInGameJoinAllowed); //System.Boolean has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }
        public static void PrintQueryShipInfo(QueryShipInfo msg, object tag)
        {
            PrintLine("QueryShipInfo:");
            PrintLine("\tShipInfo={0}", msg.ShipInfo); //ServiceCore.EndPointNetwork.ShipInfo has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintShipLaunched(ShipLaunched msg, object tag)
        {
            PrintLine("ShipLaunched:");
            PrintLine("\tHostCID={0}", msg.HostCID); //System.Int64 has a toString()
            PrintLine("\tMicroPlayID={0}", msg.MicroPlayID); //System.Int64 has a toString()
            PrintLine("\tQuestLevel={0}", msg.QuestLevel); //System.Int32 has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUseExtraStorageMessage(UseExtraStorageMessage msg, object tag)
        {
            PrintLine("UseExtraStorageMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tStorageID={0}", msg.StorageID); //System.Byte has a toString()
        }

        public static void PrintUseInventoryItemOkMessage(UseInventoryItemOkMessage msg, object tag)
        {
            PrintLine("UseInventoryItemOkMessage:");
            PrintLine("\tUseItemClass={0}", msg.UseItemClass); //System.String has a toString()
        }

        public static void PrintUseInventoryItemFailedMessage(UseInventoryItemFailedMessage msg, object tag)
        {
            PrintLine("UseInventoryItemFailedMessage:");
            PrintLine("\tUseItemClass={0}", msg.UseItemClass); //System.String has a toString()
            PrintLine("\tReason={0}", msg.Reason); //System.String has a toString()
        }

        public static void PrintVocationLearnSkillMessage(VocationLearnSkillMessage msg, object tag)
        {
            PrintLine("VocationLearnSkillMessage:");
            PrintLine("\tSkillID={0}", msg.SkillID); //System.String has a toString()
        }

        public static void PrintVocationLevelUpMessage(VocationLevelUpMessage msg, object tag)
        {
            PrintLine("VocationLevelUpMessage:");
            PrintLine("\tVocationClass={0}", msg.VocationClass); //ServiceCore.CharacterServiceOperations.VocationEnum
            PrintLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
        }

        public static void PrintVocationTransformFinishedMessage(VocationTransformFinishedMessage msg, object tag)
        {
            PrintLine("VocationTransformFinishedMessage:");
            PrintLine("\tSlotNum={0}", msg.SlotNum); //System.Int32 has a toString()
            PrintLine("\tTotalDamage={0}", msg.TotalDamage); //System.Int32 has a toString()
        }

        public static void PrintVocationTransformMessage(VocationTransformMessage msg, object tag)
        {
            PrintLine("VocationTransformMessage:");
            PrintLine("\tSlotNum={0}", msg.SlotNum); //System.Int32 has a toString()
            PrintLine("\tTransformLevel={0}", msg.TransformLevel); //System.Int32 has a toString()
        }

        public static void PrintWaitTicketMessage(WaitTicketMessage msg, object tag)
        {
            PrintLine("WaitTicketMessage:");
            PrintLine("\tQueueSpeed={0}", msg.QueueSpeed); //System.Int32 has a toString()
            PrintLine("\tPosition={0}", msg.Position); //System.Int32 has a toString()
        }

        public static void PrintUpdateWhisperFilterMessage(UpdateWhisperFilterMessage msg, object tag)
        {
            PrintLine("UpdateWhisperFilterMessage:");
            PrintLine("\tOperationType={0}", msg.OperationType); //System.Int32 has a toString()
            PrintLine("\tTargetID={0}", msg.TargetID); //System.String has a toString()
            PrintLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
        }

        public static void PrintRequestWhisperMessage(RequestWhisperMessage msg, object tag)
        {
            PrintLine("RequestWhisperMessage:");
            PrintLine("\tReceiver={0}", msg.Receiver); //System.String has a toString()
            PrintLine("\tContents={0}", msg.Contents); //System.String has a toString()
        }

        public static void PrintWhisperMessage(WhisperMessage msg, object tag)
        {
            PrintLine("WhisperMessage:");
            PrintLine("\tSender={0}", msg.Sender); //System.String has a toString()
            PrintLine("\tContents={0}", msg.Contents); //System.String has a toString()
        }

        public static void PrintWhisperFailMessage(WhisperFailMessage msg, object tag)
        {
            PrintLine("WhisperFailMessage:");
            PrintLine("\tToName={0}", msg.ToName); //System.String has a toString()
            PrintLine("\tReason={0}", msg.Reason); //System.String has a toString()
        }

        public static void Print_CheatCommandMessage(_CheatCommandMessage msg, object tag)
        {
            PrintLine("_CheatCommandMessage:");
            PrintLine("\tService={0}", msg.Service); //System.String has a toString()
            PrintLine("\tCommand={0}", msg.Command); //System.String has a toString()
            PrintLine("\tIsEntityOp={0}", msg.IsEntityOp); //System.Boolean has a toString()
        }

        public static void Print_CheatSetCafeStatusMessage(_CheatSetCafeStatusMessage msg, object tag)
        {
            PrintLine("_CheatSetCafeStatusMessage:");
            PrintLine("\tCafeLevel={0}", msg.CafeLevel); //System.Int32 has a toString()
            PrintLine("\tCafeType={0}", msg.CafeType); //System.Int32 has a toString()
            PrintLine("\tSecureCode={0}", msg.SecureCode); //System.Int32 has a toString()
        }

        public static void Print_CheatSetLevelMessage(_CheatSetLevelMessage msg, object tag)
        {
            PrintLine("_CheatSetLevelMessage:");
            PrintLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            PrintLine("\tExpPercent={0}", msg.ExpPercent); //System.Int32 has a toString()
        }

        public static void PrintUseConsumableMessage(UseConsumableMessage msg, object tag)
        {
            PrintLine("UseConsumableMessage:");
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            PrintLine("\tPart={0}", msg.Part); //System.Int32 has a toString()
            PrintLine("\tSlot={0}", msg.Slot); //System.Int32 has a toString()
        }

        public static void PrintUpdateStorageStatusMessage(UpdateStorageStatusMessage msg, object tag)
        {
            PrintLine("UpdateStorageStatusMessage:");
            PrintLine("\tStorageNo={0}", msg.StorageNo); //System.Int32 has a toString()
            PrintLine("\tStorageName={0}", msg.StorageName); //System.String has a toString()
            PrintLine("\tStorageTag={0}", msg.StorageTag); //System.Int32 has a toString()
        }
        public static void PrintSwitchChannelMessage(SwitchChannelMessage msg, object tag)
        {
            PrintLine("SwitchChannelMessage:");
            PrintLine("\tOldChannelID={0}", msg.OldChannelID); //System.Int64 has a toString()
            PrintLine("\tNewChannelID={0}", msg.NewChannelID); //System.Int64 has a toString()
        }

        public static void PrintSynthesisItemMessage(SynthesisItemMessage msg, object tag)
        {
            PrintLine("SynthesisItemMessage:");
            PrintLine("\tBaseItemID={0}", msg.BaseItemID); //System.Int64 has a toString()
            PrintLine("\tLookItemID={0}", msg.LookItemID); //System.Int64 has a toString()
            PrintLine("\tAdditionalItemClass={0}", msg.AdditionalItemClass); //System.String has a toString()
        }

        public static void PrintTeacherAssistJoin(TeacherAssistJoin msg, object tag)
        {
            PrintLine("TeacherAssistJoin:");
            PrintLine("\tShipID={0}", msg.ShipID); //System.Int64 has a toString()
        }

        public static void PrintTeacherRequest(TeacherRequest msg, object tag)
        {
            PrintLine("TeacherRequest:");
        }

        public static void PrintTeacherRequestNotice(TeacherRequestNotice msg, object tag)
        {
            PrintLine("TeacherRequestNotice:");
            PrintLine("\tCharacterName={0}", msg.CharacterName); //System.String has a toString()
            PrintLine("\tIsNotice={0}", msg.IsNotice); //System.Boolean has a toString()
        }

        public static void PrintTeacherRequestRespond(TeacherRequestRespond msg, object tag)
        {
            PrintLine("TeacherRequestRespond:");
            PrintLine("\tAccepted={0}", msg.Accepted); //System.Boolean has a toString()
        }

        public static void PrintTeacherRequestResult(TeacherRequestResult msg, object tag)
        {
            PrintLine("TeacherRequestResult:");
            PrintLine("\tAccepted={0}", msg.Accepted); //System.Boolean has a toString()
            PrintLine("\tAcceptedUserName={0}", msg.AcceptedUserName); //System.String has a toString()
        }

        public static void PrintTestMessage(TestMessage msg, object tag)
        {
            PrintLine("TestMessage:");
            PrintLine("\tName={0}", msg.Name); //System.String has a toString()
            PrintLine("\tID={0}", msg.ID); //System.Int32 has a toString()
        }

        public static void PrintPvpUnregisterMessage(PvpUnregisterMessage msg, object tag)
        {
            PrintLine("PvpUnregisterMessage:");
        }

        public static void PrintTownCampfireEffectCSMessage(TownCampfireEffectCSMessage msg, object tag)
        {
            PrintLine("TownCampfireEffectCSMessage:");
            PrintLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            PrintLine("\tType={0}", msg.Type); //System.Int32 has a toString()
        }

        public static void PrintTriggerEventMessage(TriggerEventMessage msg, object tag)
        {
            PrintLine("TriggerEventMessage:");
            PrintLine("\tEventName={0}", msg.EventName); //System.String has a toString()
            PrintLine("\tArg={0}", msg.Arg); //System.String has a toString()
        }

        public static void PrintSkillCompletionMessage(SkillCompletionMessage msg, object tag)
        {
            PrintLine("SkillCompletionMessage:");
            PrintLine("\tSkillID={0}", msg.SkillID); //System.String has a toString()
            PrintLine("\tSkillRank={0}", msg.SkillRank); //System.Int32 has a toString()
        }

        public static void PrintStartAutoFishingMessage(StartAutoFishingMessage msg, object tag)
        {
            PrintLine("StartAutoFishingMessage:");
            PrintLine("\tSerialNumber={0}", msg.SerialNumber); //System.Int32 has a toString()
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            PrintLine("\tArgument={0}", msg.Argument); //System.String has a toString()
        }

        public static void PrintAutoFishingStartedMessage(AutoFishingStartedMessage msg, object tag)
        {
            PrintLine("AutoFishingStartedMessage:");
            PrintLine("\tSerialNumber={0}", msg.SerialNumber); //System.Int32 has a toString()
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            PrintLine("\tModel={0}", msg.Model); //System.String has a toString()
            PrintLine("\tActionTimeInSeconds={0}", msg.ActionTimeInSeconds); //System.Int32 has a toString()
            PrintLine("\tTimeoutInSeconds={0}", msg.TimeoutInSeconds); //System.Int32 has a toString()
            PrintLine("\tIsCaughtAtHit={0}", msg.IsCaughtAtHit); //System.Boolean has a toString()
            PrintLine("\tIsCaughtAtMiss={0}", msg.IsCaughtAtMiss); //System.Boolean has a toString()
            PrintLine("\tIsCaughtAtTimeout={0}", msg.IsCaughtAtTimeout); //System.Boolean has a toString()
        }

        public static void PrintCatchAutoFishMessage(CatchAutoFishMessage msg, object tag)
        {
            PrintLine("CatchAutoFishMessage:");
            PrintLine("\tSerialNumber={0}", msg.SerialNumber); //System.Int32 has a toString()
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            PrintLine("\tCatchTimeInSeconds={0}", msg.CatchTimeInSeconds); //System.Int32 has a toString()
        }

        public static void PrintLostAutoFishMessage(LostAutoFishMessage msg, object tag)
        {
            PrintLine("LostAutoFishMessage:");
            PrintLine("\tSerialNumber={0}", msg.SerialNumber); //System.Int32 has a toString()
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
        }

        public static void PrintAutoFishingDeniedMessage(AutoFishingDeniedMessage msg, object tag)
        {
            PrintLine("AutoFishingDeniedMessage:");
            PrintLine("\tSerialNumber={0}", msg.SerialNumber); //System.Int32 has a toString()
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
        }

        public static void PrintCancelAutoFishingMessage(CancelAutoFishingMessage msg, object tag)
        {
            PrintLine("CancelAutoFishingMessage:");
            PrintLine("\tSerialNumber={0}", msg.SerialNumber); //System.Int32 has a toString()
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
        }

        public static void PrintStartTradeSessionMessage(StartTradeSessionMessage msg, object tag)
        {
            PrintLine("StartTradeSessionMessage:");
        }

        public static void PrintStartTradeSessionResultMessage(StartTradeSessionResultMessage msg, object tag)
        {
            PrintLine("StartTradeSessionResultMessage:");
            PrintLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
        }

        public static void PrintSharingCheckMessage(SharingCheckMessage msg, object tag)
        {
            PrintLine("SharingCheckMessage:");
            PrintLine("\tSharingCharacterName={0}", msg.SharingCharacterName); //System.String has a toString()
            PrintLine("\tItemClassName={0}", msg.ItemClassName); //System.String has a toString()
            PrintLine("\tStatusEffect={0}", msg.StatusEffect); //System.String has a toString()
            PrintLine("\tEffectLevel={0}", msg.EffectLevel); //System.Int32 has a toString()
            PrintLine("\tDurationSec={0}", msg.DurationSec); //System.Int32 has a toString()
        }

        public static void PrintSharingResponseMessage(SharingResponseMessage msg, object tag)
        {
            PrintLine("SharingResponseMessage:");
            PrintLine("\tAccepted={0}", msg.Accepted); //System.Boolean has a toString()
        }

        public static void PrintShipInfoMessage(ShipInfoMessage msg, object tag)
        {
            PrintLine("ShipInfoMessage:");
            PrintLine("\tShipInfo={0}", msg.ShipInfo); //ServiceCore.EndPointNetwork.ShipInfo has a toString()
        }

        public static void PrintShipNotLaunchedMessage(ShipNotLaunchedMessage msg, object tag)
        {
            PrintLine("ShipNotLaunchedMessage: []");
        }

        public static void PrintServerCmdMessage(ServerCmdMessage msg, object tag)
        {
            PrintLine("ServerCmdMessage:");
            PrintLine("\tCommand={0}", msg.Command); //System.String has a toString()
            PrintLine("\tReliable={0}", msg.Reliable); //System.Boolean has a toString()
        }

        public static void PrintSetLearningSkillMessage(SetLearningSkillMessage msg, object tag)
        {
            PrintLine("SetLearningSkillMessage:");
            PrintLine("\tSkillID={0}", msg.SkillID); //System.String has a toString()
        }

        public static void PrintSetRaidPartyMessage(SetRaidPartyMessage msg, object tag)
        {
            PrintLine("SetRaidPartyMessage:");
            PrintLine("\tIsRaidParty={0}", msg.IsRaidParty); //System.Boolean has a toString()
        }

        public static void PrintSetSecondPasswordMessage(SetSecondPasswordMessage msg, object tag)
        {
            PrintLine("SetSecondPasswordMessage:");
            PrintLine("\tNewPassword={0}", msg.NewPassword); //System.String has a toString()
        }

        public static void PrintSetSpSkillMessage(SetSpSkillMessage msg, object tag)
        {
            PrintLine("SetSpSkillMessage:");
            PrintLine("\tSlotID={0}", msg.SlotID); //System.Int32 has a toString()
            PrintLine("\tSkillID={0}", msg.SkillID); //System.String has a toString()
        }

        public static void PrintQuestReinforceMessage(QuestReinforceMessage msg, object tag)
        {
            PrintLine("QuestReinforceMessage:");
            PrintLine("\tShipID={0}", msg.ShipID); //System.Int64 has a toString()
        }

        public static void PrintAddReinforcementMessage(AddReinforcementMessage msg, object tag)
        {
            PrintLine("AddReinforcementMessage:");
            PrintLine("\tUID={0}", msg.UID); //System.Int32 has a toString()
            PrintLine("\tMemberInfo={0}", msg.MemberInfo); //ServiceCore.EndPointNetwork.GameJoinMemberInfo has a toString()
        }

        public static void PrintQuestReinforceGrantedMessage(QuestReinforceGrantedMessage msg, object tag)
        {
            PrintLine("QuestReinforceGrantedMessage:");
            PrintLine("\tGameInfo={0}", msg.GameInfo); //ServiceCore.EndPointNetwork.GameInfo has a toString()
            PrintLine("\tUID={0}", msg.UID); //System.Int32 has a toString()
            PrintLine("\tKey={0}", msg.Key); //System.Int32 has a toString()
        }

        public static void PrintReadyMessage(ReadyMessage msg, object tag)
        {
            PrintLine("ReadyMessage:");
            PrintLine("\tReady={0}", msg.Ready); //System.Byte has a toString()
        }

        public static void PrintPlayerRevivedMessage(PlayerRevivedMessage msg, object tag)
        {
            PrintLine("PlayerRevivedMessage:");
            PrintLine("\tCasterTag={0}", msg.CasterTag); //System.Int32 has a toString()
            PrintLine("\tReviverTag={0}", msg.ReviverTag); //System.Int32 has a toString()
            PrintLine("\tMethod={0}", msg.Method); //System.String has a toString()
        }

        public static void PrintReleaseLearningSkillMessage(ReleaseLearningSkillMessage msg, object tag)
        {
            PrintLine("ReleaseLearningSkillMessage:");
        }

        public static void PrintReloadNpcScriptMessage(ReloadNpcScriptMessage msg, object tag)
        {
            PrintLine("ReloadNpcScriptMessage:");
        }

        public static void PrintRequestCraftMessage(RequestCraftMessage msg, object tag)
        {
            PrintLine("RequestCraftMessage:");
            PrintLine("\tShopID={0}", msg.ShopID); //System.String has a toString()
            PrintLine("\tOrder={0}", msg.Order); //System.Int32 has a toString()
            PrintLine("\tCount={0}", msg.Count); //System.Int32 has a toString()
        }

        public static void PrintRequestShutDownMessage(RequestShutDownMessage msg, object tag)
        {
            PrintLine("RequestShutDownMessage:");
            PrintLine("\tDelay={0}", msg.Delay); //System.Int32 has a toString()
        }

        public static void PrintRequestStoryStatusMessage(RequestStoryStatusMessage msg, object tag)
        {
            PrintLine("RequestStoryStatusMessage:");
        }

        public static void PrintReturnMailMessage(ReturnMailMessage msg, object tag)
        {
            PrintLine("ReturnMailMessage:");
            PrintLine("\tMailID={0}", msg.MailID); //System.Int64 has a toString()
        }

        public static void PrintSelectTitleMessage(SelectTitleMessage msg, object tag)
        {
            PrintLine("SelectTitleMessage:");
            PrintLine("\tTitle={0}", msg.Title); //System.Int32 has a toString()
        }

        public static void PrintSellItemMessage(SellItemMessage msg, object tag)
        {
            PrintLine("SellItemMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tCount={0}", msg.Count); //System.Int32 has a toString()
        }

        public static void PrintMailReceivedMessage(MailReceivedMessage msg, object tag)
        {
            PrintLine("MailReceivedMessage:");
            PrintLine("\tFromName={0}", msg.FromName); //System.String has a toString()
            PrintLine("\tMailType={0}", msg.MailType); //System.Byte has a toString()
        }

        public static void PrintMailSentMessage(MailSentMessage msg, object tag)
        {
            PrintLine("MailSentMessage:");
            PrintLine("\tErrorCode={0}", msg.ErrorCode); //System.Int32 has a toString()
        }

        public static void PrintMemberInfoMessage(MemberInfoMessage msg, object tag)
        {
            PrintLine("MemberInfoMessage:");
            PrintLine("\tUID={0}", msg.UID); //System.Int32 has a toString()
            PrintLine("\tMemberInfo={0}", msg.MemberInfo); //ServiceCore.EndPointNetwork.GameJoinMemberInfo has a toString()
        }

        public static void PrintModifyStoryStatusMessage(ModifyStoryStatusMessage msg, object tag)
        {
            PrintLine("ModifyStoryStatusMessage:");
            PrintLine("\tToken={0}", msg.Token); //System.String has a toString()
            PrintLine("\tState={0}", msg.State); //System.Int32 has a toString()
        }

        public static void PrintModifyStoryVariableMessage(ModifyStoryVariableMessage msg, object tag)
        {
            PrintLine("ModifyStoryVariableMessage:");
            PrintLine("\tStoryLine={0}", msg.StoryLine); //System.String has a toString()
            PrintLine("\tKey={0}", msg.Key); //System.String has a toString()
            PrintLine("\tValue={0}", msg.Value); //System.Int32 has a toString()
        }

        public static void PrintNoticeMessage(NoticeMessage msg, object tag)
        {
            PrintLine("NoticeMessage:");
            PrintLine("\tMessage={0}", msg.Message); //System.String has a toString()
        }

        public static void PrintNoticeShutDownMessage(NoticeShutDownMessage msg, object tag)
        {
            PrintLine("NoticeShutDownMessage:");
            PrintLine("\tDelay={0}", msg.Delay); //System.Int32 has a toString()
        }

        public static void PrintNotifyForceStartMessage(NotifyForceStartMessage msg, object tag)
        {
            PrintLine("NotifyForceStartMessage:");
            PrintLine("\tUntilForceStart={0}", msg.UntilForceStart); //System.Int32 has a toString()
        }

        public static void PrintPartyChatMessage(PartyChatMessage msg, object tag)
        {
            PrintLine("PartyChatMessage:");
            PrintLine("\tSenderName={0}", msg.SenderName); //System.String has a toString()
            PrintLine("\tMessage={0}", msg.Message); //System.String has a toString()
            PrintLine("\tisFakeTownChat={0}", msg.isFakeTownChat); //System.Boolean has a toString()
        }

        public static void PrintPartyChatSendMessage(PartyChatSendMessage msg, object tag)
        {
            PrintLine("PartyChatSendMessage:");
            PrintLine("\tMessage={0}", msg.Message); //System.String has a toString()
        }

        public static void PrintPartyInvitationAcceptFailedMessage(PartyInvitationAcceptFailedMessage msg, object tag)
        {
            PrintLine("PartyInvitationAcceptFailedMessage:");
        }

        public static void PrintPartyInvitationAcceptMessage(PartyInvitationAcceptMessage msg, object tag)
        {
            PrintLine("PartyInvitationAcceptMessage:");
            PrintLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
        }

        public static void PrintPartyInvitationRejectedMessage(PartyInvitationRejectedMessage msg, object tag)
        {
            PrintLine("PartyInvitationRejectedMessage:");
            PrintLine("\tInvitedName={0}", msg.InvitedName); //System.String has a toString()
            PrintLine("\tReason={0}", msg.Reason); //System.Int32 has a toString()
        }

        public static void PrintPartyInvitationRejectMessage(PartyInvitationRejectMessage msg, object tag)
        {
            PrintLine("PartyInvitationRejectMessage:");
            PrintLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
        }

        public static void PrintPartyInvitedMessage(PartyInvitedMessage msg, object tag)
        {
            PrintLine("PartyInvitedMessage:");
            PrintLine("\tHostName={0}", msg.HostName); //System.String has a toString()
            PrintLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
        }

        public static void PrintPartyInviteMessage(PartyInviteMessage msg, object tag)
        {
            PrintLine("PartyInviteMessage:");
            PrintLine("\tInvitedName={0}", msg.InvitedName); //System.String has a toString()
        }

        public static void PrintPartyOptionMessage(PartyOptionMessage msg, object tag)
        {
            PrintLine("PartyOptionMessage:");
            PrintLine("\tRestTime={0}", msg.RestTime); //System.Int32 has a toString()
        }

        public static void PrintPingMessage(PingMessage msg, object tag)
        {
            PrintLine("PingMessage:");
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
        }

        public static void PrintPlayerKilledMessage(PlayerKilledMessage msg, object tag)
        {
            PrintLine("PlayerKilledMessage:");
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
        }

        public static void PrintPongMessage(PongMessage msg, object tag)
        {
            PrintLine("PongMessage:");
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
        }

        public static void PrintQueryAPMessage(QueryAPMessage msg, object tag)
        {
            PrintLine("QueryAPMessage:");
        }

        public static void PrintQueryEquipmentMessage(QueryEquipmentMessage msg, object tag)
        {
            PrintLine("QueryEquipmentMessage:");
        }

        public static void PrintQueryMailListMessage(QueryMailListMessage msg, object tag)
        {
            PrintLine("QueryMailListMessage:");
        }

        public static void PrintQueryNpcListMessage(QueryNpcListMessage msg, object tag)
        {
            PrintLine("QueryNpcListMessage:");
        }

        public static void PrintQueryQuestsMessage(QueryQuestsMessage msg, object tag)
        {
            PrintLine("QueryQuestsMessage:");
        }

        public static void PrintQuerySectorEntitiesMessage(QuerySectorEntitiesMessage msg, object tag)
        {
            PrintLine("QuerySectorEntitiesMessage:");
            PrintLine("\tSector={0}", msg.Sector); //System.String has a toString()
        }

        public static void PrintQueryShipInfoMessage(QueryShipInfoMessage msg, object tag)
        {
            PrintLine("QueryShipInfoMessage:");
            PrintLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
        }

        public static void PrintQueryShipListMessage(QueryShipListMessage msg, object tag)
        {
            PrintLine("QueryShipListMessage:");
        }

        public static void PrintQuerySkillListMessage(QuerySkillListMessage msg, object tag)
        {
            PrintLine("QuerySkillListMessage:");
        }

        public static void PrintQueryStatMessage(QueryStatMessage msg, object tag)
        {
            PrintLine("QueryStatMessage:");
        }

        public static void PrintQueryStoryGuideMessage(QueryStoryGuideMessage msg, object tag)
        {
            PrintLine("QueryStoryGuideMessage:");
        }

        public static void PrintQueryStoryLinesMessage(QueryStoryLinesMessage msg, object tag)
        {
            PrintLine("QueryStoryLinesMessage:");
        }

        public static void PrintQueryStoryVariablesMessage(QueryStoryVariablesMessage msg, object tag)
        {
            PrintLine("QueryStoryVariablesMessage:");
        }

        public static void PrintLoginFailMessage(LoginFailMessage msg, object tag)
        {
            PrintLine("LoginFailMessage:");
            PrintLine("\tReason={0}", msg.Reason); //System.Int32 has a toString()
            PrintLine("\tBannedReason={0}", msg.BannedReason); //System.String has a toString()
        }

        public static void PrintCreateCharacterFailMessage(CreateCharacterFailMessage msg, object tag)
        {
            PrintLine("CreateCharacterFailMessage:");
            PrintLine("\tErrorCode={0}", msg.ErrorCode); //System.Int32 has a toString()
        }

        public static void PrintPurchaseCharacterSlotMessage(PurchaseCharacterSlotMessage msg, object tag)
        {
            PrintLine("PurchaseCharacterSlotMessage:");
            PrintLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
            PrintLine("\tIsPremiumSlot={0}", msg.IsPremiumSlot); //System.Boolean has a toString()
            PrintLine("\tIsCredit={0}", msg.IsCredit); //System.Boolean has a toString()
        }

        public static void PrintQueryCharacterNameChangeMessage(QueryCharacterNameChangeMessage msg, object tag)
        {
            PrintLine("QueryCharacterNameChangeMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tRequestName={0}", msg.RequestName); //System.String has a toString()
            PrintLine("\tIsTrans={0}", msg.IsTrans); //System.Boolean has a toString()
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
        }

        public static void PrintCharacterNameChangeMessage(CharacterNameChangeMessage msg, object tag)
        {
            PrintLine("CharacterNameChangeMessage:");
            PrintLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tName={0}", msg.Name); //System.String has a toString()
            PrintLine("\tIsTrans={0}", msg.IsTrans); //System.Boolean has a toString()
        }

        public static void PrintLoggedOutMessage(LoggedOutMessage msg, object tag)
        {
            PrintLine("LoggedOutMessage:");
        }

        public static void PrintHackShieldRespondMessage(HackShieldRespondMessage msg, object tag)
        {
            PrintLine("HackShieldRespondMessage:");
            //PrintLine("\tRespond={0}",msg.Respond); //System.Byte[]
            PrintLine("\tHackCode={0}", msg.HackCode); //System.Int32 has a toString()
            PrintLine("\tHackParam={0}", msg.HackParam); //System.String has a toString()
            PrintLine("\tCheckSum={0}", msg.CheckSum); //System.Int64 has a toString()
        }

        public static void PrintHostInfoMessage(HostInfoMessage msg, object tag)
        {
            PrintLine("HostInfoMessage:");
            PrintLine("\tGameInfo={0}", msg.GameInfo); //ServiceCore.EndPointNetwork.GameInfo has a toString()
            PrintLine("\tUID={0}", msg.UID); //System.Int32 has a toString()
            PrintLine("\tKey={0}", msg.Key); //System.Int32 has a toString()
            PrintLine("\tSkipLobby={0}", msg.SkipLobby); //System.Boolean has a toString()
            PrintLine("\tIsTransferToDS={0}", msg.IsTransferToDS); //System.Boolean has a toString()
        }

        public static void PrintJoinShipMessage(JoinShipMessage msg, object tag)
        {
            PrintLine("JoinShipMessage:");
            PrintLine("\tShipID={0}", msg.ShipID); //System.Int64 has a toString()
            PrintLine("\tIsAssist={0}", msg.IsAssist); //System.Boolean has a toString()
            PrintLine("\tIsInTownWithShipInfo={0}", msg.IsInTownWithShipInfo); //System.Boolean has a toString()
            PrintLine("\tIsDedicatedServer={0}", msg.IsDedicatedServer); //System.Boolean has a toString()
            PrintLine("\tIsNewbieRecommend={0}", msg.IsNewbieRecommend); //System.Boolean has a toString()
        }

        public static void PrintKickMessage(KickMessage msg, object tag)
        {
            PrintLine("KickMessage:");
            PrintLine("\tSlot={0}", msg.Slot); //System.Int32 has a toString()
            PrintLine("\tNexonSN={0}", msg.NexonSN); //System.Int32 has a toString()
        }

        public static void PrintKilledMessage(KilledMessage msg, object tag)
        {
            PrintLine("KilledMessage:");
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            PrintLine("\tTargetKind={0}", msg.TargetKind); //System.Int32 has a toString()
            PrintLine("\tTargetName={0}", msg.TargetName); //System.String has a toString()
        }

        public static void PrintLearningSkillChangedMessage(LearningSkillChangedMessage msg, object tag)
        {
            PrintLine("LearningSkillChangedMessage:");
            PrintLine("\tSkillID={0}", msg.SkillID); //System.String has a toString()
            PrintLine("\tAP={0}", msg.AP); //System.Int32 has a toString()
        }

        public static void PrintDestroyItemMessage(DestroyItemMessage msg, object tag)
        {
            PrintLine("DestroyItemMessage:");
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            PrintLine("\tNum={0}", msg.Num); //System.Int32 has a toString()
        }

        public static void PrintGameStartedMessage(GameStartedMessage msg, object tag)
        {
            PrintLine("GameStartedMessage: []");
        }

        public static void PrintReturnToShipMessage(ReturnToShipMessage msg, object tag)
        {
            PrintLine("ReturnToShipMessage:");
        }

        public static void PrintCompleteSkillMessage(CompleteSkillMessage msg, object tag)
        {
            PrintLine("CompleteSkillMessage:");
        }

        public static void PrintCreateItemMessage(CreateItemMessage msg, object tag)
        {
            PrintLine("CreateItemMessage:");
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            PrintLine("\tItemNum={0}", msg.ItemNum); //System.Int32 has a toString()
        }

        public static void PrintCreateShipMessage(CreateShipMessage msg, object tag)
        {
            PrintLine("CreateShipMessage:");
            PrintLine("\tQuestName={0}", msg.QuestName); //System.String has a toString()
        }

        public static void PrintCustomSectorQuestMessage(CustomSectorQuestMessage msg, object tag)
        {
            PrintLine("CustomSectorQuestMessage:");
            PrintLine("\tTargetSector={0}", msg.TargetSector); //System.String has a toString()
        }

        public static void PrintDeleteCharacterMessage(DeleteCharacterMessage msg, object tag)
        {
            PrintLine("DeleteCharacterMessage:");
            PrintLine("\tName={0}", msg.Name); //System.String has a toString()
            PrintLine("\tCharacterSN={0}", msg.CharacterSN); //System.Int32 has a toString()
        }

        public static void PrintPvpRegisterMessage(PvpRegisterMessage msg, object tag)
        {
            PrintLine("PvpRegisterMessage:");
            PrintLine("\tPvpMode={0}", msg.PvpMode); //System.String has a toString()
            PrintLine("\tCheat={0}", msg.Cheat); //ServiceCore.EndPointNetwork.PvpRegisterCheat
            PrintLine("\tChannelID={0}", msg.ChannelID); //System.Int32 has a toString()
        }

        public static void PrintPvpMemberInfoMessage(PvpMemberInfoMessage msg, object tag)
        {
            PrintLine("PvpMemberInfoMessage:");
            PrintLine("\tMemberInfo={0}", msg.MemberInfo); //ServiceCore.EndPointNetwork.GameJoinMemberInfo has a toString()
            PrintLine("\tTeamID={0}", msg.TeamID); //System.Int32 has a toString()
        }

        public static void PrintPvpStartGameMessage(PvpStartGameMessage msg, object tag)
        {
            PrintLine("PvpStartGameMessage:");
        }

        public static void PrintUnequippablePartsInfoMessage(UnequippablePartsInfoMessage msg, object tag)
        {
            PrintLine("UnequippablePartsInfoMessage:");
            PrintLine("\tParts=[{0}]",String.Join(",",msg.Parts)); //System.Collections.Generic.ICollection`1[System.Int32]
        }

        public static void PrintGetMailItemCompletedMessage(GetMailItemCompletedMessage msg, object tag)
        {
            PrintLine("GetMailItemCompletedMessage:");
            PrintLine("\tMailID={0}", msg.MailID); //System.Int64 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Byte has a toString()
        }

        public static void PrintInsertTradeOrderMessage(InsertTradeOrderMessage msg, object tag)
        {
            PrintLine("InsertTradeOrderMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tNum={0}", msg.Num); //System.Int32 has a toString()
            PrintLine("\tDurationMin={0}", msg.DurationMin); //System.Int32 has a toString()
            PrintLine("\tUnitPrice={0}", msg.UnitPrice); //System.Int32 has a toString()
            PrintLine("\tTradeType={0}", msg.TradeType); //System.Byte has a toString()
        }

        public static void PrintInsertTradeOrderResultMessage(InsertTradeOrderResultMessage msg, object tag)
        {
            PrintLine("InsertTradeOrderResultMessage:");
            PrintLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
        }

        public static void PrintInsertTradeTirOrderMessage(InsertTradeTirOrderMessage msg, object tag)
        {
            PrintLine("InsertTradeTirOrderMessage:");
            PrintLine("\tNum={0}", msg.Num); //System.Int32 has a toString()
            PrintLine("\tDurationMin={0}", msg.DurationMin); //System.Int32 has a toString()
            PrintLine("\tUnitPrice={0}", msg.UnitPrice); //System.Int32 has a toString()
        }

        public static void PrintJoinChannelMessage(JoinChannelMessage msg, object tag)
        {
            PrintLine("JoinChannelMessage:");
            PrintLine("\tChannelID={0}", msg.ChannelID); //System.Int64 has a toString()
        }

        public static void PrintLeaveChannelMessage(LeaveChannelMessage msg, object tag)
        {
            PrintLine("LeaveChannelMessage:");
            PrintLine("\tChannelID={0}", msg.ChannelID); //System.Int64 has a toString()
        }

        public static void PrintPayCoinMessage(PayCoinMessage msg, object tag)
        {
            PrintLine("PayCoinMessage:");
            PrintLine("\tCoinType={0}", msg.CoinType); //ServiceCore.EndPointNetwork.CoinType
            PrintLine("\tReceiverSlot={0}", msg.ReceiverSlot); //System.Int32 has a toString()
            PrintLine("\tCoinSlot={0}", msg.CoinSlot); //System.Int32 has a toString()
            PrintLine("\tIsInsert={0}", msg.IsInsert); //System.Boolean has a toString()
        }

        public static void PrintPvpEndGameMessage(PvpEndGameMessage msg, object tag)
        {
            PrintLine("PvpEndGameMessage:");
        }

        public static void PrintNexonSNByNameMessage(NexonSNByNameMessage msg, object tag)
        {
            PrintLine("NexonSNByNameMessage:");
            PrintLine("\tQueryID={0}", msg.QueryID); //System.Int64 has a toString()
            PrintLine("\tUID={0}", msg.UID); //System.Int32 has a toString()
        }

        public static void PrintQueryAddBattleInventoryMessage(QueryAddBattleInventoryMessage msg, object tag)
        {
            PrintLine("QueryAddBattleInventoryMessage:");
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            PrintLine("\tSlotNum={0}", msg.SlotNum); //System.Int32 has a toString()
            PrintLine("\tIsFree={0}", msg.IsFree); //System.Boolean has a toString()
        }

        public static void PrintQueryCashShopGiftSenderMessage(QueryCashShopGiftSenderMessage msg, object tag)
        {
            PrintLine("QueryCashShopGiftSenderMessage:");
            PrintLine("\tOrderNo={0}", msg.OrderNo); //System.Int32 has a toString()
        }

        public static void PrintQueryFishingResultMessage(QueryFishingResultMessage msg, object tag)
        {
            PrintLine("QueryFishingResultMessage:");
        }

        public static void PrintQueryNewRecipesMessage(QueryNewRecipesMessage msg, object tag)
        {
            PrintLine("QueryNewRecipesMessage:");
        }

        public static void PrintQueryNexonSNByNameMessage(QueryNexonSNByNameMessage msg, object tag)
        {
            PrintLine("QueryNexonSNByNameMessage:");
            PrintLine("\tQueryID={0}", msg.QueryID); //System.Int64 has a toString()
            PrintLine("\tcName={0}", msg.cName); //System.String has a toString()
        }

        public static void PrintClientCmdMessage(ClientCmdMessage msg, object tag)
        {
            PrintLine("ClientCmdMessage:");
            PrintLine("\tCommand={0}", msg.Command); //System.String has a toString()
        }

        public static void PrintDeleteCharacterResultMessage(DeleteCharacterResultMessage msg, object tag)
        {
            PrintLine("DeleteCharacterResultMessage:");
            PrintLine("\tCharacterSN={0}", msg.CharacterSN); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.DeleteCharacterResult
            PrintLine("\tRemainTimeSec={0}", msg.RemainTimeSec); //System.Int32 has a toString()
        }

        public static void PrintDyeItemMessage(DyeItemMessage msg, object tag)
        {
            PrintLine("DyeItemMessage:");
            PrintLine("\tStartNewSession={0}", msg.StartNewSession); //System.Boolean has a toString()
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tIsPremium={0}", msg.IsPremium); //System.Boolean has a toString()
        }

        public static void PrintRequestSetRankFavoriteInfoMessage(RequestSetRankFavoriteInfoMessage msg, object tag)
        {
            PrintLine("RequestSetRankFavoriteInfoMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tRankID={0}", msg.RankID); //System.String has a toString()
            PrintLine("\tIsAddition={0}", msg.IsAddition); //System.Boolean has a toString()
        }

        public static void PrintRequestSetRankGoalInfoMessage(RequestSetRankGoalInfoMessage msg, object tag)
        {
            PrintLine("RequestSetRankGoalInfoMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tRankID={0}", msg.RankID); //System.String has a toString()
        }

        public static void PrintRemoveExpiredCashItemMessage(RemoveExpiredCashItemMessage msg, object tag)
        {
            PrintLine("RemoveExpiredCashItemMessage:");
        }

        public static void PrintQueryRankAllInfoMessage(QueryRankAllInfoMessage msg, object tag)
        {
            PrintLine("QueryRankAllInfoMessage:");
            PrintLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintQueryRankCharacterInfoMessage(QueryRankCharacterInfoMessage msg, object tag)
        {
            PrintLine("QueryRankCharacterInfoMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintQueryRankFavoritesInfoMessage(QueryRankFavoritesInfoMessage msg, object tag)
        {
            PrintLine("QueryRankFavoritesInfoMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintQueryRankGoalInfoMessage(QueryRankGoalInfoMessage msg, object tag)
        {
            PrintLine("QueryRankGoalInfoMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tEventID={0}", msg.EventID); //System.Int32 has a toString()
            PrintLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintQueryRankHeavyFavoritesInfoMessage(QueryRankHeavyFavoritesInfoMessage msg, object tag)
        {
            PrintLine("QueryRankHeavyFavoritesInfoMessage:");
        }

        public static void PrintManufactureLevelUpMessage(ManufactureLevelUpMessage msg, object tag)
        {
            PrintLine("ManufactureLevelUpMessage:");
            PrintLine("\tMID={0}", msg.MID); //System.String has a toString()
            PrintLine("\tGrade={0}", msg.Grade); //System.Int32 has a toString()
        }

        public static void PrintIdentifyFailed(IdentifyFailed msg, object tag)
        {
            PrintLine("IdentifyFailed:");
            PrintLine("\tErrorMessage={0}", msg.ErrorMessage); //System.String has a toString()
        }

        public static void PrintChannelChanged(ChannelChanged msg, object tag)
        {
            PrintLine("ChannelChanged:");
            PrintLine("\tChannelID={0}", msg.ChannelID); //System.Int64 has a toString()
        }

        public static void PrintChat(Chat msg, object tag)
        {
            PrintLine("Chat:");
            PrintLine("\tMessage={0}", msg.Message); //System.String has a toString()
        }

        public static void PrintLeave(Leave msg, object tag)
        {
            PrintLine("Leave:");
        }

        public static void PrintNotifyChat(NotifyChat msg, object tag)
        {
            PrintLine("NotifyChat:");
            PrintLine("\tName={0}", msg.Name); //System.String has a toString()
            PrintLine("\tMessage={0}", msg.Message); //System.String has a toString()
        }

        public static void PrintPvpCommandMessage(PvpCommandMessage msg, object tag)
        {
            PrintLine("PvpCommandMessage:");
            PrintLine("\tCommandInt={0}", msg.CommandInt); //System.Int32 has a toString()
            PrintLine("\tArg={0}", msg.Arg); //System.String has a toString()
        }

        public static void PrintHostRestartedMessage(HostRestartedMessage msg, object tag)
        {
            PrintLine("HostRestartedMessage:");
            PrintLine("\tGameInfo={0}", msg.GameInfo); //ServiceCore.EndPointNetwork.GameInfo has a toString()
        }

        public static void PrintHostRestartingMessage(HostRestartingMessage msg, object tag)
        {
            PrintLine("HostRestartingMessage:");
        }

        public static void PrintEnchantItemMessage(EnchantItemMessage msg, object tag)
        {
            PrintLine("EnchantItemMessage:");
            PrintLine("\tOpenNewSession={0}", msg.OpenNewSession); //System.Boolean has a toString()
            PrintLine("\tTargetItemID={0}", msg.TargetItemID); //System.Int64 has a toString()
            PrintLine("\tEnchantScrollItemID={0}", msg.EnchantScrollItemID); //System.Int64 has a toString()
            PrintLine("\tIndestructibleScrollItemID={0}", msg.IndestructibleScrollItemID); //System.Int64 has a toString()
            PrintLine("\tDiceItemClass={0}", msg.DiceItemClass); //System.String has a toString()
        }

        public static void PrintTradePurchaseItemMessage(TradePurchaseItemMessage msg, object tag)
        {
            PrintLine("TradePurchaseItemMessage:");
            PrintLine("\tTID={0}", msg.TID); //System.Int64 has a toString()
            PrintLine("\tPurchaseCount={0}", msg.PurchaseCount); //System.Int32 has a toString()
            PrintLine("\tUniqueNumber={0}", msg.UniqueNumber); //System.Int32 has a toString()
        }

        public static void PrintTradePurchaseItemResultMessage(TradePurchaseItemResultMessage msg, object tag)
        {
            PrintLine("TradePurchaseItemResultMessage:");
            PrintLine("\tUniqueNumber={0}", msg.UniqueNumber); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
            PrintLine("\tLeftNumber={0}", msg.LeftNumber); //System.Int32 has a toString()
        }

        public static void PrintTradeRequestMyItemMessage(TradeRequestMyItemMessage msg, object tag)
        {
            PrintLine("TradeRequestMyItemMessage:");
        }

        public static void PrintMovePetSlotMessage(MovePetSlotMessage msg, object tag)
        {
            PrintLine("MovePetSlotMessage:");
            PrintLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
            PrintLine("\tDestSlotNo={0}", msg.DestSlotNo); //System.Int32 has a toString()
        }

        public static void PrintRemovePetMessage(RemovePetMessage msg, object tag)
        {
            PrintLine("RemovePetMessage:");
            PrintLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
        }

        public static void PrintSelectPetMessage(SelectPetMessage msg, object tag)
        {
            PrintLine("SelectPetMessage:");
            PrintLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
        }

        public static void PrintQueryPetListMessage(QueryPetListMessage msg, object tag)
        {
            PrintLine("QueryPetListMessage:");
        }

        public static void PrintPetRevivedMessage(PetRevivedMessage msg, object tag)
        {
            PrintLine("PetRevivedMessage:");
            PrintLine("\tCasterTag={0}", msg.CasterTag); //System.Int32 has a toString()
            PrintLine("\tReviverTag={0}", msg.ReviverTag); //System.Int32 has a toString()
            PrintLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
            PrintLine("\tMethod={0}", msg.Method); //System.String has a toString()
        }

        public static void PrintPetKilledMessage(PetKilledMessage msg, object tag)
        {
            PrintLine("PetKilledMessage:");
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            PrintLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
        }

        public static void PrintRemovePetSkillMessage(RemovePetSkillMessage msg, object tag)
        {
            PrintLine("RemovePetSkillMessage:");
            PrintLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
            PrintLine("\tPetSkillID={0}", msg.PetSkillID); //System.Int32 has a toString()
        }

        public static void PrintPetCreateNameCheckMessage(PetCreateNameCheckMessage msg, object tag)
        {
            PrintLine("PetCreateNameCheckMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tPetName={0}", msg.PetName); //System.String has a toString()
        }

        public static void PrintPetCreateNameCheckResultMessage(PetCreateNameCheckResultMessage msg, object tag)
        {
            PrintLine("PetCreateNameCheckResultMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tPetName={0}", msg.PetName); //System.String has a toString()
            PrintLine("\tIsSuccess={0}", msg.IsSuccess); //System.Boolean has a toString()
        }

        public static void PrintPetCreateMessage(PetCreateMessage msg, object tag)
        {
            PrintLine("PetCreateMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tPetName={0}", msg.PetName); //System.String has a toString()
        }

        public static void PrintUpdateCurrentPetMessage(UpdateCurrentPetMessage msg, object tag)
        {
            PrintLine("UpdateCurrentPetMessage:");
            PrintLine("\tUID={0}", msg.UID); //System.Int32 has a toString()
            PrintLine("\tCurrentPet={0}", msg.CurrentPet); //ServiceCore.EndPointNetwork.PetStatusInfo has a toString()
        }

        public static void PrintRequestPetFoodShareMessage(RequestPetFoodShareMessage msg, object tag)
        {
            PrintLine("RequestPetFoodShareMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
        }

        public static void PrintRequestPetFoodUnshareMessage(RequestPetFoodUnshareMessage msg, object tag)
        {
            PrintLine("RequestPetFoodUnshareMessage:");
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
        }

        public static void PrintPetChangeNameCheckMessage(PetChangeNameCheckMessage msg, object tag)
        {
            PrintLine("PetChangeNameCheckMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
            PrintLine("\tPetName={0}", msg.PetName); //System.String has a toString()
        }

        public static void PrintPetChangeNameCheckResultMessage(PetChangeNameCheckResultMessage msg, object tag)
        {
            PrintLine("PetChangeNameCheckResultMessage:");
            PrintLine("\tIsSuccess={0}", msg.IsSuccess); //System.Boolean has a toString()
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
            PrintLine("\tPetName={0}", msg.PetName); //System.String has a toString()
            PrintLine("\tResultType={0}", msg.ResultType); //System.String has a toString()
        }

        public static void PrintPetChangeNameMessage(PetChangeNameMessage msg, object tag)
        {
            PrintLine("PetChangeNameMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
            PrintLine("\tPetName={0}", msg.PetName); //System.String has a toString()
        }

        public static void PrintTcProtectRespondMessage(TcProtectRespondMessage msg, object tag)
        {
            PrintLine("TcProtectRespondMessage:");
            PrintLine("\tMd5Check={0}", msg.Md5Check); //System.Int32 has a toString()
            PrintLine("\tImpressCheck={0}", msg.ImpressCheck); //System.Int32 has a toString()
        }

        public static void PrintTradeCancelMyItemMessage(TradeCancelMyItemMessage msg, object tag)
        {
            PrintLine("TradeCancelMyItemMessage:");
            PrintLine("\tTID={0}", msg.TID); //System.Int64 has a toString()
            PrintLine("\tUniqueNumber={0}", msg.UniqueNumber); //System.Int32 has a toString()
        }

        public static void PrintTradeCancelMyItemResultMessage(TradeCancelMyItemResultMessage msg, object tag)
        {
            PrintLine("TradeCancelMyItemResultMessage:");
            PrintLine("\tUniqueNumber={0}", msg.UniqueNumber); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
        }

        public static void PrintDetailOption(DetailOption msg, object tag)
        {
            PrintLine("DetailOption:");
            PrintLine("\tKey={0}", msg.Key); //System.String has a toString()
            PrintLine("\tValue={0}", msg.Value); //System.Int32 has a toString()
            PrintLine("\tSearchType={0}", msg.SearchType); //System.Byte has a toString()
        }

        public static void PrintResetSkillMessage(ResetSkillMessage msg, object tag)
        {
            PrintLine("ResetSkillMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tSkillID={0}", msg.SkillID); //System.String has a toString()
            PrintLine("\tAfterRank={0}", msg.AfterRank); //System.Int32 has a toString()
        }

        public static void PrintUseMegaphoneMessage(UseMegaphoneMessage msg, object tag)
        {
            PrintLine("UseMegaphoneMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tMessageType={0}", msg.MessageType); //System.Int32 has a toString()
            PrintLine("\tMessage={0}", msg.Message); //System.String has a toString()
        }

        public static void PrintMegaphoneMessage(MegaphoneMessage msg, object tag)
        {
            PrintLine("MegaphoneMessage:");
            PrintLine("\tMessageType={0}", msg.MessageType); //System.Int32 has a toString()
            PrintLine("\tSenderName={0}", msg.SenderName); //System.String has a toString()
            PrintLine("\tMessage={0}", msg.Message); //System.String has a toString()
        }

        public static void PrintGuildNoticeEditMessage(GuildNoticeEditMessage msg, object tag)
        {
            PrintLine("GuildNoticeEditMessage:");
            PrintLine("\tText={0}", msg.Text); //System.String has a toString()
        }

        public static void PrintGuildStoryRequestMessage(GuildStoryRequestMessage msg, object tag)
        {
            PrintLine("GuildStoryRequestMessage:");
        }

        public static void PrintDyeAmpleUsedMessage(DyeAmpleUsedMessage msg, object tag)
        {
            PrintLine("DyeAmpleUsedMessage:");
            PrintLine("\tColorTable={0}", msg.ColorTable); //System.String has a toString()
            PrintLine("\tSeed1={0}", msg.Seed1); //System.Int32 has a toString()
            PrintLine("\tSeed2={0}", msg.Seed2); //System.Int32 has a toString()
            PrintLine("\tSeed3={0}", msg.Seed3); //System.Int32 has a toString()
            PrintLine("\tSeed4={0}", msg.Seed4); //System.Int32 has a toString()
        }

        public static void PrintDyeItemCashMessage(DyeItemCashMessage msg, object tag)
        {
            PrintLine("DyeItemCashMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tAmpleID={0}", msg.AmpleID); //System.Int64 has a toString()
            PrintLine("\tPart={0}", msg.Part); //System.Int32 has a toString()
        }

        public static void PrintDeleteCharacterCancelMessage(DeleteCharacterCancelMessage msg, object tag)
        {
            PrintLine("DeleteCharacterCancelMessage:");
            PrintLine("\tName={0}", msg.Name); //System.String has a toString()
            PrintLine("\tCharacterSN={0}", msg.CharacterSN); //System.Int32 has a toString()
        }

        public static void PrintDeleteCharacterCancelResultMessage(DeleteCharacterCancelResultMessage msg, object tag)
        {
            PrintLine("DeleteCharacterCancelResultMessage:");
            PrintLine("\tCharacterSN={0}", msg.CharacterSN); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.CharacterList.DeleteCharacterCancelResult
        }

        public static void PrintChatReportMessage(ChatReportMessage msg, object tag)
        {
            PrintLine("ChatReportMessage:");
            PrintLine("\tm_Name={0}", msg.m_Name); //System.String has a toString()
            PrintLine("\tm_Type={0}", msg.m_Type); //System.Int32 has a toString()
            PrintLine("\tm_Reason={0}", msg.m_Reason); //System.String has a toString()
            PrintLine("\tm_ChatLog={0}", msg.m_ChatLog); //System.String has a toString()
        }

        public static void PrintCompletedMissionInfoMessage(CompletedMissionInfoMessage msg, object tag)
        {
            PrintLine("CompletedMissionInfoMessage:");
        }

        public static void PrintDSCommandMessage(DSCommandMessage msg, object tag)
        {
            PrintLine("DSCommandMessage:");
            PrintLine("\tCommandType={0}", msg.CommandType); //System.Int32 has a toString()
            PrintLine("\tCommand={0}", msg.Command); //System.String has a toString()
            PrintLine("\tDSCommandType={0}", msg.DSCommandType); //ServiceCore.EndPointNetwork.DS.DSCommandType
        }

        public static void PrintDSPlayerStatusMessage(DSPlayerStatusMessage msg, object tag)
        {
            PrintLine("DSPlayerStatusMessage:");
            PrintLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            PrintLine("\tStatus={0}", msg.Status); //ServiceCore.EndPointNetwork.DS.DSPlayerStatus
            PrintLine("\tRegisterTimeDiff={0}", msg.RegisterTimeDiff); //System.Int32 has a toString()
            PrintLine("\tOrderCount={0}", msg.OrderCount); //System.Int32 has a toString()
            PrintLine("\tMemberCount={0}", msg.MemberCount); //System.Int32 has a toString()
            PrintLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
            PrintLine("\tReason={0}", msg.Reason); //System.String has a toString()
            PrintLine("\tIsGiantRaid={0}", msg.IsGiantRaid); //System.Boolean has a toString()
        }

        public static void PrintRegisterDSQueueMessage(RegisterDSQueueMessage msg, object tag)
        {
            PrintLine("RegisterDSQueueMessage:");
            PrintLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
        }

        public static void PrintUnregisterDSQueueMessage(UnregisterDSQueueMessage msg, object tag)
        {
            PrintLine("UnregisterDSQueueMessage:");
            PrintLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
        }

        public static void PrintDyeAmpleCompleteMessage(DyeAmpleCompleteMessage msg, object tag)
        {
            PrintLine("DyeAmpleCompleteMessage:");
            PrintLine("\tColor={0}", msg.Color); //System.Int32 has a toString()
        }

        public static void PrintEffectTelepathyMessage(EffectTelepathyMessage msg, object tag)
        {
            PrintLine("EffectTelepathyMessage:");
            PrintLine("\tIsEffectFail={0}", msg.IsEffectFail); //System.Boolean has a toString()
            PrintLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            PrintLine("\tEffectName={0}", msg.EffectName); //System.String has a toString()
        }

        public static void PrintExpandExpirationDateItemMessage(ExpandExpirationDateItemMessage msg, object tag)
        {
            PrintLine("ExpandExpirationDateItemMessage:");
            PrintLine("\tMessageType={0}", msg.MessageType); //ServiceCore.EndPointNetwork.ExpandExpirationDateItemMessageType
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tExpanderName={0}", msg.ExpanderName); //System.String has a toString()
            PrintLine("\tExpanderPrice={0}", msg.ExpanderPrice); //System.Int32 has a toString()
            PrintLine("\tIsCredit={0}", msg.IsCredit); //System.Boolean has a toString()
        }

        public static void PrintFreeMatchReportMessage(FreeMatchReportMessage msg, object tag)
        {
            PrintLine("FreeMatchReportMessage:");
            PrintLine("\tWinnerTag={0}", msg.WinnerTag); //System.Int32 has a toString()
            PrintLine("\tLoserTag={0}", msg.LoserTag); //System.Int32 has a toString()
        }

        public static void PrintCloseGuildMessage(CloseGuildMessage msg, object tag)
        {
            PrintLine("CloseGuildMessage:");
        }

        public static void PrintInviteGuildMessage(InviteGuildMessage msg, object tag)
        {
            PrintLine("InviteGuildMessage:");
            PrintLine("\tName={0}", msg.Name); //System.String has a toString()
        }

        public static void PrintJoinGuildMessage(JoinGuildMessage msg, object tag)
        {
            PrintLine("JoinGuildMessage:");
            PrintLine("\tGuildSN={0}", msg.GuildSN); //System.Int32 has a toString()
        }

        public static void PrintLeaveGuildMessage(LeaveGuildMessage msg, object tag)
        {
            PrintLine("LeaveGuildMessage:");
        }

        public static void PrintReconnectGuildMessage(ReconnectGuildMessage msg, object tag)
        {
            PrintLine("ReconnectGuildMessage:");
        }

        public static void PrintMegaphoneFailMessage(MegaphoneFailMessage msg, object tag)
        {
            PrintLine("MegaphoneFailMessage:");
            PrintLine("\tErrorCode={0}", msg.ErrorCode); //System.Int32 has a toString()
        }

        public static void PrintNotifyPlayerReconnectMessage(NotifyPlayerReconnectMessage msg, object tag)
        {
            PrintLine("NotifyPlayerReconnectMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tPlayerName={0}", msg.PlayerName); //System.String has a toString()
        }

        public static void PrintAnswerPlayerReconnectMessage(AnswerPlayerReconnectMessage msg, object tag)
        {
            PrintLine("AnswerPlayerReconnectMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tPlayerName={0}", msg.PlayerName); //System.String has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryBuybackListMessage(QueryBuybackListMessage msg, object tag)
        {
            PrintLine("QueryBuybackListMessage: []");
        }

        public static void PrintBuybackListResultMessage(BuybackListResultMessage msg, object tag)
        {
            PrintLine(ListToString<BuybackInfo>(msg.BuybackList, "BuybackListResultMessage", 0));
        }

        public static void PrintQueryCashShopItemPickUpMessage(QueryCashShopItemPickUpMessage msg, object tag)
        {
            PrintLine("QueryCashShopItemPickUpMessage:");
        }

        public static void PrintQueryCashShopPurchaseItemMessage(QueryCashShopPurchaseItemMessage msg, object tag)
        {
            PrintLine("QueryCashShopPurchaseItemMessage:");
        }

        public static void PrintQueryCashShopPurchaseGiftMessage(QueryCashShopPurchaseGiftMessage msg, object tag)
        {
            PrintLine("QueryCashShopPurchaseGiftMessage:");
        }

        public static void PrintCashShopFailMessage(CashShopFailMessage msg, object tag)
        {
            PrintLine("CashShopFailMessage:");
        }

        public static void PrintRequestGoddessProtectionMessage(RequestGoddessProtectionMessage msg, object tag)
        {
            PrintLine("RequestGoddessProtectionMessage:");
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
        }

        public static void PrintDirectPurchaseCashShopItemMessage(DirectPurchaseCashShopItemMessage msg, object tag)
        {
            PrintLine("DirectPurchaseCashShopItemMessage:");
            PrintLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
            PrintLine("\tcashType={0}", msg.cashType); //System.Int32 has a toString()
        }

        public static void PrintBuyItemMessage(BuyItemMessage msg, object tag)
        {
            PrintLine("BuyItemMessage:");
            PrintLine("\tShopID={0}", msg.ShopID); //System.String has a toString()
            PrintLine("\tOrder={0}", msg.Order); //System.Int32 has a toString()
            PrintLine("\tCount={0}", msg.Count); //System.Int32 has a toString()
        }

        public static void PrintBuyItemResultMessage(BuyItemResultMessage msg, object tag)
        {
            PrintLine("BuyItemResultMessage:");
            PrintLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            PrintLine("\tItemCount={0}", msg.ItemCount); //System.Int32 has a toString()
            PrintLine("\tPriceItemClass={0}", msg.PriceItemClass); //System.String has a toString()
            PrintLine("\tPriceItemCount={0}", msg.PriceItemCount); //System.Int32 has a toString()
            PrintLine("\tRestrictionItemOrder={0}", msg.RestrictionItemOrder); //System.Int32 has a toString()
            PrintLine("\tRestrictionItemCount={0}", msg.RestrictionItemCount); //System.Int32 has a toString()
        }

        public static void PrintRejoinCombatSuccessMessage(RejoinCombatSuccessMessage msg, object tag)
        {
            PrintLine("RejoinCombatSuccessMessage:");
        }

        public static void PrintAcceptAssistMessage(AcceptAssistMessage msg, object tag)
        {
            PrintLine("AcceptAssistMessage:");
            PrintLine("\tSlot={0}", msg.Slot); //System.Int32 has a toString()
        }

        public static void PrintAdditionalMicroPlayContentsMessage(AdditionalMicroPlayContentsMessage msg, object tag)
        {
            PrintLine("AdditionalMicroPlayContentsMessage:");
            PrintLine("\tHostCommandString={0}", msg.HostCommandString); //System.String has a toString()
        }

        public static void PrintDestroyMicroPlayContentsMessage(DestroyMicroPlayContentsMessage msg, object tag)
        {
            PrintLine("DestroyMicroPlayContentsMessage:");
            PrintLine("\tEntityID={0}", msg.EntityID); //System.String has a toString()
        }

        public static void PrintWishListDeleteResponseMessage(WishListDeleteResponseMessage msg, object tag)
        {
            PrintLine("WishListDeleteResponseMessage:");
            PrintLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.WishListResult
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
        }

        public static void PrintUseFeverPointMessage(UseFeverPointMessage msg, object tag)
        {
            PrintLine("UseFeverPointMessage:");
        }

        public static void PrintUseInventoryItemWithTargetMessage(UseInventoryItemWithTargetMessage msg, object tag)
        {
            PrintLine("UseInventoryItemWithTargetMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tTargetName={0}", msg.TargetName); //System.String has a toString()
        }

        public static void PrintUserCareItemOpenMessage(UserCareItemOpenMessage msg, object tag)
        {
            PrintLine("UserCareItemOpenMessage:");
            PrintLine("\tIndex={0}", msg.Index); //System.Int32 has a toString()
        }

        public static void PrintUserDSHostConnectionQuery(UserDSHostConnectionQuery msg, object tag)
        {
            PrintLine("UserDSHostConnectionQuery:");
        }

        public static void PrintUserDSLaunchMessage(UserDSLaunchMessage msg, object tag)
        {
            PrintLine("UserDSLaunchMessage:");
        }

        public static void PrintUserDSProcessEndMessage(UserDSProcessEndMessage msg, object tag)
        {
            PrintLine("UserDSProcessEndMessage:");
        }

        public static void PrintUserCareStateUpdateMessage(UserCareStateUpdateMessage msg, object tag)
        {
            PrintLine("UserCareStateUpdateMessage:");
            PrintLine("\tUserCareType={0}", msg.UserCareType); //System.Int32 has a toString()
            PrintLine("\tUserCareNextState={0}", msg.UserCareNextState); //System.Int32 has a toString()
        }

        public static void PrintUserPunishNotifyMessage(UserPunishNotifyMessage msg, object tag)
        {
            PrintLine("UserPunishNotifyMessage:");
            PrintLine("\tType={0}", msg.Type); //System.Byte has a toString()
            PrintLine("\tMessage={0}", msg.Message); //System.String has a toString()
            PrintLine("\tRemainSeconds={0}", msg.RemainSeconds); //System.Int64 has a toString()
        }

        public static void PrintUserPunishReloadMessage(UserPunishReloadMessage msg, object tag)
        {
            PrintLine("UserPunishReloadMessage:");
        }

        public static void PrintUseTiticoreItemMessage(UseTiticoreItemMessage msg, object tag)
        {
            PrintLine("UseTiticoreItemMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tTargetItemClass={0}", msg.TargetItemClass); //System.String has a toString()
        }

        public static void PrintTownEffectMessage(TownEffectMessage msg, object tag)
        {
            PrintLine("TownEffectMessage:");
            PrintLine("\tEffectID={0}", msg.EffectID); //System.Int32 has a toString()
            PrintLine("\tDuration={0}", msg.Duration); //System.Int32 has a toString()
        }

        public static void PrintCouponShopItemInfoQueryMessage(CouponShopItemInfoQueryMessage msg, object tag)
        {
            PrintLine("CouponShopItemInfoQueryMessage:");
            PrintLine("\tShopVersion={0}", msg.ShopVersion); //System.Int16 has a toString()
        }

        public static void PrintMoveSharedItemMessage(MoveSharedItemMessage msg, object tag)
        {
            PrintLine("MoveSharedItemMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tStorage={0}", msg.Storage); //System.Byte has a toString()
            PrintLine("\tTarget={0}", msg.Target); //System.Int32 has a toString()
        }

        public static void PrintQueryRestoreItemListMessage(QueryRestoreItemListMessage msg, object tag)
        {
            PrintLine("QueryRestoreItemListMessage:");
        }

        public static void PrintQueryCIDByNameMessage(QueryCIDByNameMessage msg, object tag)
        {
            PrintLine("QueryCIDByNameMessage:");
            PrintLine("\tRequestName={0}", msg.RequestName); //System.String has a toString()
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
        }

        public static void PrintQueryTiticoreDisplayItemsMessage(QueryTiticoreDisplayItemsMessage msg, object tag)
        {
            PrintLine("QueryTiticoreDisplayItemsMessage:");
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            PrintLine("\tTargetItemClass={0}", msg.TargetItemClass); //System.String has a toString()
        }

        public static void PrintPvpQueryChannelListMessage(PvpQueryChannelListMessage msg, object tag)
        {
            PrintLine("PvpQueryChannelListMessage:");
            PrintLine("\tPvpMode={0}", msg.PvpMode); //System.String has a toString()
            PrintLine("\tKey={0}", msg.Key); //System.String has a toString()
        }

        public static void PrintUseAdvancedFeatherMessage(UseAdvancedFeatherMessage msg, object tag)
        {
            PrintLine("UseAdvancedFeatherMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tTargetName={0}", msg.TargetName); //System.String has a toString()
        }


        public static void PrintAskFinishQuestMessage(AskFinishQuestMessage msg, object tag)
        {
            PrintLine("AskFinishQuestMessage:");
            PrintLine("\tShowPendingDialog={0}", msg.ShowPendingDialog); //System.Boolean has a toString()
            PrintLine("\tMessage={0}", msg.Message); //System.String has a toString()
            PrintLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
        }

        public static void PrintDSHostConnectionQuery(DSHostConnectionQuery msg, object tag)
        {
            PrintLine("DSHostConnectionQuery:");
        }

        public static void PrintQueryFeverPointMessage(QueryFeverPointMessage msg, object tag)
        {
            PrintLine("QueryFeverPointMessage:");
        }

        public static void PrintQueryMileagePointMessage(QueryMileagePointMessage msg, object tag)
        {
            PrintLine("QueryMileagePointMessage:");
        }

        public static void PrintQuestTimerInfoMessage(QuestTimerInfoMessage msg, object tag)
        {
            PrintLine("QuestTimerInfoMessage:");
            PrintLine("\tQuestTime={0}", msg.QuestTime); //System.Int32 has a toString()
            PrintLine("\tIsTimerDecreasing={0}", msg.IsTimerDecreasing); //System.Boolean has a toString()
        }

        public static void PrintOpenInGameCashShopUIMessage(OpenInGameCashShopUIMessage msg, object tag)
        {
            PrintLine("OpenInGameCashShopUIMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
        }

        public static void PrintOpenRandomBoxMessage(OpenRandomBoxMessage msg, object tag)
        {
            PrintLine("OpenRandomBoxMessage:");
            PrintLine("\tGroupID={0}", msg.GroupID); //System.Int32 has a toString()
            PrintLine("\tRandomBoxName={0}", msg.RandomBoxName); //System.String has a toString()
        }

        public static void PrintOpenTiticoreUIMessage(OpenTiticoreUIMessage msg, object tag)
        {
            PrintLine("OpenTiticoreUIMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tTargetItemClass={0}", msg.TargetItemClass); //System.String has a toString()
        }

        public static void PrintOpenTreasureBoxMessage(OpenTreasureBoxMessage msg, object tag)
        {
            PrintLine("OpenTreasureBoxMessage:");
            PrintLine("\tGroupID={0}", msg.GroupID); //System.Int32 has a toString()
            PrintLine("\tTreasureBoxName={0}", msg.TreasureBoxName); //System.String has a toString()
        }

        public static void PrintPvpConfirmJoinMessage(PvpConfirmJoinMessage msg, object tag)
        {
            PrintLine("PvpConfirmJoinMessage:");
            PrintLine("\tPvpMode={0}", msg.PvpMode); //System.String has a toString()
        }

        public static void PrintQueryCharacterNameByCIDListMessage(QueryCharacterNameByCIDListMessage msg, object tag)
        {
            PrintLine("QueryCharacterNameByCIDListMessage:");
            PrintLine("\tCIDList=[{0}]",String.Join(",",msg.CIDList)); //System.Collections.Generic.List`1[System.Int64]
        }

        public static void PrintQueryCharacterNameByCIDListResultMessage(QueryCharacterNameByCIDListResultMessage msg, object tag)
        {
            PrintLine("QueryCharacterNameByCIDListResultMessage:");
            PrintLine("\tNameList=[{0}]",String.Join(",",msg.NameList)); //System.Collections.Generic.List`1[System.String]
        }

        public static void PrintIncreasePvpRankMessage(IncreasePvpRankMessage msg, object tag)
        {
            PrintLine("IncreasePvpRankMessage:");
            PrintLine("\tRankID={0}", msg.RankID); //System.String has a toString()
            PrintLine("\tRankScore={0}", msg.RankScore); //System.Int32 has a toString()
            PrintLine("\tTestGuildID={0}", msg.TestGuildID); //System.Int32 has a toString()
            PrintLine("\tTestGuildName={0}", msg.TestGuildName); //System.String has a toString()
        }

        public static void PrintQueryRandomRankInfoMessage(QueryRandomRankInfoMessage msg, object tag)
        {
            PrintLine("QueryRandomRankInfoMessage:");
        }

        public static void PrintRequestBingoRewardMessage(RequestBingoRewardMessage msg, object tag)
        {
            PrintLine("RequestBingoRewardMessage:");
        }

        public static void PrintCheckCharacterNameResultMessage(CheckCharacterNameResultMessage msg, object tag)
        {
            PrintLine("CheckCharacterNameResultMessage:");
            PrintLine("\tValid={0}", msg.Valid); //System.Boolean has a toString()
            PrintLine("\tName={0}", msg.Name); //System.String has a toString()
            PrintLine("\tErrorMsg={0}", msg.ErrorMsg); //System.String has a toString()
        }

        public static void PrintRestoreItemInfo(RestoreItemInfo msg, object tag)
        {
            PrintLine("RestoreItemInfo:");
            PrintLine("\tSlot={0}", msg.Slot); //ServiceCore.ItemServiceOperations.SlotInfo has a toString()
            PrintLine("\tPriceType={0}", msg.PriceType); //System.String has a toString()
            PrintLine("\tPriceValue={0}", msg.PriceValue); //System.Int32 has a toString()
        }

        public static void PrintRestoreItemListMessage(RestoreItemListMessage msg, object tag)
        {
            PrintLine("RestoreItemListMessage:");
            foreach (RestoreItemInfo item in msg.RestoreItemList) {
                PrintLine("\tSlot={0} Price={1}{2}",item.Slot,item.PriceValue,item.PriceType);
            }
        }

        public static void PrintRequestRouletteBoardMessage(RequestRouletteBoardMessage msg, object tag)
        {
            PrintLine("RequestRouletteBoardMessage:");
            PrintLine("\tType={0}", msg.Type); //System.Int32 has a toString()
        }

        public static void PrintRequestRoulettePickSlotMessage(RequestRoulettePickSlotMessage msg, object tag)
        {
            PrintLine("RequestRoulettePickSlotMessage:");
        }

        public static void PrintSetFreeTitleNameMessage(SetFreeTitleNameMessage msg, object tag)
        {
            PrintLine("SetFreeTitleNameMessage:");
            PrintLine("\tFreeTitleName={0}", msg.FreeTitleName); //System.String has a toString()
        }

        public static void PrintPickSharedItemMessage(PickSharedItemMessage msg, object tag)
        {
            PrintLine("PickSharedItemMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tAmount={0}", msg.Amount); //System.Int32 has a toString()
            PrintLine("\tTargetTab={0}", msg.TargetTab); //System.Byte has a toString()
            PrintLine("\tTargetSlot={0}", msg.TargetSlot); //System.Int32 has a toString()
        }

        public static void PrintCouponShopItemBuyMessage(CouponShopItemBuyMessage msg, object tag)
        {
            PrintLine("CouponShopItemBuyMessage:");
            PrintLine("\tShopID={0}", msg.ShopID); //System.Int16 has a toString()
            PrintLine("\tOrder={0}", msg.Order); //System.Int16 has a toString()
            PrintLine("\tCount={0}", msg.Count); //System.Int32 has a toString()
        }

        public static void PrintVocationResetMessage(VocationResetMessage msg, object tag)
        {
            PrintLine("VocationResetMessage:");
            PrintLine("\tVocationClass={0}", msg.VocationClass); //System.Int32 has a toString()
        }

        public static void PrintShowCharacterNameChangeDialogMessage(ShowCharacterNameChangeDialogMessage msg, object tag)
        {
            PrintLine("ShowCharacterNameChangeDialogMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tName={0}", msg.Name); //System.String has a toString()
        }

        public static void PrintShowTreasureBoxMessage(ShowTreasureBoxMessage msg, object tag)
        {
            PrintLine("ShowTreasureBoxMessage:");
            PrintLine("\tGroupID={0}", msg.GroupID); //System.Int32 has a toString()
            PrintLine("\tTreasureBoxName={0}", msg.TreasureBoxName); //System.String has a toString()
        }

        public static void PrintSkillEnhanceSessionStartMessage(SkillEnhanceSessionStartMessage msg, object tag)
        {
            PrintLine("SkillEnhanceSessionStartMessage:");
            PrintLine("\tSkillName={0}", msg.SkillName); //System.String has a toString()
            PrintLine("\tSkillEnhanceStoneItemID={0}", msg.SkillEnhanceStoneItemID); //System.Int64 has a toString()
        }

        public static void PrintSkillEnhanceSessionEndMessage(SkillEnhanceSessionEndMessage msg, object tag)
        {
            PrintLine("SkillEnhanceSessionEndMessage:");
            PrintLine("\tSkillName={0}", msg.SkillName); //System.String has a toString()
        }

        public static void PrintSkillEnhanceUseErgMessage(SkillEnhanceUseErgMessage msg, object tag)
        {
            PrintLine("SkillEnhanceUseErgMessage:");
            PrintLine("\tErgItemID={0}", msg.ErgItemID); //System.Int64 has a toString()
            PrintLine("\tUseCount={0}", msg.UseCount); //System.Int32 has a toString()
        }

        public static void PrintSkillEnhanceUseErgResultMessage(SkillEnhanceUseErgResultMessage msg, object tag)
        {
            PrintLine("SkillEnhanceUseErgResultMessage:");
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
            PrintLine("\tCurrentPercentage={0}", msg.CurrentPercentage); //System.Int32 has a toString()
            PrintLine("\tMaxPercentage={0}", msg.MaxPercentage); //System.Int32 has a toString()
        }

        public static void PrintSkillEnhanceRestoreDurabilityMessage(SkillEnhanceRestoreDurabilityMessage msg, object tag)
        {
            PrintLine("SkillEnhanceRestoreDurabilityMessage:");
            PrintLine("\tEnhanceStoneItemID={0}", msg.EnhanceStoneItemID); //System.Int64 has a toString()
            PrintLine("\tUseCount={0}", msg.UseCount); //System.Int32 has a toString()
            PrintLine("\tSkillName={0}", msg.SkillName); //System.String has a toString()
        }

        public static void PrintSpiritInjectionItemMessage(SpiritInjectionItemMessage msg, object tag)
        {
            PrintLine("SpiritInjectionItemMessage:");
            PrintLine("\tSpiritStoneID={0}", msg.SpiritStoneID); //System.Int64 has a toString()
            PrintLine("\tTargetItemID={0}", msg.TargetItemID); //System.Int64 has a toString()
        }

        public static void PrintSpiritInjectionItemResultMessage(SpiritInjectionItemResultMessage msg, object tag)
        {
            PrintLine("SpiritInjectionItemResultMessage:");
            PrintLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.SpiritInjectionItemResult
            PrintLine("\tStatName={0}", msg.StatName); //System.String has a toString()
            PrintLine("\tValue={0}", msg.Value); //System.Int32 has a toString()
        }

        public static void PrintVocationResetResultMessage(VocationResetResultMessage msg, object tag)
        {
            PrintLine("VocationResetResultMessage:");
            PrintLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
        }

        public static void PrintRequestWebVerificationMessage(RequestWebVerificationMessage msg, object tag)
        {
            PrintLine("RequestWebVerificationMessage:");
            PrintLine("\tSessionNo={0}", msg.SessionNo); //System.Int32 has a toString()
        }

        public static void PrintWebVerificationMessage(WebVerificationMessage msg, object tag)
        {
            PrintLine("WebVerificationMessage:");
            PrintLine("\tSessionNo={0}", msg.SessionNo); //System.Int32 has a toString()
            PrintLine("\tIsSuccessfullyGenerated={0}", msg.IsSuccessfullyGenerated); //System.Boolean has a toString()
            PrintLine("\tPasscode={0}", msg.Passcode); //System.Int64 has a toString()
        }

        public static void PrintWishItemInfo(WishItemInfo msg, object tag)
        {
            PrintLine("WishItemInfo:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
            PrintLine("\tProductName={0}", msg.ProductName); //System.String has a toString()
        }

        public static void PrintWishListInsertResponseMessage(WishListInsertResponseMessage msg, object tag)
        {
            PrintLine("WishListInsertResponseMessage:");
            PrintLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.WishListResult
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
        }

        public static void Print_CheatJoinPartyByUserID(_CheatJoinPartyByUserID msg, object tag)
        {
            PrintLine("_CheatJoinPartyByUserID:");
            PrintLine("\tUserID={0}", msg.UserID); //System.String has a toString()
        }

        public static void PrintWishListSelectMessage(WishListSelectMessage msg, object tag)
        {
            PrintLine("WishListSelectMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
        }

        public static void PrintXignCodeReadyMessage(XignCodeReadyMessage msg, object tag)
        {
            PrintLine("XignCodeReadyMessage:");
            PrintLine("\tUserID={0}", msg.UserID); //System.String has a toString()
        }

        public static void PrintUseExtraSharedStorageMessage(UseExtraSharedStorageMessage msg, object tag)
        {
            PrintLine("UseExtraSharedStorageMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tStorageID={0}", msg.StorageID); //System.Byte has a toString()
        }

        public static void PrintAddStatusEffect(ServiceCore.EndPointNetwork.AddStatusEffect msg, object tag)
        {
            PrintLine("AddStatusEffect:");
            PrintLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            PrintLine("\tType={0}", msg.Type); //System.String has a toString()
            PrintLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            PrintLine("\tTimeSec={0}", msg.TimeSec); //System.Int32 has a toString()
        }

        public static void PrintArmorBrokenMessage(ArmorBrokenMessage msg, object tag)
        {
            PrintLine("ArmorBrokenMessage:");
            PrintLine("\tOwner={0}", msg.Owner); //System.Int32 has a toString()
            PrintLine("\tPart={0}", msg.Part); //System.Int32 has a toString()
        }

        public static void PrintCafeLoginMessage(CafeLoginMessage msg, object tag)
        {
            PrintLine("CafeLoginMessage:");
        }

        public static void PrintCashShopGiftSenderMessage(CashShopGiftSenderMessage msg, object tag)
        {
            PrintLine("CashShopGiftSenderMessage:");
            PrintLine("\tOrderNo={0}", msg.OrderNo); //System.Int32 has a toString()
            PrintLine("\tSenderName={0}", msg.SenderName); //System.String has a toString()
        }

        public static void PrintCashShopBalanceMessage(CashShopBalanceMessage msg, object tag)
        {
            PrintLine("CashShopBalanceMessage:");
            PrintLine("\tBalance={0}", msg.Balance); //System.Int32 has a toString()
            PrintLine("\tRefundless={0}", msg.Refundless); //System.Int32 has a toString()
        }

        public static void PrintQueryCashShopInventoryMessage(QueryCashShopInventoryMessage msg, object tag)
        {
            PrintLine("QueryCashShopInventoryMessage:");
        }

        public static void PrintDirectPurchaseCashShopItemResultMessage(DirectPurchaseCashShopItemResultMessage msg, object tag)
        {
            PrintLine("DirectPurchaseCashShopItemResultMessage:");
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
            PrintLine("\tReason={0}", msg.Reason); //ServiceCore.EndPointNetwork.DirectPurchaseCashShopItemResultMessage+DirectPurchaseItemFailReason
        }

        public static void PrintCashShopProductListMessage(CashShopProductListMessage msg, object tag)
        {
            PrintLine("CashShopProductListMessage:");
        }

        public static void PrintCashShopCategoryListMessage(CashShopCategoryListMessage msg, object tag)
        {
            PrintLine("CashShopCategoryListMessage:");
        }

        public static void PrintQueryBeautyShopInfoMessage(QueryBeautyShopInfoMessage msg, object tag)
        {
            PrintLine("QueryBeautyShopInfoMessage:");
            PrintLine("\tcharacterType={0}", msg.characterType); //System.Int32 has a toString()
        }

        public static void PrintDirectPurchaseTiticoreItemMessage(DirectPurchaseTiticoreItemMessage msg, object tag)
        {
            PrintLine("DirectPurchaseTiticoreItemMessage:");
            PrintLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
            PrintLine("\tIsCredit={0}", msg.IsCredit); //System.Boolean has a toString()
        }

        public static void PrintDirectPurchaseTiticoreItemResultMessage(DirectPurchaseTiticoreItemResultMessage msg, object tag)
        {
            PrintLine("DirectPurchaseTiticoreItemResultMessage:");
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
            PrintLine("\tReason={0}", msg.Reason); //ServiceCore.EndPointNetwork.DirectPurchaseCashShopItemResultMessage+DirectPurchaseItemFailReason
            PrintLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
        }

        public static void PrintQueryCashShopRefundMessage(QueryCashShopRefundMessage msg, object tag)
        {
            PrintLine("QueryCashShopRefundMessage:");
            PrintLine("\tOrderNo={0}", msg.OrderNo); //System.Int32 has a toString()
            PrintLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
        }

        public static void PrintResetSkillResultMessage(ResetSkillResultMessage msg, object tag)
        {
            PrintLine("ResetSkillResultMessage:");
            PrintLine("\tResetResult={0}", msg.ResetResult); //System.Int32 has a toString()
            PrintLine("\tSkillID={0}", msg.SkillID); //System.String has a toString()
            PrintLine("\tSkillRank={0}", msg.SkillRank); //System.Int32 has a toString()
            PrintLine("\tReturnAP={0}", msg.ReturnAP); //System.Int32 has a toString()
        }

        public static void PrintRequestSecuredOperationMessage(RequestSecuredOperationMessage msg, object tag)
        {
            PrintLine("RequestSecuredOperationMessage:");
            PrintLine("\tOperation={0}", msg.Operation); //ServiceCore.EndPointNetwork.SecuredOperationType
        }

        public static void PrintTradeItemInfo(TradeItemInfo msg, object tag)
        {
            PrintLine("TradeItemInfo:");
            PrintLine("\tTID={0}", msg.TID); //System.Int64 has a toString()
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tChracterName={0}", msg.ChracterName); //System.String has a toString()
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            PrintLine("\tItemCount={0}", msg.ItemCount); //System.Int64 has a toString()
            PrintLine("\tItemPrice={0}", msg.ItemPrice); //System.Int64 has a toString()
            PrintLine("\tCloseDate={0}", msg.CloseDate); //System.Int32 has a toString()
            PrintLine("\tHasAttribute={0}", msg.HasAttribute); //System.Boolean has a toString()
            PrintLine("\tAttributeEX={0}", msg.AttributeEX); //System.String has a toString()
            PrintLine("\tMaxArmorCondition={0}", msg.MaxArmorCondition); //System.Int32 has a toString()
            PrintLine("\tcolor1={0}", msg.color1); //System.Int32 has a toString()
            PrintLine("\tcolor2={0}", msg.color2); //System.Int32 has a toString()
            PrintLine("\tcolor3={0}", msg.color3); //System.Int32 has a toString()
            PrintLine("\tMinPrice={0}", msg.MinPrice); //System.Int32 has a toString()
            PrintLine("\tMaxPrice={0}", msg.MaxPrice); //System.Int32 has a toString()
            PrintLine("\tAvgPrice={0}", msg.AvgPrice); //System.Int32 has a toString()
            PrintLine("\tTradeType={0}", msg.TradeType); //System.Byte has a toString()
        }

        public static void PrintPetChangeNameResultMessage(PetChangeNameResultMessage msg, object tag)
        {
            PrintLine("PetChangeNameResultMessage:");
            PrintLine("\tIsSuccess={0}", msg.IsSuccess); //System.Boolean has a toString()
            PrintLine("\tPetName={0}", msg.PetName); //System.String has a toString()
            PrintLine("\tResultType={0}", msg.ResultType); //System.String has a toString()
        }

        public static void PrintPetOperationMessage(PetOperationMessage msg, object tag)
        {
            PrintLine("PetOperationMessage:");
            PrintLine("\tOperationCode={0}", msg.OperationCode); //ServiceCore.EndPointNetwork.PetOperationType
            PrintLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
            PrintLine("\tArg={0}", msg.Arg); //System.String has a toString()
            PrintLine("\tValue1={0}", msg.Value1); //System.Int32 has a toString()
            PrintLine("\tValue2={0}", msg.Value2); //System.Int32 has a toString()
            PrintLine("\tValue3={0}", msg.Value3); //System.Int32 has a toString()
        }

        public static void PrintTransferPartyMasterMessage(TransferPartyMasterMessage msg, object tag)
        {
            PrintLine("TransferPartyMasterMessage:");
            PrintLine("\tNewMasterName={0}", msg.NewMasterName); //System.String has a toString()
        }

        public static void PrintOpenGuildMessage(OpenGuildMessage msg, object tag)
        {
            PrintLine("OpenGuildMessage:");
            PrintLine("\tGuildName={0}", msg.GuildName); //System.String has a toString()
            PrintLine("\tGuildNameID={0}", msg.GuildNameID); //System.String has a toString()
            PrintLine("\tGuildIntro={0}", msg.GuildIntro); //System.String has a toString()
        }

        public static void PrintQueryGuildListMessage(QueryGuildListMessage msg, object tag)
        {
            PrintLine("QueryGuildListMessage:");
            PrintLine("\tQueryType={0}", msg.QueryType); //System.Int32 has a toString()
            PrintLine("\tSearchKey={0}", msg.SearchKey); //System.String has a toString()
            PrintLine("\tPage={0}", msg.Page); //System.Int32 has a toString()
            PrintLine("\tPageSize={0}", msg.PageSize); //System.Byte has a toString()
        }

        public static void PrintRandomMissionMessage(RandomMissionMessage msg, object tag)
        {
            PrintLine("RandomMissionMessage:");
            PrintLine("\tMissionCommand={0}", msg.MissionCommand); //System.Int32 has a toString()
            PrintLine("\tID={0}", msg.ID); //System.Int64 has a toString()
            PrintLine("\tArgs={0}", msg.Args); //System.String has a toString()
            PrintLine("\tArgs2={0}", msg.Args2); //System.Int64 has a toString()
            PrintLine("\tMID={0}", msg.MID); //System.String has a toString()
        }

        public static void PrintEnchantItemResultMessage(EnchantItemResultMessage msg, object tag)
        {
            PrintLine("EnchantItemResultMessage:");
            PrintLine("\tCurrentValue={0}", msg.CurrentValue); //System.Int32 has a toString()
            PrintLine("\tGoalValue={0}", msg.GoalValue); //System.Int32 has a toString()
            PrintLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
            PrintLine("\tRolledDice={0}", msg.RolledDice); //System.String has a toString()
            PrintLine("\tCurrentSuccessRatio={0}", msg.CurrentSuccessRatio); //System.Int32 has a toString()
        }

        public static void PrintEnhanceItemMessage(EnhanceItemMessage msg, object tag)
        {
            PrintLine("EnhanceItemMessage:");
            PrintLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            PrintLine("\tMaterial1={0}", msg.Material1); //System.String has a toString()
            PrintLine("\tMaterial2={0}", msg.Material2); //System.String has a toString()
            PrintLine("\tAdditionalMaterial={0}", msg.AdditionalMaterial); //System.String has a toString()
            PrintLine("\tIsEventEnhanceAShot={0}", msg.IsEventEnhanceAShot); //System.Boolean has a toString()
        }

        public static void PrintItemFailMessage(ItemFailMessage msg, object tag)
        {
            PrintLine("ItemFailMessage:");
            PrintLine("\tMessage={0}", msg.Message); //ServiceCore.EndPointNetwork.HeroesString has a toString()
        }

        public static void PrintPvpKickedMessage(PvpKickedMessage msg, object tag)
        {
            PrintLine("PvpKickedMessage:");
        }

        public static void PrintPvpRegisterResultMessage(PvpRegisterResultMessage msg, object tag)
        {
            PrintLine("PvpRegisterResultMessage:");
            PrintLine("\tPvpMode={0}", msg.PvpMode); //System.String has a toString()
            PrintLine("\tUsePendingDialog={0}", msg.UsePendingDialog); //System.Boolean has a toString()
            PrintLine("\tStartWaitingTimer={0}", msg.StartWaitingTimer); //System.Boolean has a toString()
            PrintLine("\tState={0}", msg.State); //System.Int32 has a toString()
            PrintLine("\tMessage={0}", msg.Message); //ServiceCore.EndPointNetwork.HeroesString has a toString()
        }

        public static void PrintPvpReportMessage(PvpReportMessage msg, object tag)
        {
            PrintLine("PvpReportMessage:");
            PrintLine("\tEventInt={0}", msg.EventInt); //System.Int32 has a toString()
            PrintLine("\tSubject={0}", msg.Subject); //System.Int32 has a toString()
            PrintLine("\tObject={0}", msg.Object); //System.Int32 has a toString()
            PrintLine("\tArg={0}", msg.Arg); //System.String has a toString()
            PrintLine("\tEvent={0}", msg.Event); //ServiceCore.EndPointNetwork.PvpReportType
        }

        public static void PrintQueryRankInfoMessage(QueryRankInfoMessage msg, object tag)
        {
            PrintLine("QueryRankInfoMessage:");
            PrintLine("\tRankID={0}", msg.RankID); //System.String has a toString()
            PrintLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintQueryRankOtherCharacterInfoMessage(QueryRankOtherCharacterInfoMessage msg, object tag)
        {
            PrintLine("QueryRankOtherCharacterInfoMessage:");
            PrintLine("\tRequesterCID={0}", msg.RequesterCID); //System.Int64 has a toString()
            PrintLine("\tCharacterName={0}", msg.CharacterName); //System.String has a toString()
            PrintLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintRemoveMissionSuccessMessage(RemoveMissionSuccessMessage msg, object tag)
        {
            PrintLine("RemoveMissionSuccessMessage:");
            PrintLine("\tID={0}", msg.ID); //System.Int64 has a toString()
        }

        public static void PrintRemoveServerStatusEffectMessage(RemoveServerStatusEffectMessage msg, object tag)
        {
            PrintLine("RemoveServerStatusEffectMessage:");
            PrintLine("\tType={0}", msg.Type); //System.String has a toString()
        }

        public static void PrintRequestSortInventoryMessage(RequestSortInventoryMessage msg, object tag)
        {
            PrintLine("RequestSortInventoryMessage:");
            PrintLine("\tStorageNo={0}", msg.StorageNo); //System.Int32 has a toString()
        }

        public static void PrintDyeItemResultMessage(DyeItemResultMessage msg, object tag)
        {
            PrintLine("DyeItemResultMessage:");
            PrintLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
            PrintLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            PrintLine("\tColor1={0}", msg.Color1); //System.Int32 has a toString()
            PrintLine("\tColor2={0}", msg.Color2); //System.Int32 has a toString()
            PrintLine("\tColor3={0}", msg.Color3); //System.Int32 has a toString()
            PrintLine("\tTriesLeft={0}", msg.TriesLeft); //System.Int32 has a toString()
        }

        public static void PrintHotSpringAddPotionResultMessage(HotSpringAddPotionResultMessage msg, object tag)
        {
            PrintLine("HotSpringAddPotionResultMessage:");
            PrintLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.HotSpringAddPotionResult
            PrintLine("\tTownID={0}", msg.TownID); //System.Int32 has a toString()
            PrintLine("\tPrevPotionItemClass={0}", msg.PrevPotionItemClass); //System.String has a toString()
            HotSpringPotionEffectInfo h = msg.hotSpringPotionEffectInfos;
            PrintLine("\tPotionItemClass={0}",h.PotionItemClass);
            PrintLine("\tCharacterName={0}",h.CharacterName);
            PrintLine("\tGuildName={0}",h.GuildName);
            PrintLine("\tExpiredTime={0}",h.ExpiredTime);
            PrintLine("\tOtherPotionUsableTime={0}",h.OtherPotionUsableTime);
        }

        public static void PrintWishListInsertMessage(WishListInsertMessage msg, object tag)
        {
            PrintLine("WishListInsertMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tList_Wish={0}",String.Join(",",msg.List_Wish)); //System.Collections.Generic.ICollection`1[System.Int32]
        }

        public static void PrintSpawnMonsterMessage(SpawnMonsterMessage msg, object tag)
        {
            PrintLine("SpawnMonsterMessage:");
            foreach (SpawnedMonster m in msg.MonsterList) {
                PrintLine("\tSpawnedMonster:");
                PrintLine("\t\tEntityID={0}",m.EntityID);
                PrintLine("\t\tPoint={0}",m.Point);
                PrintLine("\t\tModel={0}",m.Model);
                PrintLine("\t\tVarianceBefore={0}",m.VarianceBefore);
                PrintLine("\t\tVarianceAfter={0}",m.VarianceAfter);
                PrintLine("\t\tSpeed={0}",m.Speed);
                PrintLine("\t\tMonsterType={0}",m.MonsterType);
                PrintLine("\t\tAIType={0}",m.AIType);
                PrintLine("\t\tMonsterVariance={0}",m.MonsterVariance);
            }
        }

        public static void PrintDropGoldCoreMessage(DropGoldCoreMessage msg, object tag)
        {
            PrintLine("DropGoldCoreMessage:");
            PrintLine("\tMonsterEntityName={0}", msg.MonsterEntityName); //System.String has a toString()
            PrintLine("\tDropImmediately={0}", msg.DropImmediately); //System.Int32 has a toString()
            PrintLine("\tEvilCores:");
            foreach (EvilCoreInfo e in msg.EvilCores) {
                PrintLine("\t\tEvilCore:");
                PrintLine("\t\t\tEvilCoreEntityName={0}",e.EvilCoreEntityName);
                PrintLine("\t\t\tEvilCoreType={0}",e.EvilCoreType);
                PrintLine("\t\t\tWinner={0}",e.Winner);
                PrintLine("\t\t\tAdditionalRareCoreTagList=[{0}]",String.Join(",",e.AdditionalRareCoreTagList));
            }
        }

        

        public static void PrintGuildListMessage(GuildListMessage msg, object tag)
        {
            //TODO: db connect
            PrintLine("GuildListMessage:");
            PrintLine("\tPage={0}", msg.Page); //System.Int32 has a toString()
            PrintLine("\tTotalPage={0}", msg.TotalPage); //System.Int32 has a toString()
            PrintLine("\tGuildList:");
            foreach (InGameGuildInfo g in msg.GuildList) {
                PrintLine("\t\tInGameGuildInfo:");
                PrintLine("\t\t\tGuildSN={0}", g.GuildSN); //System.Int32 has a toString()
                PrintLine("\t\t\tGuildName={0}",g.GuildName); //System.String has a toString()
                PrintLine("\t\t\tGuildLevel={0}", g.GuildLevel); //System.Int32 has a toString()
                PrintLine("\t\t\tMemberCount={0}", g.MemberCount); //System.Int32 has a toString()
                PrintLine("\t\t\tMasterName={0}", g.MasterName); //System.String has a toString()
                PrintLine("\t\t\tMaxMemberCount={0}", g.MaxMemberCount); //System.Int32 has a toString()
                PrintLine("\t\t\tIsNewbieRecommend={0}", g.IsNewbieRecommend); //System.Boolean has a toString()
                PrintLine("\t\t\tGuildPoint={0}", g.GuildPoint); //System.Int64 has a toString()
                PrintLine("\t\t\tGuildNotice={0}", g.GuildNotice); //System.String has a toString()
                PrintLine("\t\t\tDailyGainGP={0}",g.DailyGainGP); //System.Collections.Generic.Dictionary`2[System.Byte,System.Int32]
            }

        }

        public static void PrintUpdateSharedInventoryInfoMessage(UpdateSharedInventoryInfoMessage msg, object tag)
        {
            PrintLine("UpdateSharedInventoryInfoMessage:");
            ICollection<SlotInfo> slotInfos = GetPrivateProperty<ICollection<SlotInfo>>(msg, "slotInfos");
            foreach (SlotInfo s in slotInfos) {
                PrintLine(SlotInfoToString(s,"SlotInfo",1));
            }
        }

        public static void PrintWishListDeleteMessage(WishListDeleteMessage msg, object tag)
        {
            PrintLine("WishListDeleteMessage:");
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tProductNo=[{0}]",String.Join(",",msg.ProductNo)); //System.Collections.Generic.IList`1[System.Int32]
        }

        public static void PrintStartTrialEventMessage(StartTrialEventMessage msg, object tag)
        {
            PrintLine("StartTrialEventMessage:");
            PrintLine("\tSectorGroupID={0}", msg.SectorGroupID); //System.Int32 has a toString()
            PrintLine("\tFactorName={0}", msg.FactorName); //System.String has a toString()
            PrintLine("\tTimeLimit={0}", msg.TimeLimit); //System.Int32 has a toString()
            PrintLine("\tActorsIndex=[{0}]",String.Join(",",msg.ActorsIndex)); //System.Collections.Generic.List`1[System.Int32]
        }

        public static void PrintSkillEnhanceMessage(SkillEnhanceMessage msg, object tag)
        {
            PrintLine("SkillEnhanceMessage:");
            PrintLine("\tAdditionalItemIDs=[{0}]",String.Join(",",msg.AdditionalItemIDs)); //System.Collections.Generic.List`1[System.Int64]
        }

        public static void PrintFrameRateMessage(FrameRateMessage msg, object tag)
        {
            PrintLine("FrameRateMessage:");
            PrintLine("\tFrameRate={0}", msg.FrameRate); //System.Int32 has a toString()
        }

        public static void PrintQueryCharacterViewInfoMessage(QueryCharacterViewInfoMessage msg, object tag)
        {
            PrintLine("QueryCharacterViewInfoMessage:");
            PrintLine("\tQueryID={0}", msg.QueryID); //System.Int64 has a toString()
            PrintLine("\tname={0}", msg.name); //System.String has a toString()
        }

        public static void PrintChangeMissionStatusMessage(ChangeMissionStatusMessage msg, object tag)
        {
            PrintLine("ChangeMissionStatusMessage:");
            PrintLine("\tMissionList:");
            foreach (MissionMessage m in msg.MissionList)
            {
                PrintLine(MissionMessageToString(m, "MissionMessage", 1));
            }
        }

        public static void PrintAssignMissionSuccessMessage(AssignMissionSuccessMessage msg, object tag)
        {
            PrintLine("AssignMissionSuccessMessage:");
            PrintLine("\tMissionList:");
            foreach (MissionMessage m in msg.MissionList)
            {
                PrintLine(MissionMessageToString(m, "MissionMessage", 1));
            }
        }

        public static void PrintPostingInfoMessage(PostingInfoMessage msg, object tag)
        {
            PrintLine("PostingInfoMessage:");
            PrintLine("\tRemainTimeToNextPostingTime={0}", msg.RemainTimeToNextPostingTime); //System.Int32 has a toString()
            PrintLine("\tMissionList:");
            foreach (MissionMessage m in msg.MissionList)
            {
                PrintLine(MissionMessageToString(m,"MissionMessage",1));
            }
        }

        public static void PrintTcProtectRequestMessage(TcProtectRequestMessage msg, object tag)
        {
            PrintLine("TcProtectRequestMessage:");
            PrintLine("\tMd5OfDll={0}",BitConverter.ToString(msg.Md5OfDll)); //System.Byte[]
            PrintLine("\tEncodedBlock={0}", BitConverter.ToString(msg.EncodedBlock)); //System.Byte[]
        }

        public static void PrintGuildOperationMessage(GuildOperationMessage msg, object tag)
        {
            PrintLine("GuildOperationMessage:");
            foreach (GuildOperationInfo g in msg.Operations) {
                PrintLine("\tGuildOperationInfo: Command={0} Target={1} Value={2}",g.Command,g.Target,g.Value);
            }
        }

        public static void PrintBeautyShopInfoMessage(BeautyShopInfoMessage msg, object tag)
        {
            PrintLine("BeautyShopInfoMessage:");
            ICollection<CashShopCategoryListElement> CategoryList = GetPrivateProperty<ICollection<CashShopCategoryListElement>>(msg, "CategoryList");
            PrintLine("\tCategoryList:");
            foreach (CashShopCategoryListElement e in CategoryList) {
                PrintLine("CashShopCategoryListElement: CategoryNo={0} CategoryName={1} ParentCategoryNo={2} DisplayNo={3}",e.CategoryNo,e.CategoryName,e.ParentCategoryNo,e.DisplayNo);
            }
            ICollection<CashShopProductListElement> ProductList = GetPrivateProperty<ICollection<CashShopProductListElement>>(msg, "ProductList");
            PrintLine("\tProductList:");
            foreach (CashShopProductListElement e in ProductList) {
                PrintLine("\t\tCashShopProductListElement:");
                PrintLine("\t\t\tProductNo={0}",e.ProductNo);
                PrintLine("\t\t\tProductExpire={0}",e.ProductExpire);
                PrintLine("\t\t\tProductPieces={0}",e.ProductPieces);
                PrintLine("\t\t\tProductID={0}",e.ProductID);
                PrintLine("\t\t\tProductGUID={0}",e.ProductGUID);
                PrintLine("\t\t\tPaymentType={0}",e.PaymentType);
                PrintLine("\t\t\tProductType={0}",e.ProductType);
                PrintLine("\t\t\tSalePrice={0}",e.SalePrice);
                PrintLine("\t\t\tCategoryNo={0}",e.CategoryNo);
                PrintLine("\t\t\tStatus={0}",e.Status);
            }
            PrintLine("\tCouponList:");
            ICollection<BeautyShopCouponListElement> CouponList = GetPrivateProperty <ICollection<BeautyShopCouponListElement>> (msg, "CouponList");
            foreach (BeautyShopCouponListElement e in CouponList) {
                PrintLine("BeautyShopCouponListElement: Category={0} ItemClass={1} Weight={2}",e.Category,e.ItemClass,e.Weight);
            }
        }

        public static string GuildMemberInfoToString(GuildMemberInfo g, string name, int numTabs) {
            StringBuilder sb = new StringBuilder();
            string t = numTabs != 0 ? new string('\t',numTabs) : "";
            sb.Append(t);
            sb.Append(name);
            sb.Append(":");
            t = "\n\t" + t;
            sb.Append(t);
            sb.Append("CharacterName=");
            sb.Append(g.CharacterName);
            sb.Append(t);
            sb.Append("GameLevel=");
            sb.Append(g.GameLevel);
            sb.Append(t);
            sb.Append("Rank=");
            sb.Append((GuildMemberRank)g.Rank);
            sb.Append(t);
            sb.Append("Point=");
            sb.Append(g.Point);
            sb.Append(t);
            sb.Append("LastLoginTime=");
            sb.Append(g.LastLoginTime);
            sb.Append(" Days ago");
            sb.Append(t);
            sb.Append("IsOnline=");
            sb.Append(g.IsOnline);
            return sb.ToString();
        }

        public static void PrintGuildMemberListMessage(GuildMemberListMessage msg, object tag)
        {
            PrintLine("GuildMemberListMessage:");
            PrintLine("\tIsFullUpdate={0}", msg.IsFullUpdate); //System.Boolean has a toString()
            PrintLine("\tMembers:");
            int i = 0;
            foreach (GuildMemberInfo g in msg.Members) {
                string name = String.Format("GuildMemberInfo[{0}]", i++);
                PrintLine(GuildMemberInfoToString(g, name, 2));
            }
        }

        public static void PrintUpdateStoryStatusMessage(UpdateStoryStatusMessage msg, object tag)
        {
            PrintLine("UpdateStoryStatusMessage:");
            PrintLine("\tIsFullInformation={0}", msg.IsFullInformation); //System.Boolean has a toString()
            PrintLine(DictToString<string,int>(msg.StoryStatus,"StoryStatus",1)); //System.Collections.Generic.IDictionary`2[System.String,System.Int32]
        }

        public static void PrintGuildStorageLogsMessage(GuildStorageLogsMessage msg, object tag)
        {
            PrintLine("GuildStorageLogsMessage:");
            PrintLine("\tIsTodayLog={0}", msg.IsTodayLog); //System.Boolean has a toString()
            foreach (GuildStorageBriefLogElement e in msg.BriefLogs) {
                PrintLine("\tGuildStorageBriefLogElement:");
                PrintLine("\t\tCharacterName={0}", e.CharacterName); //System.String has a toString()
                PrintLine("\t\tOperationType={0}", e.OperationType); //ServiceCore.EndPointNetwork.GuildService.GuildStorageOperationType
                PrintLine("\t\tAddCount={0}", e.AddCount); //System.Int32 has a toString()
                PrintLine("\t\tPickCount={0}", e.PickCount); //System.Int32 has a toString()
                PrintLine("\t\tDatestamp={0}", e.Datestamp); //System.Int32 has a toString()
                PrintLine("\t\tTimestamp={0}", e.Timestamp); //System.Int32 has a toString()
            }
            foreach (GuildStorageItemLogElement e in msg.ItemLogs) {
                PrintLine("\tGuildStorageItemLogElement:");
                PrintLine("\t\tCharacterName={0}", e.CharacterName); //System.String has a toString()
                PrintLine("\t\tIsAddItem={0}", e.IsAddItem); //System.Boolean has a toString()
                PrintLine("\t\tItemClass={0}", e.ItemClass); //System.String has a toString()
                PrintLine("\t\tCount={0}", e.Count); //System.Int32 has a toString()
                PrintLine("\t\tDatestamp={0}", e.Datestamp); //System.Int32 has a toString()
                PrintLine("\t\tTimestamp={0}", e.Timestamp); //System.Int32 has a toString()
                PrintLine("\t\tColor1={0}", e.Color1); //System.Int32 has a toString()
                PrintLine("\t\tColor2={0}", e.Color2); //System.Int32 has a toString()
                PrintLine("\t\tColor3={0}", e.Color3); //System.Int32 has a toString()
            }
        }

        public static void PrintCashShopInventoryMessage(CashShopInventoryMessage msg, object tag)
        {
            PrintLine("CashShopInventoryMessage:");
            foreach (CashShopInventoryElement e in msg.Inventory) {
                PrintLine("\tCashShopInventoryElement:");
                PrintLine("\t\tOrderNo={0}", e.OrderNo); //System.Int32 has a toString()
                PrintLine("\t\tProductNo={0}", e.ProductNo); //System.Int32 has a toString()
                PrintLine("\t\tItemClass={0}", e.ItemClass); //System.String has a toString()
                PrintLine("\t\tItemClassEx={0}", e.ItemClassEx); //System.String has a toString()
                PrintLine("\t\tCount={0}", e.Count); //System.Int16 has a toString()
                PrintLine("\t\tr={0}", e.r); //System.String has a toString()
                PrintLine("\t\tg={0}", e.g); //System.String has a toString()
                PrintLine("\t\tb={0}", e.b); //System.String has a toString()
                PrintLine("\t\tExpire={0}", e.Expire); //System.Int16 has a toString()
                PrintLine("\t\tIsGift={0}", e.IsGift); //System.Boolean has a toString()
                PrintLine("\t\tSenderMessage={0}", e.SenderMessage); //System.String has a toString()
                PrintLine("\t\tRemainQuantity={0}", e.RemainQuantity); //System.Int16 has a toString()
            }
        }

        public static void PrintPvpChannelListMessage(PvpChannelListMessage msg, object tag)
        {
            PrintLine("PvpChannelListMessage:");
            PrintLine("\tPvpMode={0}", msg.PvpMode); //System.String has a toString()
            PrintLine("\tKey={0}", msg.Key); //System.String has a toString()
            PrintLine("\tPvpChannelInfos:");
            foreach (PvpChannelInfo p in msg.PvpChannelInfos) {
                PrintLine("\tChannelID={0} Desc={1}",p.ChannelID,p.Desc);
            }
        }

        public static void PrintPvpInfoMessage(PvpInfoMessage msg, object tag)
        {
            PrintLine("PvpInfoMessage:");
            int i = 0;
            foreach (PvpResultInfo p in msg.PvpResultList) {
                PrintLine("\tPvpResultInfo[{0}]: PvpType={1} Win={2} Draw={3} Lose={4}",i++,p.PvpType,p.Win,p.Draw,p.Lose);
            }
        }

        public static void PrintCharacterViewInfoMessage(CharacterViewInfoMessage msg, object tag)
        {
            PrintLine("CharacterViewInfoMessage:");
            PrintLine("\tQueryID={0}", msg.QueryID); //System.Int64 has a toString()
            PrintLine("\tSummary={0}", msg.Summary); //ServiceCore.CharacterServiceOperations.CharacterSummary has a toString()

            PrintLine(DictToString<string,int>(msg.Stat,"Stat",1)); //System.Collections.Generic.IDictionary`2[System.String,System.Int32]
            PrintLine("\tQuickSlotInfo={0}", msg.QuickSlotInfo); //ServiceCore.EndPointNetwork.QuickSlotInfo has a toString()
            int i = 0;
            PrintLine("\tEquipment:");
            foreach (KeyValuePair<int,ColoredEquipment> entry in msg.Equipment)
            {
                ColoredEquipment e = entry.Value;
                PrintLine("\t\t{0}=ColoredEquipment: ItemClass={1} Color1={2} Color2={3} Color3={4}",i++ ,e.ItemClass, e.Color1, e.Color2, e.Color3);
            }

            PrintLine("\tSilverCoin={0}", msg.SilverCoin); //System.Int32 has a toString()
            PrintLine("\tPlatinumCoin={0}", msg.PlatinumCoin); //System.Int32 has a toString()
            PrintLine("\tDurability:");
            foreach (KeyValuePair<int, DurabilityEquipment> entry in msg.Durability) {
                DurabilityEquipment e = entry.Value;
                PrintLine("\t\t{0}=DurabilityEquipment: MaxDurabilityBonus={1} DiffDurability={2}",entry.Key,e.MaxDurabilityBonus,e.DiffDurability);
            }
        }

        public static void PrintSkillEnhanceUseDurabilityMessage(SkillEnhanceUseDurabilityMessage msg, object tag)
        {
            PrintLine("SkillEnhanceUseDurabilityMessage:");
            PrintLine("\tSlot={0}", msg.Slot); //System.Int32 has a toString()
            PrintLine(DictToString<string,int>(msg.UseDurability,"UseDurability",1)); //System.Collections.Generic.Dictionary`2[System.String,System.Int32]
        }

        public static void PrintWishListSelectResponseMessage(WishListSelectResponseMessage msg, object tag)
        {
            PrintLine("WishListSelectResponseMessage:");
            PrintLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.WishListResult
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tProductInfo:");
            foreach (WishItemInfo w in msg.ProductInfo) {
                PrintLine("\t\tWishItemInfo: CID={0} ProductNo={1} ProductName={2}",w.CID,w.ProductNo,w.ProductName);
            }
        }

        public static void PrintSkillEnhanceChangedMessage(SkillEnhanceChangedMessage msg, object tag)
        {
            PrintLine("SkillEnhanceChangedMessage:");
            foreach (KeyValuePair<string, BriefSkillEnhance> entry in msg.SkillEnhanceChanged) {
                BriefSkillEnhance e = entry.Value;
                PrintLine("\t{0}=BriefSkillEnhance: GroupKey={1} IndexKey={2} Type={3} ReduceDurability={4} MaxDurabilityBonus={5}",entry.Key,e.GroupKey,e.IndexKey,e.Type,e.ReduceDurability,e.MaxDurabilityBonus);
            }
        }

        public static void PrintSkillEnhanceResultMessage(SkillEnhanceResultMessage msg, object tag)
        {
            PrintLine("SkillEnhanceResultMessage:");
            PrintLine("\tSkillName={0}", msg.SkillName); //System.String has a toString()
            PrintLine("\tSkillEnhanceStoneItem={0}", msg.SkillEnhanceStoneItem); //System.String has a toString()
            PrintLine("\tSuccessRatio={0}", msg.SuccessRatio); //System.Int32 has a toString()
            PrintLine("\tAdditionalItemClasses=[{0}]",String.Join(",",msg.AdditionalItemClasses)); //System.Collections.Generic.List`1[System.String]
            PrintLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
            PrintLine("\tIsAdditionalItemDestroyed={0}", msg.IsAdditionalItemDestroyed); //System.Boolean has a toString()
            PrintLine("\tIsEnhanceStoneProtected={0}", msg.IsEnhanceStoneProtected); //System.Boolean has a toString()
            PrintLine("\tEnhances:");
            foreach (BriefSkillEnhance e in msg.Enhances) {
                PrintLine("\t\tBriefSkillEnhance: GroupKey={0} IndexKey={1} Type={2} ReduceDurability={3} MaxDurabilityBonus={3}", e.GroupKey, e.IndexKey, e.Type, e.ReduceDurability, e.MaxDurabilityBonus);
            }
        }

        public static void PrintRouletteBoardResultMessage(RouletteBoardResultMessage msg, object tag)
        {
            PrintLine("RouletteBoardResultMessage:");
            PrintLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.RouletteBoardResultMessage+RouletteBoardResult
            PrintLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            PrintLine("\tRemindsSeconds={0}", msg.RemindsSeconds); //System.Int32 has a toString()
            PrintLine("\tSlotInfos:");
            foreach (RouletteSlotInfo r in msg.SlotInfos) {
                string c1 = IntToRGB(r.Color1);
                string c2 = IntToRGB(r.Color2);
                string c3 = IntToRGB(r.Color3);
                PrintLine("\t\tRouletteSlotInfo: ItemClassEx={0} ItemCount={1} Color1={2} Color2={3} Color3={4} Grade={5}",r.ItemClassEx,r.ItemCount,c1,c2,c3,r.Grade);
            }
        }

        public static void PrintShopItemInfoResultMessage(ShopItemInfoResultMessage msg, object tag)
        {
            PrintLine("ShopItemInfoResultMessage:");
            PrintLine("\tShopID={0}", msg.ShopID); //System.String has a toString()
            PrintLine("\tRestrictionCountDic:");
            foreach (KeyValuePair<short, ShopTimeRestrictedResult> entry in msg.RestrictionCountDic) {
                PrintLine("\t\t{0}=ShopTimeRestrictedResult: BuyableCount={1} NextResetTicksDiff={2}",entry.Key,entry.Value.BuyableCount,entry.Value.NextResetTicksDiff);
            }
        }

        public static void PrintCouponShopItemInfoResultMessage(CouponShopItemInfoResultMessage msg, object tag)
        {
            PrintLine("CouponShopItemInfoResultMessage:");
            PrintLine("\tShopVersion={0}", msg.ShopVersion); //System.Int16 has a toString()
            PrintLine("\tRestrictionCountDic:");
            foreach (KeyValuePair<short, ShopTimeRestrictedResult> entry in msg.RestrictionCountDic)
            {
                PrintLine("\t\t{0}=ShopTimeRestrictedResult: BuyableCount={1} NextResetTicksDiff={2}", entry.Key, entry.Value.BuyableCount, entry.Value.NextResetTicksDiff);
            }
        }

        public static string ColoredItemListToString(ICollection<ColoredItem> items, string name, int numTabs) {
            string t = numTabs != 0 ? new string('\t', numTabs) : "";
            StringBuilder sb = new StringBuilder();
            sb.Append(t);
            sb.Append(name);
            sb.Append(":");
            t = "\n\t" + t;
            foreach (ColoredItem c in items)
            {
                sb.Append(t);
                sb.Append("ColoredItem: ItemClass=");
                sb.Append(c.ItemClass);
                sb.Append(" ItemNumber=");
                sb.Append(c.ItemNumber);
                sb.Append(" Color1=");
                sb.Append(IntToRGB(c.Color1));
                sb.Append(" Color2=");
                sb.Append(IntToRGB(c.Color2));
                sb.Append(" Color3=");
                sb.Append(IntToRGB(c.Color3));
            }
            return sb.ToString();
        }

        public static void PrintTiticoreResultMessage(TiticoreResultMessage msg, object tag)
        {
            ICollection<ColoredItem> ResultItemList = GetPrivateProperty<ICollection<ColoredItem>>(msg, "ResultItemList");
            PrintLine(ColoredItemListToString(ResultItemList, "TiticoreResultMessage", 0));
        }

        public static void PrintCaptchaRequestMessage(CaptchaRequestMessage msg, object tag)
        {
            PrintLine("CaptchaRequestMessage:");
            PrintLine("\tAuthCode={0}", msg.AuthCode); //System.Int32 has a toString()
            PrintLine("\tImage={0}",BitConverter.ToString(msg.Image.ToArray())); //System.Collections.Generic.List`1[System.Byte]
            PrintLine("\tRemain={0}", msg.Remain); //System.Int32 has a toString()
            PrintLine("\tRecaptcha={0}", msg.Recaptcha); //System.Boolean has a toString()
        }

        public static void PrintTiticoreDisplayItemsMessage(TiticoreDisplayItemsMessage msg, object tag)
        {
            PrintLine("TiticoreDisplayItemsMessage:");
            ICollection<ColoredItem> TiticoreRareDisplayItems = GetPrivateProperty <ICollection<ColoredItem>> (msg, "TiticoreRareDisplayItems");
            PrintLine(ColoredItemListToString(TiticoreRareDisplayItems, "TiticoreRareDisplayItems", 1));

            ICollection<ColoredItem> TiticoreNormalDisplayItems = GetPrivateProperty <ICollection<ColoredItem>> (msg, "TiticoreNormalDisplayItems");
            PrintLine(ColoredItemListToString(TiticoreNormalDisplayItems, "TiticoreNormalDisplayItems", 1));

            ICollection<ColoredItem> TiticoreKeyItems = GetPrivateProperty <ICollection<ColoredItem>> (msg, "TiticoreKeyItems");
            PrintLine(ColoredItemListToString(TiticoreKeyItems, "TiticoreKeyItems", 1));
        }
    }
}

