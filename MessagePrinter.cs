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

        
        public void RegisterPrinters(MessageHandlerFactory mf, Dictionary<int, Guid> getGuid)
        {
            //Console.WriteLine("registering printers");
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
                    //Console.WriteLine("Not registering method {0}", m.Name);
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
                    //Console.WriteLine("Not registering method {0}", m.Name);
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
            Console.WriteLine(")");
            */
            Type msgType = paramList[0].ParameterType;//...Message Type

            if (registeredTypes.Contains(msgType))
            {
                //Console.WriteLine("Skipping");
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
            Console.WriteLine("CharacterListMessage:");
            Console.WriteLine("\tMaxFreeCharacterCount={0}", msg.MaxFreeCharacterCount);
            Console.WriteLine("\tMaxPurchasedCharacterCount={0}", msg.MaxPurchasedCharacterCount);
            Console.WriteLine("\tMaxPremiumCharacters={0}", msg.MaxPremiumCharacters);
            Console.WriteLine("\tProloguePlayed={0}", msg.ProloguePlayed);
            Console.WriteLine("\tPresetUsedCharacterCount={0}", msg.PresetUsedCharacterCount);
            Console.WriteLine("\tLoginPartyState=[{0}]", String.Join(",", msg.LoginPartyState));
			if(msg.Characters == null || msg.Characters.Count == 0) {
				return;
			}

            int i = 0;
            characters = msg.Characters.ToList();
            foreach (CharacterSummary c in msg.Characters)
            {
                String title = String.Format("Character[{0}]", i++);
                Console.WriteLine(CharacterSummaryToString(c, title, 1));
            }
        }

        private static string IntToRGB(int val)
        {
            if (val == -1)
            {
                return "-1";
            }
            Color c = Color.FromArgb(val);
            StringBuilder sb = new StringBuilder();
            sb.Append("(R=");
            sb.Append(c.R);
            sb.Append(",G=");
            sb.Append(c.G);
            sb.Append(",B=");
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

        private static String ColorDictToString(IDictionary<int, int> dict, String name, int numTabs, IDictionary<int, int> otherDict)
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

        private static string BodyShapeInfoToString(IDictionary<int, float> BodyShapeInfo, int numTabs, IDictionary<int, float> otherBodyShapeInfo)
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
            Console.WriteLine("MailListMessage:");
            if (msg.ReceivedMailList != null && msg.ReceivedMailList.Count != 0)
            {
                Console.WriteLine(ListToString<BriefMailInfo>(msg.ReceivedMailList, "ReceivedMailList", 1));
            }
            if (msg.SentMailList != null && msg.SentMailList.Count != 0)
            {
                Console.WriteLine(ListToString<BriefMailInfo>(msg.SentMailList, "SentMailList", 1));
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
            Console.WriteLine("StatusEffectUpdated:");
            Console.WriteLine("\tCharacterName={0}", msg.CharacterName);
            Console.WriteLine(StatusEffectListToString(msg.StatusEffects, "StatusEffects", msg.CharacterName, 1));
        }

        public static void PrintQuestProgressMessage(QuestProgressMessage msg, object tag)
        {
            Console.WriteLine("QuestProgressMessage:");
            Console.WriteLine(ListToString<QuestProgressInfo>(msg.QuestProgress, "QuestProgress", 1));
            if (msg.AchievedGoals != null && msg.AchievedGoals.Count != 0)
            {
                Console.WriteLine(ListToString<AchieveGoalInfo>(msg.AchievedGoals, "AchievedGoals", 1));
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
            Console.WriteLine("UpdateSharedStorageInfoMessage:");
            Console.WriteLine("\tStorageInfos:");
            foreach (StorageInfo info in msg.StorageInfos)
            {
                Console.WriteLine("\t\tstorageID={0} isAvailable={1} storageName={2} storageTag={3}", info.StorageID, info.IsAvailable, info.StorageName, info.StorageTag);
            }
        }

        public static void PrintInventoryInfoMessage(InventoryInfoMessage msg, object tag)
        {
            //TODO: db connect to share inventory
            Console.WriteLine("InventoryInfoMessage:");
            Console.WriteLine("\tStorageInfos:");
            foreach (StorageInfo info in msg.StorageInfos)
            {
                Console.WriteLine("\t\tstorageID={0} isAvailable={1} storageName={2} storageTag={3}", info.StorageID, info.IsAvailable, info.StorageName, info.StorageTag);
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
                    Console.WriteLine(infoStr);
                }
            }
            inventoryItems = newInventoryItems;

            Console.WriteLine(DictToString<int, long>(msg.EquipmentInfo, "EquipmentInfo", 1, lastEquipmentInfo));
            Console.WriteLine(QuickSlotInfoToString(msg.QuickSlotInfo, "QuickSlotInfo", 1));
            lastEquipmentInfo = msg.EquipmentInfo;
            Console.WriteLine("\tUnequippableParts=[{0}]", String.Join(",", msg.UnequippableParts));
        }

        public static void PrintTitleListMessage(TitleListMessage msg, object tag)
        {
            //TODO: fully parse
            Console.WriteLine("TitleListMessage:");
            if (msg.AccountTitles != null && msg.AccountTitles.Count != 0)
            {
                Console.WriteLine(ListToString<TitleSlotInfo>(msg.AccountTitles, "AccountTitles", 1));
            }
            if (msg.Titles != null && msg.Titles.Count != 0)
            {
                Console.WriteLine(ListToString<TitleSlotInfo>(msg.Titles, "Titles", 1));
            }
        }

        public static void PrintRandomRankInfoMessage(RandomRankInfoMessage msg, object tag)
        {
            Console.WriteLine("RandomRankInfoMessage:");
            foreach (RandomRankResultInfo info in msg.RandomRankResult)
            {
                Console.WriteLine("\tRandomRankResult:");
                Console.WriteLine("\t\tEventID={0}", info.EventID);
                Console.WriteLine("\t\tPeriodType={0}", info.PeriodType);
                if (info.RandomRankResult != null && info.RandomRankResult.Count != 0)
                {
                    Console.WriteLine(ListToString<RankResultInfo>(info.RandomRankResult, "RandomRankResult", 2));
                }
            }
        }

        public static void PrintManufactureInfoMessage(ManufactureInfoMessage msg, object tag)
        {
            Console.WriteLine("ManufactureInfoMessage:");
            if (msg.ExpDictionary != null && msg.ExpDictionary.Count != 0)
            {
                Console.WriteLine(DictToString<string, int>(msg.ExpDictionary, "ExpDictionary", 1));
            }
            if (msg.GradeDictionary != null && msg.GradeDictionary.Count != 0)
            {
                Console.WriteLine(DictToString<string, int>(msg.GradeDictionary, "GradeDictionary", 1));
            }
            if (msg.Recipes != null && msg.Recipes.Count != 0)
            {
                Console.WriteLine(ListToString<string>(msg.Recipes, "Recipes", 1));
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
            Console.WriteLine("NotifyAction:");
            Console.WriteLine("\tID={0}", msg.ID);
            ActionSync last = lastNotifyAction == null ? emptyActionSync : lastNotifyAction.Action;
            Console.WriteLine(ActionSyncToString(msg.Action, "Action", 1, last));
            lastNotifyAction = msg;
            if (MongoDBConnect.connection != null) {
                MongoDBConnect.connection.InsertNotifyAction(msg, channel, TownID);
            }
            
        }

        public static void PrintDisappeared(Disappeared msg, object tag)
        {
            Console.WriteLine("Disappeared: ID={0}", msg.ID);
        }

        public static void PrintUserLoginMessage(UserLoginMessage msg, object tag)
        {
            Console.WriteLine("UserLoginMessage:");
            Console.WriteLine("\tPassport={0}", msg.Passport);
            Console.WriteLine("\tLocalAddress={0}", new IPAddress(msg.LocalAddress));
            if (msg.hwID != null && msg.hwID.Length != 0)
            {
                Console.WriteLine("\thwID={0}", msg.hwID);
            }

            Console.WriteLine("\t{0}", new MachineID(msg.MachineID));
            Console.WriteLine("\tGameRoomClient={0}", msg.GameRoomClient);
            Console.WriteLine("\tIsCharacterSelectSkipped={0}", msg.IsCharacterSelectSkipped);
            if (msg.NexonID != null && msg.NexonID.Length != 0)
            {
                Console.WriteLine("\tNexonID={0}", msg.NexonID);
            }
            if (msg.UpToDateInfo != null && msg.UpToDateInfo.Length != 0)
            {
                Console.WriteLine("\tUpToDateInfo={0}", msg.UpToDateInfo);
            }
            Console.WriteLine("\tCheckSum={0}", msg.CheckSum);
        }


        public static void PrintClientLogMessage(ClientLogMessage msg, object tag)
        {
            String logType = ((object)(ClientLogMessage.LogTypes)msg.LogType).ToString();
            Console.WriteLine("ClientLogMessage: {0} {1}={2}", logType, msg.Key, msg.Value);
        }

        public static void PrintEnterRegion(EnterRegion msg, object tag)
        {
            Console.WriteLine("EnterRegion: RegionCode={0}", msg.RegionCode);
        }

        public static void PrintQueryCharacterCommonInfoMessage(QueryCharacterCommonInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryCharacterCommonInfoMessage: [QueryID={0} CID={1}]", msg.QueryID, msg.CID);
        }

        public static void PrintRequestJoinPartyMessage(RequestJoinPartyMessage msg, object tag)
        {
            Console.WriteLine("RequestJoinPartyMessage: RequestType={0}", msg.RequestType);
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

            Console.WriteLine("EnterChannel:");
            Console.WriteLine("\tChannelID={0}", msg.ChannelID);
            Console.WriteLine("\tPartitionID={0}", msg.PartitionID);
            Console.WriteLine(ActionSyncToString(msg.Action, "Action", 1, emptyActionSync));
        }

        public static void PrintQueryRankAlarmInfoMessage(QueryRankAlarmInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryRankAlarmInfoMessage: CID={0}", msg.CID);
        }

        public static void PrintQueryNpcTalkMessage(QueryNpcTalkMessage msg, object tag)
        {
            Console.WriteLine("QueryNpcTalkMessage:");
            Console.WriteLine("\tLocation={0}", msg.Location);
            Console.WriteLine("\tNpcID={0}", msg.NpcID);
            Console.WriteLine("\tStoryLine={0}", msg.StoryLine);
            Console.WriteLine("\tCommand={0}", msg.Command);
        }

        public static void PrintQueryBattleInventoryInTownMessage(QueryBattleInventoryInTownMessage msg, object tag)
        {
            Console.WriteLine("QueryBattleInventoryInTownMessage: []");
        }

        public static void PrintIdentify(ServiceCore.EndPointNetwork.Identify msg, object tag)
        {
            Console.WriteLine("Identify: ID={0} Key={1}", msg.ID, msg.Key);
        }

        public static int TownID = 0;

        public static void PrintHotSpringRequestInfoMessage(HotSpringRequestInfoMessage msg, object tag)
        {
            if (msg == null)
            {
                return;
            }
            Console.WriteLine("HotSpringRequestInfoMessage: Channel={0} TownID={1}", msg.Channel, msg.TownID);
            TownID = msg.TownID;
        }

        public static void PrintMovePartition(MovePartition msg, object tag)
        {
            Console.WriteLine("MovePartition: TargetPartitionID={0}", msg.TargetPartitionID);
        }

        public static void PrintTradeItemClassListSearchMessage(TradeItemClassListSearchMessage msg, object tag)
        {
            Console.WriteLine("TradeItemClassListSearchMessage:");
            Console.WriteLine("\tuniqueNumber={0}", msg.uniqueNumber);
            Console.WriteLine("\tChunkPageNumber={0}", msg.ChunkPageNumber);
            Console.WriteLine("\tOrder={0}", msg.Order);
            Console.WriteLine("\tisDescending={0}", msg.isDescending);
            Console.WriteLine(ListToString<string>(msg.ItemClassList, "ItemClassList", 1));
            Console.WriteLine("\tDetailOptions:");
            foreach (DetailOption d in msg.DetailOptions)
            {
                Console.WriteLine("\t\t{0}={1} SearchType={2}", d.Key, d.Value, d.SearchType);
            }
        }

        private static UpdateAction lastUpdateAction = null;

        public static void PrintUpdateAction(UpdateAction msg, object tag)
        {
            ActionSync last = lastUpdateAction == null ? emptyActionSync : lastUpdateAction.Data;
            Console.WriteLine(ActionSyncToString(msg.Data, "UpdateAction", 0, last));
            lastUpdateAction = msg;
        }

        public static void PrintTradeCategorySearchMessage(TradeCategorySearchMessage msg, object tag)
        {
            Console.WriteLine("TradeCategorySearchMessage:");
            Console.WriteLine("\ttradeCategory={0}", msg.tradeCategory);
            Console.WriteLine("\ttradeCategorySub={0}", msg.tradeCategorySub);
            Console.WriteLine("\tminLevel={0}", msg.minLevel);
            Console.WriteLine("\tmaxLevel={0}", msg.maxLevel);
            Console.WriteLine("\tuniqueNumber={0}", msg.uniqueNumber);
            Console.WriteLine("\tChunkPageNumber={0}", msg.ChunkPageNumber);
            Console.WriteLine("\tOrder={0}", msg.Order);
            Console.WriteLine("\tisDescending={0}", msg.isDescending);
            Console.WriteLine("\tDetailOptions:");
            foreach (DetailOption d in msg.DetailOptions)
            {
                Console.WriteLine("\t\t{0}={1} SearchType={2}", d.Key, d.Value, d.SearchType);
            }
        }

        public static void PrintCreateCharacterMessage(CreateCharacterMessage msg, object tag)
        {
            Console.WriteLine("CreateCharacterMessage:");
            Console.WriteLine("\tName={0}", msg.Name);
            Console.WriteLine("\tTemplace:");
            CharacterTemplate t = msg.Template;
            Console.WriteLine("\t\tCharacterClass={0}", BaseCharacterToString((BaseCharacter)t.CharacterClass));
            Console.WriteLine("\t\tSkinColor={0}", IntToRGB(t.SkinColor));
            Console.WriteLine("\t\tShineness={0}", t.Shineness);
            Console.WriteLine("\t\tEyeColor={0}", IntToRGB(t.EyeColor));
            Console.WriteLine("\t\tHeight={0}", t.Height);
            Console.WriteLine("\t\tBust={0}", t.Bust);
            Console.WriteLine("\t\tPaintingPosX={0}", t.PaintingPosX);
            Console.WriteLine("\t\tPaintingPosY={0}", t.PaintingPosY);
            Console.WriteLine("\t\tPaintingRotation={0}", t.PaintingRotation);
            Console.WriteLine("\t\tPaintingSize={0}", t.PaintingSize);
            Console.WriteLine(BodyShapeInfoToString(t.BodyShapeInfo, 2, null));
        }
        public static void PrintCheckCharacterNameMessage(CheckCharacterNameMessage msg, object tag)
        {
            Console.WriteLine("CheckCharacterNameMessage:");
            Console.WriteLine("\tName={0}", msg.Name);
            Console.WriteLine("\tIsNameChange={0}", msg.IsNameChange);
        }

        public static void PrintQueryReservedInfoMessage(QueryReservedInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryReservedInfoMessage: []");
        }

        public static void Print_UpdatePlayState(_UpdatePlayState msg, object tag)
        {
            Console.WriteLine("_UpdatePlayState: State={0}", msg.State);
        }

        public static void PrintLogOutMessage(LogOutMessage msg, object tag)
        {
            Console.WriteLine("LogOutMessage: []");
        }

        public static void PrintSyncFeatureMatrixMessage(SyncFeatureMatrixMessage msg, object tag)
        {
            Console.WriteLine(DictToString<String, String>(msg.FeatureDic, "SyncFeatureMatrixMessage", 0));
        }
        public static void PrintGiveAPMessage(GiveAPMessage msg, object tag)
        {
            Console.WriteLine("GiveAPMessage: AP={0}", msg.AP);
        }

        public static void PrintUserIDMessage(UserIDMessage msg, object tag)
        {
            Console.WriteLine("UserIDMessage: {0}", msg.UserID);
        }

        public static void PrintAnswerFinishQuestMessage(AnswerFinishQuestMessage msg, object tag)
        {
            Console.WriteLine("AnswerFinishQuestMessage: FollowHost={0}", msg.FollowHost);
        }

        public static void PrintExchangeMileageResultMessage(ExchangeMileageResultMessage msg, object tag)
        {
            bool IsSuccess = GetPrivateProperty<bool>(msg, "IsSuccess");
            Console.WriteLine("ExchangeMileageResultMessage: IsSuccess={0}", IsSuccess);
        }
        public static void PrintSecuredOperationMessage(SecuredOperationMessage msg, object tag)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SecuredOperationMessage: Operation=");
            sb.Append(msg.Operation);
            sb.Append("LockedTime=");
            sb.Append(msg.LockedTimeInSeconds);
            Console.WriteLine(sb.ToString());
        }
        public static void PrintUseInventoryItemWithCountMessage(UseInventoryItemWithCountMessage msg, object tag)
        {
            Console.WriteLine("UseInventoryItemWithCountMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID);
            Console.WriteLine("\tTargetItemID={0}", msg.TargetItemID);
            Console.WriteLine("\tTargetItemCount={0}", msg.TargetItemCount);

        }
        public static void PrintAllUserJoinCompleteMessage(AllUserJoinCompleteMessage msg, object tag)
        {
            Console.WriteLine("AllUserJoinCompleteMessage: []");
        }

        public static void PrintRequestMarbleProcessNodeMessage(RequestMarbleProcessNodeMessage msg, object tag)
        {
            Console.WriteLine("RequestMarbleProcessNodeMessage: CurrentIndex={0}", msg.CurrentIndex);
        }
        public static void PrintQuerySharedInventoryMessage(QuerySharedInventoryMessage msg, object tag)
        {
            Console.WriteLine("QuerySharedInventoryMessage: []");
        }

        public static void PrintUpdateHistoryBookMessage(UpdateHistoryBookMessage msg, object tag)
        {
            Console.WriteLine("UpdateHistoryBookMessage:");
            Console.WriteLine("\tType={0}", msg.Type);

            String arr = msg.HistoryBooks != null ? String.Join(",", msg.HistoryBooks) : "";
            Console.WriteLine("\tHistoryBooks=[{0}]", arr);
        }

        public static void PrintRequestItemCombinationMessage(RequestItemCombinationMessage msg, object tag)
        {
            Console.WriteLine("RequestItemCombinationMessage:");
            Console.WriteLine("\tcombinedEquipItemClass={0}", msg.combinedEquipItemClass);
            Console.WriteLine("\tpartsIDList=[{0}]", String.Join(",", msg.partsIDList));

        }
        public static void PrintGiveCashShopDiscountCouponResultMessage(GiveCashShopDiscountCouponResultMessage msg, object tag)
        {
            bool IsSuccess = GetPrivateProperty<bool>(msg, "IsSuccess");
            Console.WriteLine("GiveCashShopDiscountCouponResultMessage: result={0}", IsSuccess);
        }

        public static void PrintOpenCustomDialogUIMessage(OpenCustomDialogUIMessage msg, object tag)
        {
            Console.WriteLine("OpenCustomDialogUIMessage:");
            Console.WriteLine("\tDialogType={0}", msg.DialogType); //System.Int32 has a toString()
            Console.WriteLine("\tArg=[{0}]",String.Join(",",msg.Arg)); //System.Collections.Generic.List`1[System.String]
        }

        public static void PrintQuestListMessage(QuestListMessage msg, object tag)
        {
            Console.WriteLine("QuestListMessage:");
            Console.WriteLine("\tQuestList=[{0}]",String.Join(",",msg.QuestList)); //System.Collections.Generic.ICollection`1[System.String]
        }

        public static void PrintSaveHousingPropsMessage(SaveHousingPropsMessage msg, object tag)
        {
            Console.WriteLine("SaveHousingPropsMessage:");
            foreach (HousingPropInfo info in msg.PropList)
            {
                if (info != null)
                {
                    Console.WriteLine("\t{0}", info.ToString());
                }
            }
        }

        public static void PrintUpdateHousingPropsMessage(UpdateHousingPropsMessage msg, object tag)
        {
            Console.WriteLine("UpdateHousingPropsMessage:");
            foreach (HousingPropInfo info in msg.PropList)
            {
                if (info != null)
                {
                    Console.WriteLine("\t{0}", info.ToString());
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
            Console.WriteLine("HousingMemberInfoMessage:");
            Console.WriteLine(GameJoinMemberInfoToString(msg.MemberInfo, 1));
        }

        public static void PrintHousingKickMessage(HousingKickMessage msg, object tag)
        {
            Console.WriteLine("HousingKickMessage: Slot={0}, NexonSN={1}", msg.Slot, msg.NexonSN);
        }

        public static void PrintHousingInvitedMessage(HousingInvitedMessage msg, object tag)
        {
            Console.WriteLine("HousingInvitedMessage: HostName={0} HousingID={1}", msg.HostName, msg.HousingID);
        }

        public static void PrintHousingHostRestartingMessage(HousingHostRestartingMessage msg, object tag)
        {
            Console.WriteLine("HousingHostRestartingMessage: []");
        }

        public static void PrintEnterHousingMessage(EnterHousingMessage msg, object tag)
        {
            Console.WriteLine("EnterHousingMessage: CharacterName={0} HousingIndex={1} EnterType={2} HousingPlayID={3}", msg.CharacterName, msg.HousingIndex, msg.EnterType, msg.HousingPlayID);
        }
        public static void PrintEndPendingDialogMessage(EndPendingDialogMessage msg, object tag)
        {
            Console.WriteLine("EndPendingDialogMessage []");
        }

        public static void PrintCreateHousingMessage(CreateHousingMessage msg, object tag)
        {
            Console.WriteLine("CreateHousingMessage: OpenLevel={0} Desc={1}", msg.OpenLevel, msg.Desc);
        }

        public static void PrintHotSpringRequestPotionEffectMessage(HotSpringRequestPotionEffectMessage msg, object tag)
        {
            Console.WriteLine("HotSpringRequestPotionEffectMessage: Channel={0} TownID={1} PotionItemClass={2}", msg.Channel, msg.TownID, msg.PotionItemClass);
        }

        public static void PrintHotSpringAddPotionMessage(HotSpringAddPotionMessage msg, object tag)
        {
            Console.WriteLine("HotSpringAddPotionMessage: Channel={0} TownID={1} ItemID={2}", msg.Channel, msg.TownID, msg.ItemID);
        }

        public static void PrintBurnItemsMessage(BurnItemsMessage msg, object tag)
        {
            Console.WriteLine("BurnItemsMessage:");
            foreach (BurnItemInfo info in msg.BurnItemList)
            {
                Console.WriteLine("\tItemID={0} Count={1}", info.ItemID, info.Count);
            }
        }

        public static void PrintFreeTitleNameCheckMessage(FreeTitleNameCheckMessage msg, object tag)
        {
            Console.WriteLine("FreeTitleNameCheckMessage: ItemID={0} FreeTitleName={1}", msg.ItemID, msg.FreeTitleName);
        }

        public static void PrintBurnRewardItemsMessage(BurnRewardItemsMessage msg, object tag)
        {
            Console.WriteLine("BurnRewardItemsMessage:");
            Console.WriteLine(DictToString<string, int>(msg.RewardItems, "RewardItems", 1));
            Console.WriteLine(DictToString<string, int>(msg.RewardMailItems, "RewardMailItems", 1));
        }

        public static void PrintAllUserGoalEventModifyMessage(AllUserGoalEventModifyMessage msg, object tag)
        {
            Console.WriteLine("AllUserGoalEventModifyMessage: GoalID={0} Count={1}", msg.GoalID, msg.Count);
        }

        public static void PrintAvatarSynthesisItemMessage(AvatarSynthesisItemMessage msg, object tag)
        {
            Console.WriteLine("AvatarSynthesisItemMessage: Material1ID={0} Material2ID={1} Material3ID={2}", msg.Material1ID, msg.Material2ID, msg.Material3ID);
        }

        public static void PrintGetFriendshipPointMessage(GetFriendshipPointMessage msg, object tag)
        {
            Console.WriteLine("GetFriendshipPointMessage []");
        }

        public static void PrintExchangeMileageMessage(ExchangeMileageMessage msg, object tag)
        {
            Console.WriteLine("ExchangeMileageMessage []");
        }

        public static void PrintCaptchaResponseMessage(CaptchaResponseMessage msg, object tag)
        {
            Console.WriteLine("CaptchaResponseMessage: AuthCode={0} Response={1}", msg.AuthCode, msg.Response);
        }

        public static void PrintGuildChatMessage(GuildChatMessage msg, object tag)
        {
            Console.WriteLine("GuildChatMessage:");
            Console.WriteLine("\tSender={0}", msg.Sender);
            Console.WriteLine("\tMessage={0}", msg.Message);
        }

        public static void PrintChangeMasterMessage(ChangeMasterMessage msg, object tag)
        {
            Console.WriteLine("ChangeMasterMessage:");
            Console.WriteLine("\tNewMasterName={0}", msg.NewMasterName); //System.String has a toString()
        }

        public static void PrintGuildGainGPMessage(GuildGainGPMessage msg, object tag)
        {
            Console.WriteLine("GuildGainGPMessage:");
            Console.WriteLine("\tGuildPoint={0}", msg.GuildPoint); //System.Int64 has a toString()
            Console.WriteLine(DictToString<byte,int>(msg.DailyGainGP,"DailyGainGP",1)); //System.Collections.Generic.Dictionary`2[System.Byte,System.Int32]
        }

        public static void PrintGuildLevelUpMessage(GuildLevelUpMessage msg, object tag)
        {
            Console.WriteLine("GuildLevelUpMessage:");
            Console.WriteLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
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
            Console.WriteLine("HousingRoomListMessage:");
            Console.WriteLine(ListToString<HousingRoomInfo>(msg.HousingRoomList, "HousingRoomList", 1));
        }

        public static void PrintHotSpringRequestInfoResultMessage(HotSpringRequestInfoResultMessage msg, object tag)
        {
            Console.WriteLine("HotSpringRequestInfoResultMessage: TownID={0}", msg.TownID);
            foreach (HotSpringPotionEffectInfo info in msg.HotSpringPotionEffectInfos)
            {
                Console.WriteLine("\tPotionItemClass={0} CharacterName={1} GuildName={2} ExpiredTime={3} OtherPotionUsableTime={4}", info.PotionItemClass, info.CharacterName, info.GuildName, info.ExpiredTime, info.OtherPotionUsableTime);
            }
        }
        public static void PrintBurnJackpotMessage(BurnJackpotMessage msg, object tag)
        {
            Console.WriteLine("BurnJackpotMessage: CID={0}", msg.CID);
        }

        public static void PrintRequestMarbleCastDiceMessage(RequestMarbleCastDiceMessage msg, object tag)
        {
            Console.WriteLine("RequestMarbleCastDiceMessage: DiceID={0}", msg.DiceID);
        }

        public static void PrintUpdateHousingItemsMessage(UpdateHousingItemsMessage msg, object tag)
        {
            Console.WriteLine("UpdateHousingItemsMessage: ClearInven={0}", msg.ClearInven);
            foreach (HousingItemInfo info in msg.ItemList)
            {
                Console.WriteLine("\t{0}", info);
            }

        }
        public static void PrintHousingPartyInfoMessage(HousingPartyInfoMessage msg, object tag)
        {
            Console.WriteLine("HousingPartyInfoMessage:");
            Console.WriteLine("\tHousingID={0}", msg.HousingID); //System.Int64 has a toString()
            Console.WriteLine("\tPartySize={0}", msg.PartySize); //System.Int32 has a toString()
            Console.WriteLine("\tMembers:");
            foreach (HousingPartyMemberInfo m in msg.Members) {
                Console.WriteLine("\t\tHousingPartyMemberInfo:");
                Console.WriteLine("\t\t\tNexonSN={0}", m.NexonSN); //System.Int32 has a toString()
                Console.WriteLine("\t\t\tCharacter={0}", m.Character); //ServiceCore.CharacterServiceOperations.BaseCharacter
                Console.WriteLine("\t\t\tCharacterName={0}", m.CharacterName); //System.String has a toString()
                Console.WriteLine("\t\t\tSlotNumber={0}", m.SlotNumber); //System.Int32 has a toString()
                Console.WriteLine("\t\t\tLevel={0}", m.Level); //System.Int32 has a toString()
            }
            Console.WriteLine("\tMembers={0}",msg.Members); //System.Collections.Generic.ICollection`1[ServiceCore.EndPointNetwork.Housing.HousingPartyMemberInfo]
        }

        public static void PrintAddFriendShipResultMessage(AddFriendShipResultMessage msg, object tag)
        {
            String result = ((AddFriendShipResultMessage.AddFriendShipResult)msg.Result).ToString();
            Console.WriteLine("AddFriendShipResultMessage: friendName={0} Result={1}", msg.friendName, result);
        }

        public static void PrintHousingInvitationRejectMessage(HousingInvitationRejectMessage msg, object tag)
        {
            Console.WriteLine("HousingInvitationRejectMessage: HousingID={0}",msg.HousingID);
        }

        public static void PrintEnhanceSuccessRatioDebugMessage(EnhanceSuccessRatioDebugMessage msg, object tag)
        {
            Console.WriteLine("EnhanceSuccessRatioDebugMessage:");
            Console.WriteLine("\tSuccessRatio={0}", msg.SuccessRatio); //System.Single has a toString()
            Console.WriteLine("\tBonusRatio={0}", msg.BonusRatio); //System.Single has a toString()
            Console.WriteLine("\tFeaturebonusratio={0}", msg.Featurebonusratio); //System.Single has a toString()
        }

        public static void PrintHasSecondPasswordMessage(HasSecondPasswordMessage msg, object tag)
        {
            Console.WriteLine("HasSecondPasswordMessage:");
            Console.WriteLine("\tIsFirstQuery={0}", msg.IsFirstQuery);
            Console.WriteLine("\tHasSecondPassword={0}", msg.HasSecondPassword);
            Console.WriteLine("\tIsPassed={0}", msg.IsPassed);
            Console.WriteLine("\tFailCount={0}", msg.FailCount);
            Console.WriteLine("\tRetryLockedSec={0}", msg.RetryLockedSec);
        }

        public static void PrintGetUserIDMessage(GetUserIDMessage msg, object type)
        {
            Console.WriteLine("GetUserIDMessage: []");
        }

        public static void PrintMakeNamedRingMessage(MakeNamedRingMessage msg, object type)
        {
            Console.WriteLine("MakeNamedRingMessage: ItemID={0} UserName={1}", msg.ItemID, msg.UserName);
        }

        public static void PrintMarbleInfoResultMessage(MarbleInfoResultMessage msg, object type)
        {
            Console.WriteLine("MarbleInfoResultMessage:");
            Console.WriteLine("\tMarbleID={0}", msg.MarbleID);
            Console.WriteLine("\tCurrentIndex={0}", msg.CurrentIndex);
            Console.WriteLine("\tNodeList:");
            foreach (MarbleNode node in msg.NodeList)
            {
                Console.WriteLine("\t\tMarbleNode:");
                Console.WriteLine("\t\t\tNodeIndex={0}", node.NodeIndex);
                Console.WriteLine("\t\t\tNodeType={0}", node.NodeType);
                Console.WriteLine("\t\t\tNodeGrade={0}", node.NodeGrade);
                Console.WriteLine("\t\t\tArg=[{0}]", String.Join(",", node.Arg));
                Console.WriteLine("\t\t\tDesc={0}", node.Desc);
            }
            Console.WriteLine("\tIsFirst={0}", msg.IsFirst);
            Console.WriteLine("\tIsProcessed={0}", msg.IsProcessed);
        }

        public static void PrintRequestPartChangingMessage(RequestPartChangingMessage msg, object type)
        {
            Console.WriteLine("RequestPartChangingMessage: combinedEquipItemID={0} targetIndex={1} partID={2}", msg.combinedEquipItemID, msg.targetIndex, msg.partID);
        }

        public static void PrintCaptchaResponseResultMessage(CaptchaResponseResultMessage msg, object tag)
        {
            int Result = GetPrivateProperty<int>(msg, "Result");
            Console.WriteLine("CaptchaResponseResultMessage: Result={0}", Result);
        }

        public static void PrintJoinGuildChatRoomMessage(JoinGuildChatRoomMessage msg, object tag)
        {
            Console.WriteLine("JoinGuildChatRoomMessage: GuildKey={0}", msg.GuildKey);
        }

        public static void PrintHousingListMessage(HousingListMessage msg, object tag)
        {
            Console.WriteLine("HousingListMessage: HousingList=[{0}]", String.Join(",", msg.HousingList));
        }

        public static void PrintAvatarSynthesisMaterialRecipesMessage(AvatarSynthesisMaterialRecipesMessage msg, object tag)
        {
            Console.WriteLine("AvatarSynthesisMaterialRecipesMessage: MaterialRecipies=[{0}]", String.Join(",", msg.MaterialRecipes));
        }

        public static void PrintAvatarSynthesisRequestMessage(AvatarSynthesisRequestMessage msg, object tag)
        {
            Console.WriteLine("AvatarSynthesisRequestMessage:");
            Console.WriteLine("\tFirstItemID={0}", msg.FirstItemID); //System.Int64 has a toString()
            Console.WriteLine("\tSecondItemID={0}", msg.SecondItemID); //System.Int64 has a toString()
        }

        public static void PrintGameResourceRespondMessage(GameResourceRespondMessage msg, object tag)
        {
            Console.WriteLine("GameResourceRespondMessage: ResourceRespond={0}", msg.ResourceRespond);
        }

        public static void PrintAllUserGoalEventMessage(AllUserGoalEventMessage msg, object tag)
        {
            Console.WriteLine("AllUserGoalEventMessage:");
            Console.WriteLine(DictToString<int, int>(msg.AllUserGoalInfo, "AllUserGoalInfo", 1));
        }

        public static void PrintLeaveHousingMessage(LeaveHousingMessage msg, object tag)
        {
            Console.WriteLine("LeaveHousingMessage []");
        }

        public static void PrintDecomposeItemResultMessage(DecomposeItemResultMessage msg, object tag)
        {
            Console.WriteLine("DecomposeItemResultMessage: ResultEXP={0}", msg.ResultEXP);
            Console.WriteLine(ListToString<string>(msg.GiveItemClassList, "GiveItemClassList", 1));

        }

        public static void PrintQueryAvatarSynthesisMaterialRecipesMessage(QueryAvatarSynthesisMaterialRecipesMessage msg, object tag)
        {
            Console.WriteLine("QueryAvatarSynthesisMaterialRecipesMessage: []");
        }

        public static void PrintSearchHousingRoomMessage(SearchHousingRoomMessage msg, object tag)
        {
            Console.WriteLine("SearchHousingRoomMessage: Option={0} Keyword={1}", msg.Option, msg.Keyword);
        }

        public static void PrintMarbleSetTimerMessage(MarbleSetTimerMessage msg, object tag)
        {
            Console.WriteLine("MarbleSetTimerMessage: Time={0}", msg.Time);
        }

        public static void PrintFreeTitleNameCheckResultMessage(FreeTitleNameCheckResultMessage msg, object tag)
        {
            Console.WriteLine("FreeTitleNameCheckResultMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID);
            Console.WriteLine("\tFreeTitleName={0}", msg.FreeTitleName);
            Console.WriteLine("\tIsSuccess={0}", msg.IsSuccess);
            Console.WriteLine("\tHasFreeTitle={0}", msg.HasFreeTitle);
        }

        public static void PrintInsertBlessStoneCompleteMessage(InsertBlessStoneCompleteMessage msg, object tag)
        {
            Console.WriteLine("InsertBlessStoneCompleteMessage:");
            Console.WriteLine("\tSlot={0}", msg.Slot);
            Console.WriteLine("\tOwnerList=[{0}]", String.Join(",", msg.OwnerList));
            Console.WriteLine("\tTypeList=[{0}]", String.Join(",", msg.TypeList));
            /*StringBuilder sb = new StringBuilder();
            if (msg.TypeList != null && msg.TypeList.Count != 0) {
                foreach (BlessStoneType t in msg.TypeList)
                {
                    sb.Append(BlessStoneToString(t));
                    sb.Append(",");
                }
                sb.Remove(sb.Length - 1, 1);
                Console.WriteLine("\tTypeList=[{0}]", sb.ToString());
            }*/
        }

        public static void PrintEnchantLimitlessMessage(EnchantLimitlessMessage msg, object tag)
        {
            Console.WriteLine("EnchantLimitlessMessage:");
            Console.WriteLine("\tenchantID={0}", msg.enchantID); //System.Int64 has a toString()
            Console.WriteLine("\tenchantLimitlessID={0}", msg.enchantLimitlessID); //System.Int64 has a toString()
        }

        public static void PrintRequestBraceletCombinationMessage(RequestBraceletCombinationMessage msg, object tag)
        {
            Console.WriteLine("RequestBraceletCombinationMessage:");
            Console.WriteLine("\tBreaceletItemID={0}", msg.BreaceletItemID); //System.Int64 has a toString()
            Console.WriteLine("\tGemstoneItemID={0}", msg.GemstoneItemID); //System.Int64 has a toString()
            Console.WriteLine("\tGemstoneIndex={0}", msg.GemstoneIndex); //System.Int32 has a toString()
            Console.WriteLine("\tIsChanging={0}", msg.IsChanging); //System.Boolean has a toString()
        }

        public static void PrintSwapHousingItemMessage(SwapHousingItemMessage msg, object tag)
        {
            Console.WriteLine("SwapHousingItemMessage:");
            Console.WriteLine("\tFrom={0}", msg.From); //System.Int32 has a toString()
            Console.WriteLine("\tTo={0}", msg.To); //System.Int32 has a toString()
        }

        public static void PrintMarbleProcessNodeResultMessage(MarbleProcessNodeResultMessage msg, object tag)
        {
            Console.WriteLine("MarbleProcessNodeResultMessage: Type={0} IsChance={1}", msg.Type, msg.IsChance);
        }

        public static void PrintBurnGaugeRequestMessage(BurnGaugeRequestMessage msg, object tag)
        {
            Console.WriteLine("BurnGaugeRequestMessage: []");
        }

        public static void PrintSetQuoteMessage(SetQuoteMessage msg, object tag)
        {
            Console.WriteLine("SetQuoteMessage:");
            Console.WriteLine("\tQuote={0}", msg.Quote); //System.String has a toString()
        }

        public static void PrintRequestAttendanceRewardMessage(RequestAttendanceRewardMessage msg, object tag)
        {
            Console.WriteLine("RequestAttendanceRewardMessage:");
            Console.WriteLine("\tEventType={0}", msg.EventType); //System.Int32 has a toString()
            Console.WriteLine("\tIsBonus={0}", msg.IsBonus); //System.Boolean has a toString()
        }

        public static void PrintHousingGameHostedMessage(HousingGameHostedMessage msg, object tag)
        {
            Console.WriteLine("HousingGameHostedMessage: Map={0} IsOwner={1}", msg.Map, msg.IsOwner);
            Console.WriteLine(ListToString<HousingPropInfo>(msg.HousingProps, "HousingProps", 1));
            Console.WriteLine("\tHostInfo:");
            Console.WriteLine(GameJoinMemberInfoToString(msg.HostInfo, 2));
        }

        public static void PrintHousingKickedMessage(HousingKickedMessage msg, object tag)
        {
            Console.WriteLine("HousingKickedMessage: []");
        }

        public static void PrintBuyIngameCashshopUseTirMessage(BuyIngameCashshopUseTirMessage msg, object tag)
        {
            Console.WriteLine("BuyIngameCashshopUseTirMessage: Products=[{0}]", String.Join(",", msg.Products));
        }

        public static void PrintRequestAddPeerMessage(RequestAddPeerMessage msg, object tag)
        {
            Console.WriteLine("RequestAddPeerMessage: PingEntityIDs=[{0}]", String.Join(",", msg.PingEntityIDs));
        }

        public static void PrintMaxDurabilityRepairItemMessage(MaxDurabilityRepairItemMessage msg, object tag)
        {
            Console.WriteLine("MaxArmorRepairItemMessage: TargetItemID={0} SourceItemID={1}", msg.TargetItemID, msg.SourceItemID);
        }

        public static void PrintSecondPasswordResultMessage(SecondPasswordResultMessage msg, object tag)
        {
            Console.WriteLine("SecondPasswordResultMessage:");
            Console.WriteLine("\tOperationType={0}", msg.OperationType); //ServiceCore.EndPointNetwork.SecondPasswordResultMessage+ProcessType
            Console.WriteLine("\tPassed={0}", msg.Passed); //System.Boolean has a toString()
            Console.WriteLine("\tFailCount={0}", msg.FailCount); //System.Int32 has a toString()
            Console.WriteLine("\tRetryLockedSec={0}", msg.RetryLockedSec); //System.Int32 has a toString()
        }

        private static string IntToDecorationSlot(int key)
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

        private static string IntToDecorationColorSlot(int key)
        {
            switch (key)
            {
                default:
                    return key.ToString();
            }
        }

        private static String CharacterDictToString<T>(IDictionary<int, T> dict, String name, int numTabs, IDictionary<int, T> otherDict)
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
            Console.WriteLine(CharacterSummaryToString(msg.Info, "CharacterCommonInfoMessage", 0));
        }

        public static long channel = 0;

        public static void PrintChannelServerAddress(ChannelServerAddress msg, object tag)
        {
            Console.WriteLine("ChannelServerAddress: ChannelID={0} Address={1} Port={2} Key={3}", msg.ChannelID, msg.Address, msg.Port, msg.Key);
            channel = msg.ChannelID;
        }

        public static void PrintSystemMessage(SystemMessage msg, object tag)
        {
            Console.WriteLine("SystemMessage:");
            Console.WriteLine("\tCategory={0}", msg.Category); //System.Byte has a toString()
            Console.WriteLine("\tMessage={0}", msg.Message); //ServiceCore.EndPointNetwork.HeroesString has a toString()
        }

        public static void PrintNpcTalkMessage(NpcTalkMessage msg, object tag)
        {
            Console.WriteLine(ListToString<NpcTalkEntity>(msg.Content, "NpcTalkMessage", 0));
        }
        public static void PrintHousingStartGrantedMessage(HousingStartGrantedMessage msg, object tag)
        {
            if (msg == null)
            {
                Console.WriteLine("HousingStartGrantedMessage: [null]", msg.NewSlot, msg.NewKey);
            }
            else
            {
                Console.WriteLine("HousingStartGrantedMessage: [NewSlot={0} NewKey={1}]", msg.NewSlot, msg.NewKey);
            }
        }

        public static void PrintUpdateStoryGuideMessage(UpdateStoryGuideMessage msg, object tag)
        {
            Console.WriteLine("UpdateStoryGuideMessage:");
            Console.WriteLine("\tTargetLandMark={0}", msg.TargetLandMark); //System.String has a toString()
            Console.WriteLine("\tGuideMessage={0}", msg.GuideMessage); //System.String has a toString()
        }

        public static void PrintAddFriendshipInfoMessage(AddFriendshipInfoMessage msg, object tag)
        {
            Console.WriteLine("AddFriendshipInfoMessage: FriendID={0} FriendLimitCount={1}", msg.FriendID, msg.FriendLimitCount);
        }

        public static void PrintSkillListMessage(SkillListMessage msg, object tag)
        {
            Console.WriteLine("SkillListMessage:");
            Console.WriteLine("\tLearningSkillID={0}",msg.LearningSkillID);
            Console.WriteLine("\tCurrentAp={0}",msg.CurrentAp);
            Console.WriteLine("\tIsResetVocation={0}",msg.IsResetVocation);

            Console.WriteLine("\tSkillList:");
            int i = 0;
            foreach(BriefSkillInfo b in msg.SkillList) {
                Console.WriteLine("\tBriefSkillInfo[{0}]:",i++);
                Console.WriteLine("\t\tSkillID={0}", b.SkillID); //System.String has a toString()
                Console.WriteLine("\t\tBaseRank={0}", b.BaseRank); //System.Int32 has a toString()
                Console.WriteLine("\t\tFinalRank={0}", b.FinalRank); //System.Int32 has a toString()
                Console.WriteLine("\t\tRequiredAP={0}", b.RequiredAP); //System.Int32 has a toString()
                Console.WriteLine("\t\tIsLocked={0}", b.IsLocked); //System.Byte has a toString()
                Console.WriteLine("\t\tCanStartTraining={0}", b.CanStartTraining); //System.Byte has a toString()
                Console.WriteLine("\t\tCurrentAP={0}", b.CurrentAP); //System.Int32 has a toString()
                Console.WriteLine("\t\tResetSkillAP={0}", b.ResetSkillAP); //System.Int32 has a toString()
                Console.WriteLine("\t\tUntrainSkillAP={0}", b.UntrainSkillAP); //System.Int32 has a toString()
                Console.WriteLine("\t\tEnhances:");
                foreach (BriefSkillEnhance e in b.Enhances) {
                    Console.WriteLine("\tBriefSkillEnhance: GroupKey={0} IndexKey={1} Type={2} ReduceDurability={3} MaxDurabilityBonus={4}", e.GroupKey, e.IndexKey, e.Type, e.ReduceDurability, e.MaxDurabilityBonus);
                }
            }
        }

        public static void PrintLoginOkMessage(LoginOkMessage msg, object tag)
        {
            Console.WriteLine("LoginOkMessage:");
            Console.WriteLine("\tRegionCode={0}",msg.RegionCode);
            Console.WriteLine("\tTime={0}",msg.Time);
            Console.WriteLine("\tLimited={0}",msg.Limited);
            Console.WriteLine("\tFacebookToken={0}",msg.FacebookToken);
            Console.WriteLine("\tUserCareType={0}",msg.UserCareType);
            Console.WriteLine("\tUserCareNextState={0}",msg.UserCareNextState);
            Console.WriteLine("\tMapStateInfo={0}",msg.MapStateInfo);
        }

        public static void PrintTodayMissionInitializeMessage(TodayMissionInitializeMessage msg, object tag)
        {
            Console.WriteLine("TodayMissionInitializeMessage:");

            Dictionary<int, TodayMissinState> missionStates = GetPrivateProperty<Dictionary<int, TodayMissinState>>(msg, "MissionState");
            int remainResetMinute = GetPrivateProperty<int>(msg, "RemainResetMinute");

            Console.WriteLine("\tRemainResetMinute={0}", remainResetMinute);
            if (missionStates != null && missionStates.Count != 0)
            {
                Console.WriteLine("\tMissionStates:");
                foreach (KeyValuePair<int, TodayMissinState> t in missionStates)
                {
                    if (t.Value != null)
                    {
                        Console.WriteLine("\t\t{0}=(ID={1} CurrentCount={2} IsFinished={3}", t.Key, t.Value.ID, t.Value.CurrentCount, t.Value.IsFinished);
                    }

                }
            }
        }

        public static void PrintAPMessage(APMessage msg, object tag)
        {
            Console.WriteLine("APMessage:");
            Console.WriteLine("\tAP={0}", msg.AP);
            Console.WriteLine("\tMaxAP={0}", msg.MaxAP);
            Console.WriteLine("\tNextBonusTimeTicks={0}", msg.NextBonusTimeTicks);
            Console.WriteLine("\tAPBonusInterval={0}", msg.APBonusInterval);
        }

        public static void PrintGuildResultMessage(GuildResultMessage msg, object tag)
        {
            Console.WriteLine("GuildResultMessage:");
            Console.WriteLine("\tResult={0}",msg.Result);
            Console.WriteLine("\tArg={0}",msg.Arg);
            Console.WriteLine("\tGuildID={0}",msg.GuildID);
        }

        public static void PrintCostumeUpdateMessage(CostumeUpdateMessage msg, object tag)
        {
            //TODO: db connect
            string s = CostumeInfoToString(msg.CostumeInfo, 0, character.CharacterID, "CostumeUpdateMessage");
            if (s == null || s.Length == 0)
            {
                Console.WriteLine("CostumeUpdateMessage:");
            }
            else
            {
                Console.WriteLine(s);
            }
        }

        private static IDictionary<int, long> lastEquipInfos = null;

        public static void PrintEquipmentInfoMessage(EquipmentInfoMessage msg, object tag)
        {
            Console.WriteLine(DictToString<int, long>(msg.EquipInfos, "EquipmentInfoMessage", 0, lastEquipInfos));
            lastEquipInfos = msg.EquipInfos;
        }

        private static IDictionary<string, int> lastStats = null;
        public static void PrintUpdateStatMessage(UpdateStatMessage msg, object tag)
        {
            string s = DictToString<string, int>(msg.Stat, "UpdateStatMessage", 0, lastStats);
            if (s.Length == 0)
            {
                Console.WriteLine("UpdateStatMessage:");
            }
            else
            {
                Console.WriteLine(s);
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
            Console.WriteLine("UpdateInventoryInfoMessage:");

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
                    Console.WriteLine(infoStr);
                }
            }

            inventoryItems = newInventoryItems;
        }

        public static void PrintFriendshipInfoListMessage(FriendshipInfoListMessage msg, object tag)
        {
            Console.WriteLine("FriendshipInfoListMessage: FriendList=[{0}]", String.Join(",", msg.FriendList));
        }

        public static void PrintNpcListMessage(NpcListMessage msg, object tag)
        {
            Console.WriteLine("NpcListMessage:");
            if (msg.Buildings == null)
            {
                return;
            }
            foreach (BuildingInfo b in msg.Buildings)
            {
                Console.WriteLine("\tBuildingID={0} Npcs=[{1}]", b.BuildingID, String.Join(",", b.Npcs));
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
            Console.WriteLine("UseCrateItemResultMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.UseCrateItemResultMessage+UseCrateItemResult
            String CrateItem = GetPrivateProperty<String>(msg, "CrateItem");
            Console.WriteLine("\tCrateItem={0}", CrateItem); //System.String has a toString()
            List<string> KeyItems = GetPrivateProperty<List<string>>(msg, "KeyItems");
            Console.WriteLine("\tKeyItems=[{0}]", String.Join(",", KeyItems)); //System.Collections.Generic.List`1[System.String]
        }

        public static void PrintUnregisterNewRecipesMessage(UnregisterNewRecipesMessage msg, object tag)
        {
            Console.WriteLine("UnregisterNewRecipesMessage:");
            Console.WriteLine("\tRecipeList={0}", String.Join(",", msg.RecipeList)); //System.Collections.Generic.ICollection`1[System.String]
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
            Console.WriteLine("MissionMessage:");
            Console.WriteLine("\tMID={0}", msg.MID); //System.String has a toString()
            Console.WriteLine("\tID={0}", msg.ID); //System.Int64 has a toString()
            Console.WriteLine("\tCategory={0}", msg.Category); //System.String has a toString()
            Console.WriteLine("\tTitle={0}", msg.Title); //System.String has a toString()
            Console.WriteLine("\tLocation={0}", msg.Location); //System.String has a toString()
            Console.WriteLine("\tDescription={0}", msg.Description); //System.String has a toString()
            Console.WriteLine("\tRequiredLevel={0}", msg.RequiredLevel); //System.Int32 has a toString()
            Console.WriteLine("\tRewardAP={0}", msg.RewardAP); //System.Int32 has a toString()
            Console.WriteLine("\tRewardEXP={0}", msg.RewardEXP); //System.Int32 has a toString()
            Console.WriteLine("\tRewardGold={0}", msg.RewardGold); //System.Int32 has a toString()
            Console.WriteLine("\tRewardItemIDs=[{0}]",String.Join(",",msg.RewardItemIDs)); //System.Collections.Generic.List`1[System.String]
            Console.WriteLine("\tRewardItemNums=[{0}]", String.Join(",", msg.RewardItemNums)); //System.Collections.Generic.List`1[System.Int32]
            Console.WriteLine("\tModifiedExpirationTime={0}", msg.ModifiedExpirationTime); //System.Int32 has a toString()
            Console.WriteLine("\tExpirationTime={0}", msg.ExpirationTime); //System.Int32 has a toString()
            Console.WriteLine("\tExpirationPeriod={0}", msg.ExpirationPeriod); //System.Int32 has a toString()
            Console.WriteLine("\tComplete={0}", msg.Complete); //System.Boolean has a toString()
            Console.WriteLine("\tQuestTitle0={0}", msg.QuestTitle0); //System.String has a toString()
            Console.WriteLine("\tProgress0={0}", msg.Progress0); //System.Int32 has a toString()
            Console.WriteLine("\tTotalProgress0={0}", msg.TotalProgress0); //System.Int32 has a toString()
            Console.WriteLine("\tQuestTitle1={0}", msg.QuestTitle1); //System.String has a toString()
            Console.WriteLine("\tProgress1={0}", msg.Progress1); //System.Int32 has a toString()
            Console.WriteLine("\tTotalProgress1={0}", msg.TotalProgress1); //System.Int32 has a toString()
            Console.WriteLine("\tQuestTitle2={0}", msg.QuestTitle2); //System.String has a toString()
            Console.WriteLine("\tProgress2={0}", msg.Progress2); //System.Int32 has a toString()
            Console.WriteLine("\tTotalProgress2={0}", msg.TotalProgress2); //System.Int32 has a toString()
            Console.WriteLine("\tQuestTitle3={0}", msg.QuestTitle3); //System.String has a toString()
            Console.WriteLine("\tProgress3={0}", msg.Progress3); //System.Int32 has a toString()
            Console.WriteLine("\tTotalProgress3={0}", msg.TotalProgress3); //System.Int32 has a toString()
            Console.WriteLine("\tQuestTitle4={0}", msg.QuestTitle4); //System.String has a toString()
            Console.WriteLine("\tProgress4={0}", msg.Progress4); //System.Int32 has a toString()
            Console.WriteLine("\tTotalProgress4={0}", msg.TotalProgress4); //System.Int32 has a toString()
        }

        public static void PrintTradeMyItemInfos(TradeMyItemInfos msg, object tag)
        {
            Console.WriteLine("TradeMyItemInfos:");
            int i = 0;
            foreach (TradeItemInfo item in msg.TradeItemList) {
                Console.WriteLine("TradeItemList[{0}]",i++);
                Console.WriteLine(TradeItemInfoToString(item, 1));
            }
            Console.WriteLine("\tresult={0}", msg.result); //System.Int32 has a toString()
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
            Console.WriteLine("TradeSearchResult:");
            Console.WriteLine("\tUniqueNumber={0}", msg.UniqueNumber);
            Console.WriteLine("\tIsMoreResult={0}", msg.IsMoreResult);
            Console.WriteLine("\tresult={0}", msg.result);
            Console.WriteLine("\tTradeItemList:");
            if (msg.TradeItemList == null)
            {
                return;
            }
            foreach (TradeItemInfo i in msg.TradeItemList)
            {
                string closeDate = DateTimeToString(CloseDateToDateTime(i.CloseDate));
                Console.WriteLine("\t\tTID={0} CID={1} ChracterName={2} ItemClass={3} ItemCount={4} ItemPrice={5} CloseDate={6} HasAttribute={7} MaxArmorCondition={8} color1={9} color2={10} color3={11}", i.TID, i.CID, i.ChracterName, i.ItemClass, i.ItemCount, i.ItemPrice, closeDate, i.HasAttribute, i.MaxArmorCondition, i.color1, i.color2, i.color3);
            }
        }

        public static void PrintAskSecondPasswordMessage(AskSecondPasswordMessage msg, object tag)
        {
            Console.WriteLine("AskSecondPasswordMessage: []");
        }

        public static void PrintNoticeGameEnvironmentMessage(NoticeGameEnvironmentMessage msg, object tag)
        {
            Console.WriteLine("NoticeGameEnvironmentMessage: CafeType={0} IsOTP={1}", msg.CafeType, msg.IsOTP);
        }

        public static void PrintSpSkillMessage(SpSkillMessage msg, object tag)
        {
            Console.WriteLine(DictToString<int, string>(msg.SpSkills, "SpSkillMessage", 0));
        }

        public static void PrintVocationSkillListMessage(VocationSkillListMessage msg, object tag)
        {
            Console.WriteLine(DictToString<string, int>(msg.SkillList, "VocationSkillListMessage", 0));
        }

        public static void PrintWhisperFilterListMessage(WhisperFilterListMessage msg, object tag)
        {
            Console.WriteLine(DictToString<string, int>(msg.Filter, "WhisperFilterListMessage", 0));
        }

        public static void PrintGetCharacterMissionStatusMessage(GetCharacterMissionStatusMessage msg, object tag)
        {
            Console.WriteLine("GetCharacterMissionStatusMessage:");
            Console.WriteLine("\tMissionCompletionCount={0}", msg.MissionCompletionCount);
            Console.WriteLine("\tRemainTimeToCleanMissionCompletionCount={0}", msg.RemainTimeToCleanMissionCompletionCount);
            Console.WriteLine("\tMissionList:");
            foreach (MissionMessage m in msg.MissionList)
            {
                Console.WriteLine("\t\tMID={0} Title={1} Location={2} Description={3}", m.MID, m.Title, m.Location, m.Description);
            }
        }

        public static void PrintSelectPatternMessage(SelectPatternMessage msg, object tag)
        {
            int pattern = GetPrivateProperty<int>(msg, "pattern");
            Console.WriteLine("SelectPatternMessage: pattern={0}", pattern);
        }

        public static void PrintQueryHousingItemsMessage(QueryHousingItemsMessage msg, object tag)
        {
            Console.WriteLine("QueryHousingItemsMessage: []");
        }

        public static void PrintFishingResultMessage(FishingResultMessage msg, object tag)
        {
            Console.WriteLine(ListToString<FishingResultInfo>(msg.FishingResult, "FishingResultMessage", 0));
        }

        public static void PrintPetListMessage(PetListMessage msg, object tag)
        {
            Console.WriteLine("PetListMessage:");
            Console.WriteLine("\tIsTotalPetList={0}", msg.IsTotalPetList);
            Console.WriteLine(ListToString<PetStatusInfo>(msg.PetList, "PetList", 1));
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
            Console.WriteLine("PetFeedListMessage:");
            Console.WriteLine("\tIsTotalPetList={0}", msg.IsTotalPetList);
            Console.WriteLine(ListToString(msg.PetFeedList, "PetFeedList", 1));
            lastPetFeedMsg = msg;
        }

        public static void PrintUpdateStorageInfoMessage(UpdateStorageInfoMessage msg, object tag)
        {
            Console.WriteLine("UpdateStorageInfoMessage:");
            foreach (StorageInfo info in msg.StorageInfos)
            {
                Console.WriteLine("\tstorageID={0} isAvailable={1} storageName={2} storageTag={3}", info.StorageID, info.IsAvailable, info.StorageName, info.StorageTag);
            }
        }

        public static void PrintReservedInfoMessage(ReservedInfoMessage msg, object tag)
        {
            Console.WriteLine("ReservedInfoMessage:");
            Console.WriteLine("\tReservedName=[{0}]",string.Join(",",msg.ReservedName)); //System.Collections.Generic.ICollection`1[System.String]
            Console.WriteLine("\tReservedTitle=[{0}]",string.Join(",",msg.ReservedTitle)); //System.Collections.Generic.ICollection`1[System.Int32]
        }

        public static void PrintPvpGameHostedMessage(PvpGameHostedMessage msg, object tag)
        {
            Console.WriteLine("PvpGameHostedMessage:");
            Console.WriteLine("\tGameInfo={0}", msg.GameInfo); //ServiceCore.EndPointNetwork.PvpGameInfo has a toString()
            Console.WriteLine("\tHostInfo={0}", msg.HostInfo); //ServiceCore.EndPointNetwork.GameJoinMemberInfo has a toString()
            Console.WriteLine("\tTeamID={0}", msg.TeamID); //System.Int32 has a toString()
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            Console.WriteLine(DictToString(msg.Config, "Config", 1)); //System.Collections.Generic.Dictionary`2[System.String,System.String]
        }

        public static void PrintRepairItemMessage(RepairItemMessage msg, object tag)
        {
            Console.WriteLine("RepairItemMessage:");
            Console.WriteLine("\tItemIDs=[{0}]",String.Join(",",msg.ItemIDs)); //System.Collections.Generic.ICollection`1[System.Int64]
            Console.WriteLine("\tAddAllEquippedItems={0}", msg.AddAllEquippedItems); //System.Boolean has a toString()
            Console.WriteLine("\tAddAllBrokenItems={0}", msg.AddAllBrokenItems); //System.Boolean has a toString()
            Console.WriteLine("\tAddAllRepairableItems={0}", msg.AddAllRepairableItems); //System.Boolean has a toString()
            Console.WriteLine("\tPrice={0}", msg.Price); //System.Int32 has a toString()
        }

        public static void PrintRegisterNewRecipesMessage(RegisterNewRecipesMessage msg, object tag)
        {
            Console.WriteLine(DictToString<string, long>(msg.RecipeList, "RegisterNewRecipesMessage", 0));
        }

        public static void PrintNewRecipesMessage(NewRecipesMessage msg, object tag)
        {
            Console.WriteLine(DictToString<string, long>(msg.RecipeList, "NewRecipesMessage", 0));
        }

        public static void PrintGoddessProtectionMessage(GoddessProtectionMessage msg, object tag)
        {
            Console.WriteLine("GoddessProtectionMessage:");
            Console.WriteLine("\tCaster={0}", msg.Caster); //System.Int32 has a toString()
            Console.WriteLine("\tRevived={0}",String.Join(",",msg.Revived)); //System.Collections.Generic.List`1[System.Int32]
        }

        public static void PrintRewardMissionSuccessMessage(RewardMissionSuccessMessage msg, object tag)
        {
            Console.WriteLine("RewardMissionSuccessMessage:");
            Console.WriteLine("\tID={0}", msg.ID); //System.Int64 has a toString()
            Console.WriteLine("\tRewardAP={0}", msg.RewardAP); //System.Int32 has a toString()
            Console.WriteLine("\tRewardEXP={0}", msg.RewardEXP); //System.Int32 has a toString()
            Console.WriteLine("\tRewardGold={0}", msg.RewardGold); //System.Int32 has a toString()
            Console.WriteLine("\tRewardItemIDs=[{0}]",string.Join(",",msg.RewardItemIDs)); //System.Collections.Generic.List`1[System.String]
            Console.WriteLine("\tRewardItemNums=[{0}]",string.Join(",",msg.RewardItemNums)); //System.Collections.Generic.List`1[System.Int32]
        }

        public static void PrintSharedInventoryInfoMessage(SharedInventoryInfoMessage msg, object tag)
        {
            Console.WriteLine("SharedInventoryInfoMessage:");
            if (msg.StorageInfos != null && msg.StorageInfos.Count != 0)
            {
                Console.WriteLine("\tStorageInfos:");
                foreach (StorageInfo info in msg.StorageInfos)
                {
                    Console.WriteLine("\t\tstorageID={0} isAvailable={1} storageName={2} storageTag={3}", info.StorageID, info.IsAvailable, info.StorageName, info.StorageTag);
                }
            }
            if (msg.SlotInfos != null && msg.SlotInfos.Count != 0)
            {
                int i = 0;
                foreach (SlotInfo info in msg.SlotInfos)
                {
                    Console.WriteLine(SlotInfoToString(info, String.Format("SlotInfos[{0}]", i++), 1));
                }
            }
        }

        public static void PrintTirCoinInfoMessage(TirCoinInfoMessage msg, object tag)
        {
			if(msg.TirCoinInfo == null || msg.TirCoinInfo.Count == 0) {
				Console.WriteLine("TirCoinInfoMessage: []");
			}
            else if (msg.TirCoinInfo.ContainsKey(1))
            {
                Console.WriteLine("TirCoinInfoMessage: Quantity={0}", msg.TirCoinInfo[1]);
            }
            else
            {
                Console.WriteLine(DictToString<byte, int>(msg.TirCoinInfo, "TirCoinInfoMessage", 0));
            }
        }

        public static void PrintRankAlarmInfoMessage(RankAlarmInfoMessage msg, object tag)
        {
            Console.WriteLine(ListToString<RankAlarmInfo>(msg.RankAlarm, "RankAlarmInfoMessage", 0));
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
            Console.WriteLine("UpdateBattleInventoryMessage:");
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            Console.WriteLine("\tUID={0}", msg.UID); //System.Int32 has a toString()
            Console.WriteLine(BattleInventoryToString(msg.BattleInventory, "BattleInventory", 1)); //ServiceCore.MicroPlayServiceOperations.BattleInventory
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
            Console.WriteLine(BattleInventoryToString(msg.BattleInventory, "UpdateBattleInventoryInTownMessage", 0));
        }

        public static void PrintBingoBoardResultMessage(BingoBoardResultMessage msg, object tag)
        {
            Console.WriteLine("BingoBoardResultMessage:");
            Console.WriteLine("\tResult={0}", ((BingoBoardResultMessage.Bingo_Result)msg.Result).ToString());
            Console.WriteLine("\tBingoBoardNumbers=[{0}]", String.Join(",", msg.BingoBoardNumbers));
        }

        public static void PrintJoinHousingMessage(JoinHousingMessage msg, object tag)
        {
            Console.WriteLine("JoinHousingMessage: [TargetID={0}]", msg.TargetID);
        }

        public static void PrintAttendanceInfoMessage(AttendanceInfoMessage msg, object tag)
        {
            //TODO: db connect
            Console.WriteLine("AttendanceInfoMessage:");
            Console.WriteLine("\tEventType={0}", msg.EventType);
            Console.WriteLine("\tCurrentVersion={0}", msg.CurrentVersion);
            Console.WriteLine("\tPeriodText={0}", msg.PeriodText);
            if (msg.AttendanceInfo != null && msg.AttendanceInfo.Count != 0)
            {
                Console.WriteLine("\tAttendanceInfo:");
                foreach (AttendanceDayInfo info in msg.AttendanceInfo)
                {
                    Console.WriteLine("\t\tday={0} isCompleted={1} completedRewardsIndex={2}", info.day, info.isCompleted, info.completedRewardIndex);
                }
            }

            if (msg.BonusRewardInfo != null && msg.BonusRewardInfo.Count != 0)
            {
                Console.WriteLine("\tBonusRewardInfo:");
                foreach (AttendanceDayInfo info in msg.BonusRewardInfo)
                {
                    Console.WriteLine("\t\tday={0} isCompleted={1} completedRewardsIndex={2}", info.day, info.isCompleted, info.completedRewardIndex);
                }
            }

        }
        public static void PrintUpdateTitleMessage(UpdateTitleMessage msg, object tag)
        {
            Console.WriteLine(ListToString<TitleSlotInfo>(msg.Titles, "UpdateTitleMessage", 0));
        }

        public static void PrintGuildInventoryInfoMessage(GuildInventoryInfoMessage msg, object tag)
        {
            Console.WriteLine("GuildInventoryInfoMessage:");
            Console.WriteLine("\tIsEnabled={0}", msg.IsEnabled);
            Console.WriteLine("\tStorageCount={0}", msg.StorageCount);
            Console.WriteLine("\tGoldLimit={0}", msg.GoldLimit);
            Console.WriteLine("\tAccessLimtiTag={0}", msg.AccessLimtiTag);
            int i = 0;
            foreach (SlotInfo slot in msg.SlotInfos)
            {
                Console.WriteLine(SlotInfoToString(slot, String.Format("SlotInfo[{0}]", i++), 1));
            }
        }

        public static void PrintCashshopTirCoinResultMessage(CashshopTirCoinResultMessage msg, object tag)
        {
            Console.WriteLine("CashshopTirCoinResultMessage:");
            Console.WriteLine("\tIsSuccess={0}", msg.isSuccess);
            Console.WriteLine("\tisBeautyShop={0}", msg.isBeautyShop);
            Console.WriteLine("\tsuccessCount={0}", msg.successCount);
            Console.WriteLine("\tIgnoreItems:");
            foreach (TirCoinIgnoreItemInfo item in msg.IgnoreItems)
            {
                Console.WriteLine("\t\tItemClass={0} Amount={1} Duration={2} Price={3}", item.ItemClass, item.Amount, item.Duration, item.Price);
            }
        }

        public static void PrintGiveCashShopDiscountCouponMessage(GiveCashShopDiscountCouponMessage msg, object tag)
        {
            Console.WriteLine("GiveCashShopDiscountCouponMessage: CouponCode={0}", msg.CouponCode);
        }

        public static void PrintNextSectorMessage(NextSectorMessage msg, object tag)
        {
            Console.WriteLine("NextSectorMessage: OnSectorStart={0}", msg.OnSectorStart);
        }

        public static void PrintBurnGauge(BurnGauge msg, object tag)
        {
            //TODO: add db connect
            Console.WriteLine("BurnGauge:");
            Console.WriteLine("\tGauge={0}", msg.Gauge);
            Console.WriteLine("\tJackpotStartGauge={0}", msg.JackpotStartGauge);
            Console.WriteLine("\tJackpotMaxGauge={0}", msg.JackpotMaxGauge);
        }

        public static void PrintStoryLinesMessage(StoryLinesMessage msg, object tag)
        {
            Console.WriteLine("StoryLinesMessage:");
            foreach (BriefStoryLineInfo info in msg.StoryStatus)
            {
                Console.WriteLine("\tStoryLine={0} Phase={1} Status={2} PhaseText={3}", info.StoryLine, info.Phase, info.Status, info.PhaseText);
            }
        }

        public static void PrintQuickSlotInfoMessage(QuickSlotInfoMessage msg, object tag)
        {
            QuickSlotInfo info = GetPrivateProperty<QuickSlotInfo>(msg, "info");
            Console.WriteLine(QuickSlotInfoToString(info, "QuickSlotInfoMessage", 0));
        }

        public static void PrintGuildInfoMessage(GuildInfoMessage msg, object tag)
        {
            Console.WriteLine("GuildInfoMessage:");
            if (msg.GuildInfo != null) {
                InGameGuildInfo g = msg.GuildInfo;
                Console.WriteLine("\tGuildSN={0}", g.GuildSN); //System.Int32 has a toString()
                Console.WriteLine("\tGuildName={0}", g.GuildName); //System.String has a toString()
                Console.WriteLine("\tGuildLevel={0}", g.GuildLevel); //System.Int32 has a toString()
                Console.WriteLine("\tMemberCount={0}", g.MemberCount); //System.Int32 has a toString()
                Console.WriteLine("\tMasterName={0}", g.MasterName); //System.String has a toString()
                Console.WriteLine("\tMaxMemberCount={0}", g.MaxMemberCount); //System.Int32 has a toString()
                Console.WriteLine("\tIsNewbieRecommend={0}", g.IsNewbieRecommend); //System.Boolean has a toString()
                Console.WriteLine("\tGuildPoint={0}", g.GuildPoint); //System.Int64 has a toString()
                Console.WriteLine("\tGuildNotice={0}", g.GuildNotice); //System.String has a toString()
                Console.WriteLine(DictToString<byte, int>(g.DailyGainGP, "DailyGainGP", 1)); //System.Collections.Generic.Dictionary`2[System.Byte,System.Int32]
            }
            //TODO: db connect
        }

        public static void PrintNotifyLook(NotifyLook msg, object tag)
        {
            //TODO: db connect
            Console.WriteLine("NotifyLook:");
            Console.WriteLine("\tID={0}", msg.ID);
            Console.WriteLine(CharacterSummaryToString(msg.Look, "Look", 1));
        }

        public static void PrintQueryCashShopProductListMessage(QueryCashShopProductListMessage msg, object tag)
        {
            Console.WriteLine("QueryCashShopProductListMessage: []");
        }

        public static void PrintQueryCashShopBalanceMessage(QueryCashShopBalanceMessage msg, object tag)
        {
            Console.WriteLine("QueryCashShopBalanceMessage: []");
        }

        public static void PrintSecondPasswordMessage(SecondPasswordMessage msg, object tag)
        {
            //TODO: hide
            Console.WriteLine("SecondPasswordMessage: Password={0}", msg.Password);
        }

        private static CharacterSummary character = null;

        public static void PrintSelectCharacterMessage(SelectCharacterMessage msg, object tag)
        {
            character = characters[msg.Index];
            Console.WriteLine("SelectCharacterMessage: index={0}",msg.Index);
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
            Console.WriteLine(GameInfoToString(msg.TheInfo, "RegisterServerMessage", 0));
        }
        public static void PrintQueryQuestProgressMessage(QueryQuestProgressMessage msg, object tag)
        {
            Console.WriteLine("QueryQuestProgressMessage: []");
        }

        public static void PrintHousingHostInfoMessage(HousingHostInfoMessage msg, object tag)
        {
            Console.WriteLine("HousingHostInfoMessage:");
            Console.WriteLine(GameInfoToString(msg.GameInfo, "GameInfo", 1)); //ServiceCore.EndPointNetwork.GameInfo has a toString()
            Console.WriteLine("\tMemberInfo:");
            Console.WriteLine(GameJoinMemberInfoToString(msg.MemberInfo, 2)); //ServiceCore.EndPointNetwork.GameJoinMemberInfo has a toString()
        }


        public static void PrintRequestGemstoneRollbackMessage(RequestGemstoneRollbackMessage msg, object tag)
        {
            Console.WriteLine("RequestGemstoneRollbackMessage:");
            Console.WriteLine("\tBraceletItemID={0}", msg.BraceletItemID); //System.Int64 has a toString()
        }

        public static void PrintRequestMarbleInfoMessage(RequestMarbleInfoMessage msg, object tag)
        {
            Console.WriteLine("RequestMarbleInfoMessage:");
        }

        public static void PrintMarbleCastDiceResultMessage(MarbleCastDiceResultMessage msg, object tag)
        {
            Console.WriteLine("MarbleCastDiceResultMessage:");
            Console.WriteLine("\tFaceNumber={0}", msg.FaceNumber); //System.Int32 has a toString()
            Console.WriteLine("\tNextNodeIndex={0}", msg.NextNodeIndex); //System.Int32 has a toString()
            Console.WriteLine("\tIsChance={0}", msg.IsChance); //System.Boolean has a toString()
        }

        public static void PrintRequestMarbleProcessChanceRoadMessage(RequestMarbleProcessChanceRoadMessage msg, object tag)
        {
            Console.WriteLine("RequestMarbleProcessChanceRoadMessage:");
        }

        public static void PrintMarbleResultMessage(MarbleResultMessage msg, object tag)
        {
            Console.WriteLine("MarbleResultMessage:");
            Console.WriteLine("\tResultType={0}", msg.ResultType); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.String has a toString()
        }

        public static void PrintMarbleGetItemMessage(MarbleGetItemMessage msg, object tag)
        {
            Console.WriteLine("MarbleGetItemMessage:");
            Console.WriteLine("\tItemClassEx={0}", msg.ItemClassEx); //System.String has a toString()
            Console.WriteLine("\tItemCount={0}", msg.ItemCount); //System.Int32 has a toString()
            Console.WriteLine("\tType={0}", msg.Type); //System.String has a toString()
        }

        public static void PrintInsertBlessStoneMessage(InsertBlessStoneMessage msg, object tag)
        {
            Console.WriteLine("InsertBlessStoneMessage:");
            Console.WriteLine("\tStoneType={0}", msg.StoneType); //ServiceCore.EndPointNetwork.BlessStoneType
            Console.WriteLine("\tIsInsert={0}", msg.IsInsert); //System.Boolean has a toString()
            Console.WriteLine("\tRemainFatigue={0}", msg.RemainFatigue); //System.Int32 has a toString()
        }

        public static void PrintAddSharedItemMessage(AddSharedItemMessage msg, object tag)
        {
            Console.WriteLine("AddSharedItemMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tAmount={0}", msg.Amount); //System.Int32 has a toString()
            Console.WriteLine("\tTargetTab={0}", msg.TargetTab); //System.Byte has a toString()
            Console.WriteLine("\tTargetSlot={0}", msg.TargetSlot); //System.Int32 has a toString()
        }

        public static void PrintAllUserGoalRewardMessage(AllUserGoalRewardMessage msg, object tag)
        {
            Console.WriteLine("AllUserGoalRewardMessage:");
            Console.WriteLine("\tGoalID={0}", msg.GoalID); //System.Int32 has a toString()
        }

        public static void PrintAltarStatusEffectMessage(AltarStatusEffectMessage msg, object tag)
        {
            Console.WriteLine("AltarStatusEffectMessage:");
            Console.WriteLine("\tType={0}", msg.Type); //System.Int32 has a toString()
        }

        public static void PrintAvatarSynthesisResultMessage(AvatarSynthesisResultMessage msg, object tag)
        {
            Console.WriteLine("AvatarSynthesisResultMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tResultFlag={0}", msg.ResultFlag); //System.Int32 has a toString()
            Console.WriteLine("\tAttribute={0}", msg.Attribute); //System.String has a toString()
        }

        public static void PrintCIDByNameMessage(CIDByNameMessage msg, object tag)
        {
            Console.WriteLine("CIDByNameMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tName={0}", msg.Name); //System.String has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
            Console.WriteLine("\tIsEqualAccount={0}", msg.IsEqualAccount); //System.Boolean has a toString()
            Console.WriteLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            Console.WriteLine("\tCharacterClass={0}", msg.CharacterClass); //System.Int32 has a toString()
        }

        public static void PrintCounterEventMessage(CounterEventMessage msg, object tag)
        {
            Console.WriteLine("CounterEventMessage:");
            Console.WriteLine("\tTotalCount={0}", msg.TotalCount); //System.Int32 has a toString()
            Console.WriteLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            Console.WriteLine("\tEventName={0}", msg.EventName); //System.String has a toString()
        }

        public static void PrintDecomposeItemMessage(DecomposeItemMessage msg, object tag)
        {
            Console.WriteLine("DecomposeItemMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
        }

        public static void PrintUseFreeTitleMessage(UseFreeTitleMessage msg, object tag)
        {
            Console.WriteLine("UseFreeTitleMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tFreeTitleName={0}", msg.FreeTitleName); //System.String has a toString()
        }

        public static void PrintGetFriendshipInfoMessage(GetFriendshipInfoMessage msg, object tag)
        {
            Console.WriteLine("GetFriendshipInfoMessage:");
        }

        public static void PrintDeleteFriendshipInfoMessage(DeleteFriendshipInfoMessage msg, object tag)
        {
            Console.WriteLine("DeleteFriendshipInfoMessage:");
            Console.WriteLine("\tFriendID={0}", msg.FriendID); //System.Int32 has a toString()
        }

        public static void PrintUpdateFriendshipPointMessage(UpdateFriendshipPointMessage msg, object tag)
        {
            Console.WriteLine("UpdateFriendshipPointMessage:");
            Console.WriteLine("\tPoint={0}", msg.Point); //System.Int32 has a toString()
        }

        public static void PrintAddFriendShipMessage(AddFriendShipMessage msg, object tag)
        {
            Console.WriteLine("AddFriendShipMessage:");
            Console.WriteLine("\tCharacterName={0}", msg.CharacterName); //System.String has a toString()
        }

        public static void PrintGatheringMessage(GatheringMessage msg, object tag)
        {
            Console.WriteLine("GatheringMessage:");
            Console.WriteLine("\tEntityName={0}", msg.EntityName); //System.String has a toString()
            Console.WriteLine("\tGatherTag={0}", msg.GatherTag); //System.Int32 has a toString()
        }

        public static void PrintGameResourceRequestMessage(GameResourceRequestMessage msg, object tag)
        {
            Console.WriteLine("GameResourceRequestMessage:");
            Console.WriteLine("\tRequestType={0}", msg.RequestType); //System.Int32 has a toString()
            Console.WriteLine("\tRequestParam={0}", msg.RequestParam); //System.String has a toString()
        }

        public static void PrintDirectPurchaseGuildItemMessage(DirectPurchaseGuildItemMessage msg, object tag)
        {
            Console.WriteLine("DirectPurchaseGuildItemMessage:");
            Console.WriteLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
            Console.WriteLine("\tIsCredit={0}", msg.IsCredit); //System.Boolean has a toString()
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
        }

        public static void PrintDirectPurchaseGuildItemResultMessage(DirectPurchaseGuildItemResultMessage msg, object tag)
        {
            Console.WriteLine("DirectPurchaseGuildItemResultMessage:");
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
        }

        public static void PrintFindNewbieRecommendGuildResultMessage(FindNewbieRecommendGuildResultMessage msg, object tag)
        {
            Console.WriteLine("FindNewbieRecommendGuildResultMessage:");
            Console.WriteLine("\tinfo={0}", msg.info); //ServiceCore.EndPointNetwork.GuildService.InGameGuildInfo has a toString()
        }

        public static void PrintGuildChatMemberInfoMessage(GuildChatMemberInfoMessage msg, object tag)
        {
            Console.WriteLine("GuildChatMemberInfoMessage:");
            Console.WriteLine("\tSender={0}", msg.Sender); //System.String has a toString()
            Console.WriteLine("\tIsOnline={0}", msg.IsOnline); //System.Boolean has a toString()
        }

        public static void PrintAddGuildStorageItemMessage(AddGuildStorageItemMessage msg, object tag)
        {
            Console.WriteLine("AddGuildStorageItemMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            Console.WriteLine("\tAmount={0}", msg.Amount); //System.Int32 has a toString()
            Console.WriteLine("\tTargetTab={0}", msg.TargetTab); //System.Byte has a toString()
            Console.WriteLine("\tTargetSlot={0}", msg.TargetSlot); //System.Int32 has a toString()
        }

        public static void PrintPickGuildStorageItemMessage(PickGuildStorageItemMessage msg, object tag)
        {
            Console.WriteLine("PickGuildStorageItemMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            Console.WriteLine("\tAmount={0}", msg.Amount); //System.Int32 has a toString()
            Console.WriteLine("\tTargetTab={0}", msg.TargetTab); //System.Byte has a toString()
            Console.WriteLine("\tTargetSlot={0}", msg.TargetSlot); //System.Int32 has a toString()
        }

        public static void PrintSetFacebookAccessTokenMessage(SetFacebookAccessTokenMessage msg, object tag)
        {
            Console.WriteLine("SetFacebookAccessTokenMessage:");
            Console.WriteLine("\tAccessToken={0}", msg.AccessToken); //System.String has a toString()
        }

        public static void PrintNoticeNewbieRecommendMessage(NoticeNewbieRecommendMessage msg, object tag)
        {
            Console.WriteLine("NoticeNewbieRecommendMessage:");
            Console.WriteLine("\tRequestUserName={0}", msg.RequestUserName); //System.String has a toString()
            Console.WriteLine("\tShipID={0}", msg.ShipID); //System.Int64 has a toString()
        }

        public static void PrintRequestNewbieRecommendGuildMessage(RequestNewbieRecommendGuildMessage msg, object tag)
        {
            Console.WriteLine("RequestNewbieRecommendGuildMessage:");
            Console.WriteLine("\tGuildSN={0}", msg.GuildSN); //System.Int32 has a toString()
        }

        public static void PrintQueryHousingListMessage(QueryHousingListMessage msg, object tag)
        {
            Console.WriteLine("QueryHousingListMessage:");
        }

        public static void PrintQueryHousingPropsMessage(QueryHousingPropsMessage msg, object tag)
        {
            Console.WriteLine("QueryHousingPropsMessage:");
        }

        public static void PrintPurchaseGuildStorageMessage(PurchaseGuildStorageMessage msg, object tag)
        {
            Console.WriteLine("PurchaseGuildStorageMessage:");
            Console.WriteLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
            Console.WriteLine("\tIsCredit={0}", msg.IsCredit); //System.Boolean has a toString()
        }

        public static void PrintArrangeGuildStorageItemMessage(ArrangeGuildStorageItemMessage msg, object tag)
        {
            Console.WriteLine("ArrangeGuildStorageItemMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tSlot={0}", msg.Slot); //System.Int32 has a toString()
        }

        public static void PrintHandleGuildStorageSessionMessage(HandleGuildStorageSessionMessage msg, object tag)
        {
            Console.WriteLine("HandleGuildStorageSessionMessage:");
            Console.WriteLine("\tIsStarted={0}", msg.IsStarted); //System.Boolean has a toString()
        }

        public static void PrintUpdateGuildStorageSettingMessage(UpdateGuildStorageSettingMessage msg, object tag)
        {
            Console.WriteLine("UpdateGuildStorageSettingMessage:");
            Console.WriteLine("\tGoldLimit={0}", msg.GoldLimit); //System.Int32 has a toString()
            Console.WriteLine("\tAccessLimtiTag={0}", msg.AccessLimtiTag); //System.Int64 has a toString()
        }

        public static void PrintNotifyEnhanceMessage(NotifyEnhanceMessage msg, object tag)
        {
            Console.WriteLine("NotifyEnhanceMessage:");
            Console.WriteLine("\tcharacterName={0}", msg.characterName); //System.String has a toString()
            Console.WriteLine("\tisSuccess={0}", msg.isSuccess); //System.Boolean has a toString()
            Console.WriteLine("\tnextEnhanceLevel={0}", msg.nextEnhanceLevel); //System.Int32 has a toString()
            Console.WriteLine("\titem={0}", msg.item); //ServiceCore.EndPointNetwork.TooltipItemInfo has a toString()
        }

        public static void PrintItemCombinationResultMessage(ItemCombinationResultMessage msg, object tag)
        {
            Console.WriteLine("ItemCombinationResultMessage:");
            Console.WriteLine("\tResultCode={0}", msg.ResultCode); //System.Int32 has a toString()
            Console.WriteLine("\tResultMessage={0}", msg.ResultMessage); //System.String has a toString()
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            Console.WriteLine("\tColor1={0}", msg.Color1); //System.Int32 has a toString()
            Console.WriteLine("\tColor2={0}", msg.Color2); //System.Int32 has a toString()
            Console.WriteLine("\tColor3={0}", msg.Color3); //System.Int32 has a toString()
        }

        public static void PrintHotSpringRequestInfoResultChannelMessage(HotSpringRequestInfoResultChannelMessage msg, object tag)
        {
            Console.WriteLine("HotSpringRequestInfoResultChannelMessage:");
            Console.WriteLine(ListToString<HotSpringPotionEffectInfo>(msg.HotSpringPotionEffectInfos, "HotSpringPotionEffectInfos",1)); //System.Collections.Generic.ICollection`1[ServiceCore.EndPointNetwork.HotSpringPotionEffectInfo]
            Console.WriteLine("\tTownID={0}", msg.TownID); //System.Int32 has a toString()
        }

        public static void PrintUpdateFlexibleMapLoadingInfoMessage(UpdateFlexibleMapLoadingInfoMessage msg, object tag)
        {
            byte mapState = GetPrivateProperty<byte>(msg, "mapState");
            string regionName = GetPrivateProperty<string>(msg, "regionName");
            Console.WriteLine("UpdateFlexibleMapLoadingInfoMessage:");
            Console.WriteLine("\tMapState={0}",mapState);
            Console.WriteLine("\tregionName={0}",regionName);
        }

        public static void PrintHotSpringPotionEffectInfo(HotSpringPotionEffectInfo msg, object tag)
        {
            Console.WriteLine("HotSpringPotionEffectInfo:");
            Console.WriteLine("\tPotionItemClass={0}", msg.PotionItemClass); //System.String has a toString()
            Console.WriteLine("\tCharacterName={0}", msg.CharacterName); //System.String has a toString()
            Console.WriteLine("\tGuildName={0}", msg.GuildName); //System.String has a toString()
            Console.WriteLine("\tExpiredTime={0}", msg.ExpiredTime); //System.Int32 has a toString()
            Console.WriteLine("\tOtherPotionUsableTime={0}", msg.OtherPotionUsableTime); //System.Int32 has a toString()
        }

        public static void PrintChangedCashShopMessage(ChangedCashShopMessage msg, object tag)
        {
            Console.WriteLine("ChangedCashShopMessage: []");
        }

        public static void PrintNotifyBurnMessage(NotifyBurnMessage msg, object tag)
        {
            Console.WriteLine("NotifyBurnMessage:");
            Console.WriteLine("\tMessage={0}", msg.Message); //ServiceCore.EndPointNetwork.HeroesString has a toString()
            Console.WriteLine("\tIsTelepathyEnable={0}", msg.IsTelepathyEnable); //System.Boolean has a toString()
            Console.WriteLine("\tIsUIEnable={0}", msg.IsUIEnable); //System.Boolean has a toString()
        }

        public static void PrintNotifyRandomItemMessage(NotifyRandomItemMessage msg, object tag)
        {
            Console.WriteLine("NotifyRandomItemMessage:");
            Console.WriteLine("\tMessage={0}", msg.Message); //ServiceCore.EndPointNetwork.HeroesString has a toString()
            Console.WriteLine("\tIsTelepathyEnable={0}", msg.IsTelepathyEnable); //System.Boolean has a toString()
            Console.WriteLine("\tIsUIEnable={0}", msg.IsUIEnable); //System.Boolean has a toString()
        }

        public static void PrintNGSecurityMessage(NGSecurityMessage msg, object tag)
        {
            Console.WriteLine("NGSecurityMessage:");
            Console.WriteLine("\tmessage={0}",BitConverter.ToString(msg.message)); //System.Byte[]
            Console.WriteLine("\tcheckSum={0}", msg.checkSum); //System.UInt64 has a toString()
        }

        public static void PrintQueryInventoryMessage(QueryInventoryMessage msg, object tag)
        {
            Console.WriteLine("QueryInventoryMessage: []");
        }

        public static void PrintReturnToTownMessage(ReturnToTownMessage msg, object tag)
        {
            Console.WriteLine("ReturnToTownMessage: []");
        }

        public static void PrintLeavePartyMessage(LeavePartyMessage msg, object tag)
        {
            Console.WriteLine("LeavePartyMessage: []");
        }

        public static void PrintFindNewbieRecommendGuildMessage(FindNewbieRecommendGuildMessage msg, object tag)
        {
            Console.WriteLine("FindNewbieRecommendGuildMessage: []");
        }

        public static void PrintPropBrokenMessage(PropBrokenMessage msg, object tag)
        {
            Console.WriteLine("PropBrokenMessage:");
            Console.WriteLine("\tBrokenProp={0}",msg.BrokenProp);
            Console.WriteLine("\tEntityName={0}",msg.EntityName);
            Console.WriteLine("\tAttacker={0}",msg.Attacker);
        }

        public static void PrintSectorPropListMessage(SectorPropListMessage msg, object tag)
        {
            if (msg == null || msg.Props.Count == 0)
            {
                Console.WriteLine("SectorPropListMessage: []");
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
            Console.WriteLine(sb.ToString());
        }

        public static void PrintMoveToNextSectorMessage(MoveToNextSectorMessage msg, object tag)
        {
            Console.WriteLine("MoveToNextSectorMessage:");
            Console.WriteLine("\tTriggerName={0}", msg.TriggerName);
            Console.WriteLine("\tTargetGroup={0}", msg.TargetGroup);
            Console.WriteLine("\tHolyProps=[{0}]", String.Join(",", msg.HolyProps));
        }
        public static void PrintStartGameMessage(StartGameMessage msg, object tag)
        {
            Console.WriteLine("StartGameMessage: []");
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
            Console.WriteLine("AcceptQuestMessage:");
            Console.WriteLine("\tQuestID={0}", msg.QuestID);
            Console.WriteLine("\tTitle={0}", msg.Title);
            Console.WriteLine("\tSwearID={0}", msg.SwearID);
            Console.WriteLine(ShipOptionInfoToString(msg.Option, 1));
        }

        public static void PrintQueryRecommendShipMessage(QueryRecommendShipMessage msg, object tag)
        {
            Console.WriteLine("QueryRecommendShipMessage:");
            if (msg.Restriction == null) {
                return;
            }
            RecommendShipRestriction r = msg.Restriction;
            Console.WriteLine("\tQuestSet={0}",r.QuestSet);
            Console.WriteLine("\tTargetQuestID={0}",r.TargetQuestID);
            Console.WriteLine("\tDifficulty={0}",r.Difficulty);
            Console.WriteLine("\tIsSeason2={0}",r.IsSeason2);
            Console.WriteLine("\tSelectedBossQuestIDInfos=[{0}]",String.Join(",",r.SelectedBossQuestIDInfos));
        }

        public static void PrintEquipItemMessage(EquipItemMessage msg, object tag)
        {
            Console.WriteLine("EquipItemMessage:");
            Console.WriteLine("\tItemID={0}",msg.ItemID);
            Console.WriteLine("\tPartID={0}",msg.PartID);
        }

        public static void PrintEquipBundleMessage(EquipBundleMessage msg, object tag)
        {
            Console.WriteLine("EquipBundleMessage:");
            Console.WriteLine("\tItemClass={0}",msg.ItemClass);
            Console.WriteLine("\tQuickSlotID={0}",msg.QuickSlotID);
        }

        public static void PrintMoveInventoryItemMessage(MoveInventoryItemMessage msg, object tag)
        {
            Console.WriteLine("MoveInventoryItemMessage:");
            Console.WriteLine("\tItemID={0}",msg.ItemID);
            Console.WriteLine("\tStorage={0}",msg.Storage);
            Console.WriteLine("\tTarget={0}",msg.Target);
        }

        private static BeautyShopCustomizeMessage lastBeautyShopMsg = null;

        public static void PrintBeautyShopCustomizeMessage(BeautyShopCustomizeMessage msg, object tag)
        {
            Console.WriteLine("BeautyShopCustomizeMessage:");
            Console.WriteLine("CustomizeItems:");
            foreach (CustomizeItemRequestInfo info in msg.CustomizeItems)
            {
                String color = String.Format("({0},{1},{2})", IntToRGB(info.Color1), IntToRGB(info.Color2), IntToRGB(info.Color3));
                Console.WriteLine("\tItemClass={0} Category={1} Color={2} Duration={3} Price={4} CouponItemID={5}", info.ItemClass, info.Category, color, info.Duration, info.Price, info.CouponItemID);
            }
            BeautyShopCustomizeMessage l = lastBeautyShopMsg;
            BeautyShopCustomizeMessage c = msg;
            if (l == null || c.PaintingPosX != l.PaintingPosX)
            {
                Console.WriteLine("\tPaintingPosX={0}", c.PaintingPosX);
            }
            if (l == null || c.PaintingPosX != l.PaintingPosX)
            {
                Console.WriteLine("\tPaintingPosY={0}", c.PaintingPosY);
            }
            if (l == null || c.PaintingSize != l.PaintingSize)
            {
                Console.WriteLine("\tPaintingSize={0}", c.PaintingSize);
            }
            if (l == null || c.PaintingRotation != l.PaintingRotation)
            {
                Console.WriteLine("\tPaintingRotation={0}", c.PaintingRotation);
            }
            if (l == null || c.payment != l.payment)
            {
                Console.WriteLine("\tPaintingRotation={0}", c.payment);
            }
            if (l == null || c.BodyPaintingPosX != l.BodyPaintingPosX)
            {
                Console.WriteLine("\tBodyPaintingPosX={0}", c.BodyPaintingPosX);
            }
            if (l == null || c.BodyPaintingPosX != l.BodyPaintingPosX)
            {
                Console.WriteLine("\tBodyPaintingPosY={0}", c.BodyPaintingPosY);
            }
            if (l == null || c.BodyPaintingSize != l.BodyPaintingSize)
            {
                Console.WriteLine("\tBodyPaintingSize={0}", c.BodyPaintingSize);
            }
            if (l == null || c.BodyPaintingRotation != l.BodyPaintingRotation)
            {
                Console.WriteLine("\tBodyPaintingRotation={0}", c.BodyPaintingRotation);
            }
            if (l == null || c.BodyPaintingSide != l.BodyPaintingSide)
            {
                Console.WriteLine("\tBodyPaintingSide={0}", c.BodyPaintingSide);
            }
            if (l == null || c.BodyPaintingClip != l.BodyPaintingClip)
            {
                Console.WriteLine("\tBodyPaintingClip={0}", c.BodyPaintingClip);
            }
            if (l == null || c.BodyPaintingMode != l.BodyPaintingMode)
            {
                Console.WriteLine("\tBodyPaintingMode={0}", c.BodyPaintingMode);
            }
            if (l == null || c.HeightValue != l.HeightValue)
            {
                Console.WriteLine("\tHeight={0}", c.HeightValue);
            }
            if (l == null || c.BustValue != l.BustValue)
            {
                Console.WriteLine("\tBust={0}", c.BustValue);
            }
            if (l == null || c.ShinenessValue != l.ShinenessValue)
            {
                Console.WriteLine("\tShineness={0}", c.ShinenessValue);
            }
            if (l == null || c.SkinColor != l.SkinColor)
            {
                Console.WriteLine("\tSkinColor={0}", IntToRGB(c.SkinColor));
            }
            if (l == null || c.EyeColor != l.EyeColor)
            {
                Console.WriteLine("\tEyeColor={0}", IntToRGB(c.EyeColor));
            }
            if (l == null || c.EyebrowItemClass != l.EyebrowItemClass)
            {
                Console.WriteLine("\tEyebrowItemClass={0}", c.EyebrowItemClass);
            }
            if (l == null || c.EyebrowColor != l.EyebrowColor)
            {
                Console.WriteLine("\tEyebrowColor={0}", IntToRGB(c.EyebrowColor));
            }
            if (l == null || c.LookChangeItemClass != l.LookChangeItemClass)
            {
                Console.WriteLine("\tLookChangeItemClass={0}", c.LookChangeItemClass);
            }

            if (l == null || c.LookChangeDuration != l.LookChangeDuration)
            {
                Console.WriteLine("\tLookChangeDuration={0}", c.LookChangeDuration);
            }
            if (l == null || c.LookChangePrice != l.LookChangePrice)
            {
                Console.WriteLine("\tLookChangePrice={0}", c.LookChangePrice);
            }
            Console.WriteLine(BodyShapeInfoToString(c.BodyShapeInfo, 1, l.BodyShapeInfo));
            lastBeautyShopMsg = msg;
        }

        public static void PrintHideCostumeMessage(HideCostumeMessage msg, object tag)
        {
            Console.WriteLine("HideCostumeMessage: HideHead={0} AvatarPart={1} AvatarState={2}", msg.HideHead, msg.AvatarPart, msg.AvatarState);
        }

        public static void PrintUseInventoryItemMessage(UseInventoryItemMessage msg, object tag)
        {
            Console.WriteLine("UseInventoryItemMessage: ItemID={0} TargetItemID={1}", msg.ItemID, msg.TargetItemID);
        }

        public static void PrintSharingStartMessage(SharingStartMessage msg, object tag)
        {
            Console.WriteLine("SharingStartMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tTargetsCID=[{0}]",String.Join(",",msg.TargetsCID)); //System.Collections.Generic.List`1[System.Int64]
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
            Console.WriteLine("RankOtherCharacterInfoMessage:");
            Console.WriteLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            Console.WriteLine("\tBaseCharacter={0}", msg.BaseCharacter); //System.Int32 has a toString()
            Console.WriteLine("\tCharacterName={0}", msg.CharacterName); //System.String has a toString()
            if (msg.RankResult != null && msg.RankResult.Count != 0)
            {
                Console.WriteLine("\tRankResult:");
                foreach (RankResultInfo rank in msg.RankResult)
                {
                    Console.WriteLine(RankResultInfoToString(rank, 2));
                }
            }
            if (msg.RequesterRankResult != null && msg.RequesterRankResult.Count != 0)
            {
                Console.WriteLine("\tRequesterRankResult:");
                foreach (RankResultInfo rank in msg.RequesterRankResult)
                {
                    Console.WriteLine(RankResultInfoToString(rank, 2));
                }
            }
            Console.WriteLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintXignCodeSecureDataMessage(XignCodeSecureDataMessage msg, object tag)
        {
            Console.WriteLine("XignCodeSecureDataMessage:");
            Console.WriteLine("\tSecureData={0}",BitConverter.ToString(msg.SecureData)); //System.Byte[]
            Console.WriteLine("\tHackCode={0}", msg.HackCode); //System.Int32 has a toString()
            Console.WriteLine("\tHackParam={0}", msg.HackParam); //System.String has a toString()
            Console.WriteLine("\tCheckSum={0}", msg.CheckSum); //System.UInt64 has a toString()
        }

        public static void PrintRankGoalInfoMessage(RankGoalInfoMessage msg, object tag)
        {
            Console.WriteLine("RankGoalInfoMessage:");
            Console.WriteLine("\tEventID={0}", msg.EventID); //System.Int32 has a toString()
            Console.WriteLine("\tRivalName={0}", msg.RivalName); //System.String has a toString()
            Console.WriteLine("\tRivalScore={0}", msg.RivalScore); //System.Int64 has a toString()
            Console.WriteLine("\tRank={0}", msg.Rank); //System.Int32 has a toString()
            Console.WriteLine("\tRankPrev={0}", msg.RankPrev); //System.Int32 has a toString()
        }

        public static void PrintRankFavoritesInfoMessage(RankFavoritesInfoMessage msg, object tag)
        {
            Console.WriteLine("RankFavoritesInfoMessage:");
            if (msg.RankResult != null && msg.RankResult.Count != 0)
            {
                Console.WriteLine("\tRankResult:");
                foreach (RankResultInfo rank in msg.RankResult)
                {
                    Console.WriteLine(RankResultInfoToString(rank, 2));
                }
            }
            Console.WriteLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintRankCharacterInfoMessage(RankCharacterInfoMessage msg, object tag)
        {
            Console.WriteLine("RankCharacterInfoMessage:");
            if (msg.RankResult != null && msg.RankResult.Count != 0)
            {
                Console.WriteLine("\tRankResult:");
                foreach (RankResultInfo rank in msg.RankResult)
                {
                    Console.WriteLine(RankResultInfoToString(rank, 2));
                }
            }
            Console.WriteLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintRankHeavyFavoritesInfoMessage(RankHeavyFavoritesInfoMessage msg, object tag)
        {
            Console.WriteLine("RankHeavyFavoritesInfoMessage:");
            if (msg.RankResult != null && msg.RankResult.Count != 0)
            {
                Console.WriteLine("\tRankResult:");
                foreach (RankResultInfo rank in msg.RankResult)
                {
                    Console.WriteLine(RankResultInfoToString(rank, 2));
                }
            }
        }

        public static void PrintRankAllInfoMessage(RankAllInfoMessage msg, object tag)
        {
            Console.WriteLine("RankAllInfoMessage:");
            if (msg.RankResult != null && msg.RankResult.Count != 0)
            {
                Console.WriteLine("\tRankResult:");
                foreach (RankResultInfo rank in msg.RankResult)
                {
                    Console.WriteLine(RankResultInfoToString(rank, 2));
                }
            }
            Console.WriteLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintRankInfoMessage(RankInfoMessage msg, object tag)
        {
            Console.WriteLine("RankInfoMessage:");
            Console.WriteLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
            if (msg.RankResult != null && msg.RankResult.Count != 0) {
                Console.WriteLine("\tRankResult:");
                foreach (RankResultInfo rank in msg.RankResult) {
                    Console.WriteLine(RankResultInfoToString(rank,2));
                }
            }
        }

        public static void PrintApplyMicroPlayEffectsMessage(ApplyMicroPlayEffectsMessage msg, object tag)
        {
            Console.WriteLine("ApplyMicroPlayEffectsMessage:");
            Console.WriteLine("\tCaster={0}", msg.Caster); //System.Int32 has a toString()
            Console.WriteLine("\tEffectName={0}", msg.EffectName); //System.String has a toString()
            if (msg.EffectList != null && msg.EffectList.Count != 0) {
                Console.WriteLine("\tEffectList:");
                foreach (MicroPlayEffect effect in msg.EffectList)
                {
                    Console.WriteLine("\t\tMicroPlayEffect: PlayerSlotNo={0} Effect={1} Amount={2} Argument={3}",effect.PlayerSlotNo,effect.Effect,effect.Amount,effect.Argument);
                }
            }
        }

        public static void PrintSendMailMessage(SendMailMessage msg, object tag)
        {
            Console.WriteLine("SendMailMessage:");
            Console.WriteLine("\tMailType={0}", msg.MailType); //System.Byte has a toString()
            Console.WriteLine("\tToName={0}", msg.ToName); //System.String has a toString()
            Console.WriteLine("\tTitle={0}", msg.Title); //System.String has a toString()
            Console.WriteLine("\tContent={0}", msg.Content); //System.String has a toString()
            if (msg.ItemList != null && msg.ItemList.Count != 0) {
                Console.WriteLine("\tItemList:");
                foreach (AttachedTransferItemInfo item in msg.ItemList)
                {
                    Console.WriteLine("\t\tAttachedTransferItemInfo: ItemID={0} ItemClass={1} ItemCount={2}",item.ItemID,item.ItemClass,item.ItemCount);
                }
            }
            Console.WriteLine("\tGold={0}", msg.Gold); //System.Int32 has a toString()
            Console.WriteLine("\tExpress={0}", msg.Express); //System.Boolean has a toString()
            Console.WriteLine("\tChargedGold={0}", msg.ChargedGold); //System.Int32 has a toString()
        }

        public static void PrintMailInfoMessage(MailInfoMessage msg, object tag)
        {
            Console.WriteLine("MailInfoMessage:");
            Console.WriteLine("\tMailID={0}", msg.MailID); //System.Int64 has a toString()
            Console.WriteLine(ListToString<MailItemInfo>(msg.Items, "Items", 1));
            Console.WriteLine("\tMailType={0}", msg.MailType); //System.Byte has a toString()
            Console.WriteLine("\tFromName={0}", msg.FromName); //System.String has a toString()
            Console.WriteLine("\tTitle={0}", msg.Title); //System.String has a toString()
            Console.WriteLine("\tContent={0}", msg.Content); //System.String has a toString()
            Console.WriteLine("\tChargedGold={0}", msg.ChargedGold); //System.Int32 has a toString()
        }

        public static void PrintGetMailItemMessage(GetMailItemMessage msg, object tag)
        {
            Console.WriteLine("GetMailItemMessage: MailID={0}", msg.MailID);
        }

        public static void PrintQueryMailInfoMessage(QueryMailInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryMailInfoMessage: MailID={0}", msg.MailID);
        }

        public static void PrintDyeAmpleRequestMessage(DyeAmpleRequestMessage msg, object tag)
        {
            Console.WriteLine("DyeAmpleRequestMessage:");
            Console.WriteLine("\tX={0}", msg.X); //System.Int32 has a toString()
            Console.WriteLine("\tY={0}", msg.Y); //System.Int32 has a toString()
            Console.WriteLine("\tIsAvatar={0}", msg.IsAvatar); //System.Boolean has a toString()
            if (msg.ColorValue != null && msg.ColorValue.Count != 0) {
                Console.WriteLine("\tColorValue:");
                foreach (int color in msg.ColorValue) {
                    Console.WriteLine("\t\t{0}",IntToRGB(color));
                }
            }
        }

        public static void PrintRarityCoreListMessage(RarityCoreListMessage msg, object tag)
        {
            Console.WriteLine("RarityCoreListMessage:");
            if (msg.RareCores != null && msg.RareCores.Count != 0) {
                foreach (RareCoreInfo r in msg.RareCores) {
                    Console.WriteLine("\tRareCore: PlayerTag={0} CoreEntityName={1} CoreType={2}",r.PlayerTag,r.CoreEntityName,r.CoreType);
                }
            }
        }

        public static void PrintRandomItemRewardListMessage(RandomItemRewardListMessage msg, object tag)
        {
            Console.WriteLine("RandomItemRewardListMessage:");
            if (msg.ItemClasses != null && msg.ItemClasses.Count != 0) {
                Console.WriteLine("\tItemClasses:");
                foreach (ColoredEquipment e in msg.ItemClasses)
                {
                    Console.WriteLine("\t\tColoredEquipment: ItemClass={0} Color1={1} Color2={2} Color3={3}",e.ItemClass,e.Color1,e.Color2,e.Color3);
                }
            }
            
            Console.WriteLine("\tItemQuantities=[{0}]",String.Join(",",msg.ItemQuantities)); //System.Collections.Generic.ICollection`1[System.Int32]
            Console.WriteLine("\tIsUserCare={0}", msg.IsUserCare); //System.Boolean has a toString()
            Console.WriteLine("\tKeyItemClass={0}", msg.KeyItemClass); //System.String has a toString()
            Console.WriteLine("\tDisableShowPopUp={0}", msg.DisableShowPopUp); //System.Boolean has a toString()
        }

        public static void PrintManufactureCraftMessage(ManufactureCraftMessage msg, object tag)
        {
            Console.WriteLine("ManufactureCraftMessage:");
            Console.WriteLine("\tRecipeID={0}", msg.RecipeID); //System.String has a toString()
            Console.WriteLine("\tPartsIDList=[{0}]",string.Join(",",msg.PartsIDList)); //System.Collections.Generic.List`1[System.Int64]
        }

        public static void PrintItemPriceInfoMessage(ItemPriceInfoMessage msg, object tag)
        {
            Console.WriteLine("ItemPriceInfoMessage:");
            foreach (KeyValuePair<string,PriceRange> entry in msg.Prices) {
                PriceRange p = entry.Value;
                Console.WriteLine("\t{0}=PriceRange: Price={1} Min={2} Max={3}",entry.Key,p.Price,p.Min,p.Max);
            }
            //Console.WriteLine("\tPrices={0}",msg.Prices); //System.Collections.Generic.Dictionary`2[System.String,ServiceCore.EndPointNetwork.PriceRange]
        }

        public static void PrintEnhanceItemResultMessage(EnhanceItemResultMessage msg, object tag)
        {
            Console.WriteLine("EnhanceItemResultMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //System.String has a toString()
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tPrevItemClass={0}", msg.PrevItemClass); //System.String has a toString()
            Console.WriteLine("\tEnhancedItemClass={0}", msg.EnhancedItemClass); //System.String has a toString()
            Console.WriteLine(DictToString<string,int>(msg.FailReward,"FailReward",1)); //System.Collections.Generic.Dictionary`2[System.String,System.Int32]
        }

        public static void PrintRespawnMessage(RespawnMessage msg, object tag)
        {
            Console.WriteLine("RespawnMessage:");
            Console.WriteLine("\tSpawnerNames=[{0}]",String.Join(",",msg.SpawnerNames)); //System.Collections.Generic.List`1[System.String]
            Console.WriteLine("\tSectorID={0}", msg.SectorID); //System.Int32 has a toString()
            if (msg.TrialFactorInfos != null && msg.TrialFactorInfos.Count != 0) {
                Console.WriteLine("\tTrialFactorInfos:");
                foreach (TrialFactorInfo trial in msg.TrialFactorInfos) {
                    Console.WriteLine("\t\tGroupNumber={0} TrialMod={1} TrialName={2} SectorGroupID={3}",trial.GroupNumber,trial.TrialMod,trial.TrialName,trial.SectorGroupID);
                }
            }
            Console.WriteLine(DictToString<string,int>(msg.MonsterItemDrop,"MonsterItemDrop",1)); //System.Collections.Generic.Dictionary`2[System.String,System.Int32]
        }

        public static void PrintStoryVariablesMessage(StoryVariablesMessage msg, object tag)
        {
            Console.WriteLine("StoryVariablesMessage:");
            foreach (StoryVariableInfo s in msg.StoryVariables) {
                Console.WriteLine("StoryVariable: StoryLine={0} {1}={2}",s.StoryLine,s.Key,s.Value);
            }
        }

        public static void PrintSetMileagePointMessage(SetMileagePointMessage msg, object tag)
        {
            int MileagePoint = GetPrivateProperty<int>(msg, "MileagePoint");
            Console.WriteLine("SetMileagePointMessage: MileagePoint={0}",MileagePoint);
        }

        public static void PrintSetFeverPointMessage(SetFeverPointMessage msg, object tag)
        {
            int FeverPoint = GetPrivateProperty<int>(msg, "FeverPoint");
            Console.WriteLine("SetFeverPointMessage: FeverPoint={0}",FeverPoint);
        }

        public static void PrintRoulettePickSlotResultMessage(RoulettePickSlotResultMessage msg, object tag)
        {
            Console.WriteLine("RoulettePickSlotResultMessage: PickedSlot={0}",msg.PickedSlot);
        }

        public static void PrintSelectTargetItemMessage(SelectTargetItemMessage msg, object tag)
        {
            Console.WriteLine("SelectTargetItemMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tTargetItems=[{0}]",String.Join(",",msg.TargetItems)); //System.Collections.Generic.List`1[System.Int64]
            Console.WriteLine("\tLocalTextKey={0}", msg.LocalTextKey); //System.String has a toString()
        }

        public static void PrintBraceletCombinationResultMessage(BraceletCombinationResultMessage msg, object tag)
        {
            Console.WriteLine("BraceletCombinationResultMessage:");
            Console.WriteLine("\tBraceletItemID={0}", msg.BraceletItemID); //System.Int64 has a toString()
            Console.WriteLine("\tGemstoneItemID={0}", msg.GemstoneItemID); //System.Int64 has a toString()
            Console.WriteLine("\tResultCode={0}", msg.ResultCode); //System.Int32 has a toString()
            Console.WriteLine("\tResultMessage={0}", msg.ResultMessage); //System.String has a toString()
            Console.WriteLine(DictToString<string,int>(msg.PreviousStat,"PreviousStat",1)); //System.Collections.Generic.Dictionary`2[System.String,System.Int32]
            Console.WriteLine(DictToString<string, int>(msg.newStat, "NewStat", 1)); //System.Collections.Generic.Dictionary`2[System.String,System.Int32]
        }

        public static void PrintBuyIngameCashshopUseTirResultMessage(BuyIngameCashshopUseTirResultMessage msg, object tag)
        {
            Console.WriteLine("BuyIngameCashshopUseTirResultMessage:");
            Console.WriteLine("\tProducts=[{0}]",String.Join(",",msg.Products)); //System.Collections.Generic.List`1[System.Int32]
            Console.WriteLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
        }

        public static void PrintRecommendFriendListMessage(RecommendFriendListMessage msg, object tag)
        {
            Console.WriteLine("RecommendFriendListMessage:");
            Console.WriteLine("\tfriendName={0}", msg.friendName); //System.String has a toString()
            Console.WriteLine("\trecommenderList=[{0}]",String.Join(",",msg.recommenderList)); //System.Collections.Generic.List`1[System.String]
        }

        public static void PrintOpenTreasureBoxResultMessage(OpenTreasureBoxResultMessage msg, object tag)
        {
            Console.WriteLine("OpenTreasureBoxResultMessage:");
            Console.WriteLine("\tGroupID={0}", msg.GroupID); //System.Int32 has a toString()
            Console.WriteLine("\tEntityName={0}", msg.EntityName); //System.String has a toString()
            Console.WriteLine(DictToString<string,int>(msg.MonsterList,"MonsterList",1)); //System.Collections.Generic.Dictionary`2[System.String,System.Int32]
        }

        public static void PrintPvpTeamInfoMessage(PvpTeamInfoMessage msg, object tag)
        {
            Console.WriteLine("PvpTeamInfoMessage:");
            Console.WriteLine(DictToString<int,int>(msg.TeamInfo,"TeamInfo",1)); //System.Collections.Generic.Dictionary`2[System.Int32,System.Int32]
        }

        public static void PrintMonsterKilledMessage(MonsterKilledMessage msg, object tag)
        {
            Console.WriteLine("MonsterKilledMessage:");
            Console.WriteLine("\tAttacker={0}", msg.Attacker);
            Console.WriteLine("\tTarget={0}", msg.Target);
            Console.WriteLine("\tActionType={0}", msg.ActionType);
            Console.WriteLine("\tHasEvilCore={0}", msg.HasEvilCore);
            Console.WriteLine("\tDamage={0}", msg.Damage);
            Console.WriteLine("\tDamagePositionX={0}", msg.DamagePositionX);
            Console.WriteLine("\tDamagePositionY={0}", msg.DamagePositionY);
            Console.WriteLine("\tDistance={0}", msg.Distance);
            Console.WriteLine("\tActorIndex={0}", msg.ActorIndex);
        }

        public static void PrintExecuteNpcServerCommandMessage(ExecuteNpcServerCommandMessage msg, object tag)
        {
            Console.WriteLine("ExecuteNpcServerCommandMessage: []");
        }

        public static void PrintRagdollKickedMessage(RagdollKickedMessage msg, object tag)
        {
            Console.WriteLine("RagdollKickedMessage:");
            Console.WriteLine("\tTag={0}", msg.Tag);
            Console.WriteLine("\tTargetEntityName={0}", msg.TargetEntityName);
            Console.WriteLine("\tEvilCoreType={0}", msg.EvilCoreType);
            Console.WriteLine("\tIsRareCore={0}", msg.IsRareCore);
        }

        public static void PrintCombatRecordMessage(CombatRecordMessage msg, object tag)
        {
            Console.WriteLine("CombatRecordMessage:");
            Console.WriteLine("\tPlayerNumber={0}", msg.PlayerNumber);
            Console.WriteLine("\tComboMax={0}", msg.ComboMax);
            Console.WriteLine("\tHitMax={0}", msg.HitMax);
            Console.WriteLine("\tStyleMax={0}", msg.StyleMax);
            Console.WriteLine("\tDeath={0}", msg.Death);
            Console.WriteLine("\tKill={0}", msg.Kill);
            Console.WriteLine("\tBattleAchieve={0}", msg.BattleAchieve);
            Console.WriteLine("\tHitTake={0}", msg.HitTake);
            Console.WriteLine("\tStyleCount={0}", msg.StyleCount);
            Console.WriteLine("\tRankStyle={0}", msg.RankStyle);
            Console.WriteLine("\tRankBattle={0}", msg.RankBattle);
            Console.WriteLine("\tRankTotal={0}", msg.RankTotal);
        }


        public static void PrintBurnItemInfo(BurnItemInfo msg, object tag)
        {
            Console.WriteLine("BurnItemInfo:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tCount={0}", msg.Count); //System.Int32 has a toString()
        }

        public static void PrintBurnResultMessage(BurnResultMessage msg, object tag)
        {
            Console.WriteLine("BurnResultMessage:");
            Console.WriteLine("\tMessage={0}", msg.Message); //System.String has a toString()
            Console.WriteLine("\tGauge={0}", msg.Gauge); //System.Int32 has a toString()
        }

        public static void PrintCaptchaRefreshMessage(CaptchaRefreshMessage msg, object tag)
        {
            Console.WriteLine("CaptchaRefreshMessage:");
        }

        public static void PrintCaptchaRefreshResultMessage(CaptchaRefreshResultMessage msg, object tag)
        {
            Console.WriteLine("CaptchaRefreshResultMessage:");
        }
        public static void PrintMicroPlayEventMessage(MicroPlayEventMessage msg, object tag)
        {
            Console.WriteLine("MicroPlayEventMessage: Slot={0} EventString={1}", msg.Slot, msg.EventString);
        }

        public static void PrintMonsterDamageReportMessage(MonsterDamageReportMessage msg, object tag)
        {
            Console.WriteLine("MonsterDamageReportMessage: Target={0}", msg.Target);
            foreach (MonsterTakeDamageInfo i in msg.TakeDamageList)
            {
                Console.WriteLine("\tMonsterTakeDamageInfo: Attacker={0} AttackTime={1} ActionName={2} Damage={3}", i.Attacker, i.AttackTime, i.ActionName, i.Damage);
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
            Console.WriteLine("PartyInfoMessage:");

            //don't want to show a diff between two different parties
            if (lastParty != null && lastParty.PartyID != msg.PartyID)
            {
                lastParty = null;
            }
            Console.WriteLine("\tPartyID={0}", msg.PartyID);
            if (lastParty != null && lastParty.PartySize != msg.PartySize)
            {
                Console.WriteLine("\tPartySize={0}", msg.PartySize);
            }
            if (lastParty != null && msg.State != lastParty.State)
            {
                Console.WriteLine("\tState={0}", msg.State);
            }

            int i = 0;
            if (lastParty?.Members != null && lastParty.Members.Count != 0)
            {
                PartyMemberInfo[] lastList = lastParty.Members.ToArray();
                foreach (PartyMemberInfo m in msg.Members)
                {
                    Console.WriteLine(PartyMemberInfoToString(m, String.Format("Member[{0}]", i++), 1));
                }
            }
            lastParty = msg;
        }

        public static void PrintExpandExpirationDateResponseMessage(ExpandExpirationDateResponseMessage msg, object tag)
        {
            Console.WriteLine("ExpandExpirationDateResponseMessage:");
            int type = GetPrivateProperty<int>(msg, "Type");
            long ItemID = GetPrivateProperty<long>(msg, "ItemID");
            Console.WriteLine("\tType={0}",type);
            Console.WriteLine("\tItemID={0}",ItemID);
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
            Console.WriteLine("RecommendShipMessage:");
            int i = 0;
            foreach (ShipInfo ship in msg.RecommendedShip) {
                Console.WriteLine(ShipInfoToString(ship,String.Format("ShipInfo[{0}]",i++),1)); //System.Collections.Generic.ICollection`1[ServiceCore.EndPointNetwork.ShipInfo]
            }
        }

        public static void PrintTransferValuesListMessage(TransferValuesListMessage msg, object tag)
        {
            Console.WriteLine("TransferValuesListMessage:");
            foreach(TransferValues values in msg.TransferInfos)
            {
                Console.WriteLine("TransferValues:");
                Console.WriteLine("\t\tCID={0}",values.CID);
                Console.WriteLine(DictToString<string,string>(values.Values,"Values",2));
            }
        }

        public static void PrintTransferValuesSetMessage(TransferValuesSetMessage msg, object tag)
        {
            Console.WriteLine("TransferValuesSetMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tCommand={0}", msg.Command); //System.String has a toString()
            Console.WriteLine(DictToString<string, string>(msg.Values, "Values", 1)); //System.Collections.Generic.IDictionary`2[System.String,System.String]
        }

        public static void PrintTransferValuesResultMessage(TransferValuesResultMessage msg, object tag)
        {
            Console.WriteLine("TransferValuesResultMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tCommandResult={0}", msg.CommandResult); //System.String has a toString()
            Console.WriteLine(DictToString<string, string>(msg.ResultValues, "ResultValues", 1));
        }

        public static void PrintSelectOrdersChangeMessage(SelectOrdersChangeMessage msg, object tag)
        {
            Console.WriteLine("SelectOrdersChangeMessage:");
            Console.WriteLine("\tSelectOrders=[{0}]",String.Join(",",msg.SelectOrders)); //System.Collections.Generic.ICollection`1[System.Byte]
        }

        public static void PrintSingleCharacterMessage(SingleCharacterMessage msg, object tag)
        {
            Console.WriteLine("SingleCharacterMessage:");
            Console.WriteLine("\tLoginPartyState={0}", msg.LoginPartyState); //System.Collections.Generic.ICollection`1[System.Int32]
            int i = 0;
            foreach (CharacterSummary c in msg.Characters) {
                Console.WriteLine(CharacterSummaryToString(c, String.Format("Characters[{0}]", i++), 1));
            }
            
        }

        public static void PrintLevelUpMessage(LevelUpMessage msg, object tag)
        {
            Console.WriteLine("LevelUpMessage:");
            Console.WriteLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            Console.WriteLine(DictToString<string,int>(msg.StatIncreased,"StatIncreased",1)); //System.Collections.Generic.IDictionary`2[System.String,System.Int32]
        }

        public static void PrintHackShieldRequestMessage(HackShieldRequestMessage msg, object tag)
        {
            Console.WriteLine("HackShieldRequestMessage:");
            if (msg.Request == null) {
                return;
            }
            ArraySegment<byte> r = msg.Request;
            Console.WriteLine("\tRequest={0}",BitConverter.ToString(r.Array,r.Offset,r.Count)); //System.ArraySegment`1[System.Byte]
        }

        public static void PrintDeleteMailMessage(DeleteMailMessage msg, object tag)
        {
            Console.WriteLine("DeleteMailMessage:");
            Console.WriteLine("\tMailList=[{0}]",String.Join(",",msg.MailList)); //System.Collections.Generic.ICollection`1[System.Int64]
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
            Console.WriteLine(ShipInfoToString(s, "UpdateShipMessage", 0));
        }

        public static void PrintPayCoinCompletedMessage(PayCoinCompletedMessage msg, object tag)
        {
            Console.WriteLine("PayCoinCompletedMessage:");
            foreach (PaidCoinInfo p in msg.Coininfos)
            {
                Console.WriteLine("\tSlot={0} SilverCoin=[{1}] PlatinumCoinOwner={2} PlatinumCoinType={3}", p.Slot, String.Join(",", p.SilverCoin), p.PlatinumCoinOwner, p.PlatinumCoinType);
            }
        }

        public static ConnectionRequestMessage lastConnectionRequestMsg = null;

        public static void PrintConnectionRequestMessage(ConnectionRequestMessage msg, object tag)
        {
            //TODO: use this info to decrypt relay messages
            Console.WriteLine("ConnectionRequestMessage:");
            Console.WriteLine("\tAddress={0}", msg.Address);
            Console.WriteLine("\tPort={0}", msg.Port);
            Console.WriteLine("\tPosixTime={0}", msg.PosixTime);
            Console.WriteLine("\tKey={0}", msg.Key);
            Console.WriteLine("\tCategory={0}", msg.Category);
            Console.WriteLine("\tPingHostCID={0}", msg.PingHostCID);
            Console.WriteLine("\tGroupID={0}", msg.GroupID);
            lastConnectionRequestMsg = msg;
        }

        public static void PrintLaunchShipGrantedMessage(LaunchShipGrantedMessage msg, object tag)
        {
            Console.WriteLine("\tLaunchShipGrantedMessage:");
            Console.WriteLine("\tQuestID={0}", msg.QuestID);
            Console.WriteLine("\tAdultRule={0}", msg.AdultRule);
            Console.WriteLine("\tIsPracticeMode={0}", msg.IsPracticeMode);
            Console.WriteLine("\tHostInfo:");
            Console.WriteLine(GameJoinMemberInfoToString(msg.HostInfo, 2));
        }

        public static void PrintTodayMissionUpdateMessage(TodayMissionUpdateMessage msg, object tag)
        {
            Console.WriteLine("TodayMissionUpdateMessage:");
            Console.WriteLine("\tID={0}", msg.ID);
            Console.WriteLine("\tCurrentCount={0}", msg.CurrentCount);
            Console.WriteLine("\tIsFinished={0}", msg.IsFinished);
        }
        public static void PrintReturnFromQuestMessage(ReturnFromQuestMessage msg, object tag)
        {
            Console.WriteLine("ReturnFromQuestMessage:");
            Console.WriteLine("\tOrder={0}", msg.Order);
            Console.WriteLine("\tStorySectorBSP={0}", msg.StorySectorBSP);
            Console.WriteLine("\tStorySectorEntity={0}", msg.StorySectorEntity);
        }

        public static void PrintKickedMessage(KickedMessage msg, object tag)
        {
            Console.WriteLine("KickedMessage: []");
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
            Console.WriteLine("FinishGameMessage:");
            Console.WriteLine("\tHasStorySector={0}", msg.HasStorySector);
            Console.WriteLine("\tCanRestart={0}", msg.CanRestart);
            Console.WriteLine("\tCanContinueFinishedQuest={0}", msg.CanContinueFinishedQuest);
            Console.WriteLine("\tSummary:");
            QuestSummaryInfo q = msg.Summary;
            Console.WriteLine("\t\tQuestID={0}", q.QuestID);
            Console.WriteLine("\t\tSwearID={0}", q.SwearID);
            Console.WriteLine("\t\tIsTodayQuest={0}", q.IsTodayQuest);
            Console.WriteLine("\t\tResult={0}", q.Result);
            Console.WriteLine("\t\tLastProgress={0}", q.LastProgress);
            Console.WriteLine("\t\tCurrentProgress={0}", q.CurrentProgress);
            if (q.ClearedGoals.Count != 0)
            {
                Console.WriteLine("\t\tClearedGoals:");
                foreach (KeyValuePair<int, SummaryGoalInfo> entry in q.ClearedGoals)
                {
                    Console.WriteLine("\t\t\t{0}=(GoalID={1} Exp={2} Gold={3} Ap={4})", entry.Key, entry.Value.GoalID, entry.Value.Exp, entry.Value.Gold, entry.Value.Ap);
                }
            }
            Console.WriteLine(SummaryRewardInfoListToString(q.BattleExp, "BattleExp", 2));
            Console.WriteLine(SummaryRewardInfoListToString(q.EtcExp, "EtcExp", 2));
            Console.WriteLine(SummaryRewardInfoListToString(q.MainGold, "MainGold", 2));
            Console.WriteLine(SummaryRewardInfoListToString(q.EtcGold, "EtcGold", 2));
            Console.WriteLine(SummaryRewardInfoListToString(q.QuestAp, "QuestAp", 2));
            Console.WriteLine(SummaryRewardInfoListToString(q.EtcAp, "EtcAp", 2));
            Console.WriteLine("\t\tExp={0}", q.Exp);
            Console.WriteLine("\t\tGold={0}", q.Gold);
            Console.WriteLine("\t\tAp={0}", q.Ap);
            Console.WriteLine(DictToString<string, int>(q.RewardItem, "RewardItem", 2));
        }
        public static void PrintMicroStatusEffectUpdated(MicroStatusEffectUpdated msg, object tag)
        {
            Console.WriteLine("MicroStatusEffectUpdated:");
            Console.WriteLine("\tCharacterName={0}", msg.CharacterName);
            Console.WriteLine("\tSlot={0}", msg.Slot);
            Console.WriteLine(StatusEffectListToString(msg.StatusEffects, "StatusEffects", msg.CharacterName, 1));
        }

        public static void PrintQuestSuccessSceneStartMessage(QuestSuccessSceneStartMessage msg, object tag)
        {
            Console.WriteLine("QuestSuccessSceneStartMessage: []");
        }

        public static void PrintQuestTargetResultMessage(QuestTargetResultMessage msg, object tag)
        {
            Console.WriteLine("QuestTargetResultMessage:");
            Console.WriteLine("\tCharacterName={0}", msg.CharacterName);
            Console.WriteLine("\tGoalID={0}", msg.GoalID);
            Console.WriteLine("\tIsGoalSuccess={0}", msg.IsGoalSuccess);
            Console.WriteLine("\tIsQuestSuccess={0}", msg.IsQuestSuccess);
            Console.WriteLine("\tExp={0}", msg.Exp);
            Console.WriteLine("\tGold={0}", msg.Gold);
            Console.WriteLine("\tAp={0}", msg.Ap);
        }

        public static void PrintDropErgMessage(DropErgMessage msg, object tag)
        {
            Console.WriteLine("DropErgMessage:");
            Console.WriteLine("\tBrokenProp={0}", msg.BrokenProp);
            Console.WriteLine("\tMonsterEntityName={0}", msg.MonsterEntityName);
            Console.WriteLine("\tDropErg:");
            foreach (ErgInfo e in msg.DropErg)
            {
                int winner = GetPrivateProperty<int>(e, "winner");
                int ergID = GetPrivateProperty<int>(e, "ergID");
                string ergClass = GetPrivateProperty<string>(e, "ergClass");
                string ergType = GetPrivateProperty<string>(e, "ergType");
                int amount = GetPrivateProperty<int>(e, "amount");
                Console.WriteLine("\t\tWinner={0} ErgID={1} ErgClass={2} ErgType={3} Amount={4}", winner, ergID, ergClass, ergType, amount);
            }
        }
        public static void PrintGetItemMessage(GetItemMessage msg, object tag)
        {
            Console.WriteLine("GetItemMessage:");
            Console.WriteLine("\tPlayerName={0}", msg.PlayerName);
            Console.WriteLine("\tItemClass={0}", msg.ItemClass);
            Console.WriteLine("\tCount={0}", msg.Count);
            Console.WriteLine("\tCoreType={0}", msg.CoreType);
            Console.WriteLine("\tLucky={0}", msg.Lucky);
            Console.WriteLine("\tLuckBonus={0}", msg.LuckBonus);
            Console.WriteLine("\tGiveItemResult={0}", (GiveItem.ResultEnum)msg.GiveItemResult);
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
            Console.WriteLine("GameStartGrantedMessage:");
            Console.WriteLine("\tQuestLevel={0}", msg.QuestLevel);
            Console.WriteLine("\tQuestTime={0}", msg.QuestTime);
            Console.WriteLine("\tHardMode={0}", msg.HardMode);
            Console.WriteLine("\tSoloSector={0}", msg.SoloSector);
            Console.WriteLine("\tIsHuntingQuest={0}", msg.IsHuntingQuest);
            Console.WriteLine("\tInitGameTime={0}", msg.InitGameTime);
            Console.WriteLine("\tSectorMoveGameTime={0}", msg.SectorMoveGameTime);
            Console.WriteLine("\tDifficulty={0}", msg.Difficulty);
            Console.WriteLine("\tIsTimerDecreasing={0}", msg.IsTimerDecreasing);
            Console.WriteLine("\tQuestStartedPlayerCount={0}", msg.QuestStartedPlayerCount);
            Console.WriteLine("\tNewSlot={0}", msg.NewSlot);
            Console.WriteLine("\tNewKey={0}", msg.NewKey);
            Console.WriteLine("\tIsUserDedicated={0}", msg.IsUserDedicated);

            Console.WriteLine(QuestSectorInfosToString(msg.QuestSectorInfos, "SectorInfo", 1));

        }
        public static void PrintShipListMessage(ShipListMessage msg, object tag)
        {
            Console.WriteLine("ShipListMessage:");
            Console.WriteLine("\tIgnored={0}", msg.Ignored);
            int i = 0;
            foreach (ShipInfo s in msg.ShipList)
            {
                Console.WriteLine(ShipInfoToString(s, String.Format("ShipInfo[{0}]", i++), 1));
            }
        }

        public static void PrintLearnNewSkillResultMessage(LearnNewSkillResultMessage msg, object tag)
        {
            Console.WriteLine("LearnNewSkillResultMessage: Result={0}", (LearnNewSkillResultMessage.LearnNewSkillResult)msg.result);
        }

        public static void PrintLearnSkillMessage(LearnSkillMessage msg, object tag)
        {
            Console.WriteLine("LearnSkillMessage:");
            Console.WriteLine("\tSkillID={0}", msg.SkillID);
            Console.WriteLine("\tAP={0}", msg.AP);
        }
        public static void PrintLearnNewSkillMessage(LearnNewSkillMessage msg, object tag)
        {
            Console.WriteLine("LearnNewSkillMessage: SkillID={0}", msg.SkillID);
        }

        public static void PrintTodayMissionCompleteMessage(TodayMissionCompleteMessage msg, object tag)
        {
            Console.WriteLine("TodayMissionCompleteMessage: ID={0}", msg.ID);
        }

        public static void PrintEndTradeSessionMessage(EndTradeSessionMessage msg, object tag)
        {
            Console.WriteLine("EndTradeSessionMessage: []");
        }

        public static void PrintPutErgMessage(PutErgMessage msg, object tag)
        {
            int ergID = GetPrivateProperty<int>(msg, "ergID");
            int ergTag = GetPrivateProperty<int>(msg, "tag");
            Console.WriteLine("PutErgMessage: ergID={0} tag={1}", ergID, ergTag);
        }

        public static void PrintPickErgMessage(PickErgMessage msg, object tag)
        {
            int prop = GetPrivateProperty<int>(msg, "prop");
            Console.WriteLine("PickErgMessage: prop={0}", prop);
        }

        public static void PrintUserDSHostTransferStartMessage(UserDSHostTransferStartMessage msg, object tag)
        {
            Console.WriteLine("UserDSHostTransferStartMessage: []");
        }
        public static void PrintUserDSProcessStartMessage(UserDSProcessStartMessage msg, object tag)
        {
            Console.WriteLine("UserDSProcessStartMessage:");
            Console.WriteLine("\tServerAddress={0}", msg.ServerAddress);
            Console.WriteLine("\tServerPort={0}", msg.ServerPort);
            Console.WriteLine("\tEntityID={0}", msg.EntityID);
        }

        public static void PrintStartGameAckMessage(StartGameAckMessage msg, object tag)
        {
            Console.WriteLine("StartGameAckMessage: []");
        }

        public static void PrintShipOptionMessage(ShipOptionMessage msg, object tag)
        {
            Console.WriteLine("ShipOptionMessage:");
            Console.WriteLine(ShipOptionInfoToString(msg.ShipOption,1)); //ServiceCore.EndPointNetwork.ShipOptionInfo
        }

        public static void PrintOpenPartyWithShipInfoMessage(OpenPartyWithShipInfoMessage msg, object tag)
        {
            Console.WriteLine("OpenPartyWithShipInfoMessage:");
            Console.WriteLine("\tQuestID={0}", msg.QuestID);
            Console.WriteLine("\tTitle={0}", msg.Title);
            Console.WriteLine("\tSwearID={0}", msg.SwearID);
            Console.WriteLine(ShipOptionInfoToString(msg.Option,1));
        }

        public static void PrintPvpHostInfoMessage(PvpHostInfoMessage msg, object tag)
        {
            Console.WriteLine("PvpHostInfoMessage:");
            Console.WriteLine("\tGameInfo={0}", msg.GameInfo); //ServiceCore.EndPointNetwork.GameInfo has a toString()
            Console.WriteLine(GameJoinMemberInfoToString(msg.MemberInfo,1)); //ServiceCore.EndPointNetwork.GameJoinMemberInfo has a toString()
            Console.WriteLine(DictToString<string,string>(msg.Config,"Config",1)); //System.Collections.Generic.Dictionary`2[System.String,System.String]
        }

        public static void PrintSetStoryLineStatusMessage(SetStoryLineStatusMessage msg, object tag)
        {
            Console.WriteLine("SetStoryLineStatusMessage:");
            Console.WriteLine("\tStoryLineID={0}", msg.StoryLineID);
            Console.WriteLine("\tStatus={0}", msg.Status);
        }

        public static void PrintQueryShopItemInfoMessage(QueryShopItemInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryShopItemInfoMessage: ShopID={0}", msg.ShopID);
        }

        public static void PrintSelectButtonMessage(SelectButtonMessage msg, object tag)
        {
            Console.WriteLine("SelectButtonMessage: ButtonIndex={0}", msg.ButtonIndex);
        }

        public static void PrintDestroySlotItemMessage(DestroySlotItemMessage msg, object tag)
        {
            long itemID = GetPrivateProperty<long>(msg, "itemID");
            Console.WriteLine("DestroySlotItemMessage: ItemID={0}", itemID);
        }

        public static void PrintCloseUserDS(CloseUserDS msg, object tag)
        {
            Console.WriteLine("CloseUserDS:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintLaunchUserDS(LaunchUserDS msg, object tag)
        {
            Console.WriteLine("LaunchUserDS:");
            Console.WriteLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            Console.WriteLine("\tMicroPlayID={0}", msg.MicroPlayID); //System.Int64 has a toString()
            Console.WriteLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
            Console.WriteLine("\tFrontendID={0}", msg.FrontendID); //System.Int64 has a toString()
            Console.WriteLine("\tIsAdultMode={0}", msg.IsAdultMode); //System.Boolean has a toString()
            Console.WriteLine("\tIsPracticeMode={0}", msg.IsPracticeMode); //System.Boolean has a toString()
            Console.WriteLine("\tHostIP={0}", msg.HostIP); //System.Net.IPAddress has a toString()
            Console.WriteLine("\tUserDSRealHostCID={0}", msg.UserDSRealHostCID); //System.Int64 has a toString()
            Console.WriteLine("\tServerAddress={0}", msg.ServerAddress); //System.String has a toString()
            Console.WriteLine("\tUserDSEntityID={0}", msg.UserDSEntityID); //System.Int64 has a toString()
            Console.WriteLine("\tPort={0}", msg.Port); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintSendPacketUserDS(SendPacketUserDS msg, object tag)
        {
            Console.WriteLine("SendPacketUserDS:");
            Console.WriteLine("\tPacket={0}", msg.Packet); //Devcat.Core.Net.Message.Packet has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUserDSHostTransferStart(UserDSHostTransferStart msg, object tag)
        {
            Console.WriteLine("UserDSHostTransferStart:");
            Console.WriteLine("\tUserDSRealHostCID={0}", msg.UserDSRealHostCID); //System.Int64 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUpdateArenaWinCount(UpdateArenaWinCount msg, object tag)
        {
            Console.WriteLine("UpdateArenaWinCount:");
            Console.WriteLine("\tArenaWinCount={0}", msg.ArenaWinCount); //System.Int32 has a toString()
            Console.WriteLine("\tArenaLoseCount={0}", msg.ArenaLoseCount); //System.Int32 has a toString()
            Console.WriteLine("\tArenaSuccessiveWinCount={0}", msg.ArenaSuccessiveWinCount); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryDSServiceInfo(QueryDSServiceInfo msg, object tag)
        {
            Console.WriteLine("QueryDSServiceInfo:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryPvpDSInfos(QueryPvpDSInfos msg, object tag)
        {
            Console.WriteLine("QueryPvpDSInfos:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRegisterDSEntity(RegisterDSEntity msg, object tag)
        {
            Console.WriteLine("RegisterDSEntity:");
            Console.WriteLine("\tServiceID={0}", msg.ServiceID); //System.Int32 has a toString()
            Console.WriteLine("\tCoreCount={0}", msg.CoreCount); //System.Int32 has a toString()
            Console.WriteLine("\tGiantRaidMachine={0}", msg.GiantRaidMachine); //System.Boolean has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRemoveDSEntity(RemoveDSEntity msg, object tag)
        {
            Console.WriteLine("RemoveDSEntity:");
            Console.WriteLine("\tDSID={0}", msg.DSID); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintDSServiceInfoMessage(DSServiceInfoMessage msg, object tag)
        {
            Console.WriteLine("DSServiceInfoMessage:");
        }

        public static void PrintQueryDSServiceInfoMessage(QueryDSServiceInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryDSServiceInfoMessage:");
        }

        public static void PrintArenaResultInfoMessage(ArenaResultInfoMessage msg, object tag)
        {
            Console.WriteLine("ArenaResultInfoMessage:");
            Console.WriteLine("\tArenaWinCount={0}", msg.ArenaWinCount); //System.Int32 has a toString()
            Console.WriteLine("\tArenaLoseCount={0}", msg.ArenaLoseCount); //System.Int32 has a toString()
            Console.WriteLine("\tArenaSuccessiveWinCount={0}", msg.ArenaSuccessiveWinCount); //System.Int32 has a toString()
        }

        public static void PrintPvpPenaltyMessage(PvpPenaltyMessage msg, object tag)
        {
            Console.WriteLine("PvpPenaltyMessage:");
            Console.WriteLine("\tPlayerIndex={0}", msg.PlayerIndex); //System.Int32 has a toString()
        }

        public static void PrintPvpRegisterGameWaitingQueueMessage(PvpRegisterGameWaitingQueueMessage msg, object tag)
        {
            Console.WriteLine("PvpRegisterGameWaitingQueueMessage:");
            Console.WriteLine("\tGameIndex={0}", msg.GameIndex); //System.Int32 has a toString()
        }

        public static void PrintPvpUnregisterGameWaitingQueueMessage(PvpUnregisterGameWaitingQueueMessage msg, object tag)
        {
            Console.WriteLine("PvpUnregisterGameWaitingQueueMessage:");
        }

        public static void PrintSetStoryHintCategoryMessage(SetStoryHintCategoryMessage msg, object tag)
        {
            Console.WriteLine("SetStoryHintCategoryMessage:");
            Console.WriteLine("\tCategory={0}", msg.Category); //System.Int32 has a toString()
        }

        public static void PrintPvpUnregisterGameWaitingQueue(PvpUnregisterGameWaitingQueue msg, object tag)
        {
            Console.WriteLine("PvpUnregisterGameWaitingQueue:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintPvpGivePanalty(PvpGivePanalty msg, object tag)
        {
            Console.WriteLine("PvpGivePanalty:");
            Console.WriteLine("\tPlayerIndex={0}", msg.PlayerIndex); //System.Int32 has a toString()
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintSetStoryHintCategory(SetStoryHintCategory msg, object tag)
        {
            Console.WriteLine("SetStoryHintCategory:");
            Console.WriteLine("\tCategory={0}", msg.Category); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintWhisperResultToGameClient(WhisperResultToGameClient msg, object tag)
        {
            Console.WriteLine("WhisperResultToGameClient:");
            Console.WriteLine("\tMyCID={0}", msg.MyCID); //System.Int64 has a toString()
            Console.WriteLine("\tResultNo={0}", msg.ResultNo); //System.Int32 has a toString()
            Console.WriteLine("\tReceiverName={0}", msg.ReceiverName); //System.String has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintWhisperToGameClient(WhisperToGameClient msg, object tag)
        {
            Console.WriteLine("WhisperToGameClient:");
            Console.WriteLine("\tFrom={0}", msg.From); //System.String has a toString()
            Console.WriteLine("\tToCID={0}", msg.ToCID); //System.Int64 has a toString()
            Console.WriteLine("\tMessage={0}", msg.Message); //System.String has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintStopStorytelling(StopStorytelling msg, object tag)
        {
            Console.WriteLine("StopStorytelling:");
            Console.WriteLine("\tTargetState={0}", msg.TargetState); //System.Boolean has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryNpcList(QueryNpcList msg, object tag)
        {
            Console.WriteLine("QueryNpcList:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryStoryLog(QueryStoryLog msg, object tag)
        {
            Console.WriteLine("QueryStoryLog:");
            Console.WriteLine("\tStoryLineID={0}", msg.StoryLineID); //System.String has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryStoryProgress(QueryStoryProgress msg, object tag)
        {
            Console.WriteLine("QueryStoryProgress:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintReloadNpcScript(ReloadNpcScript msg, object tag)
        {
            Console.WriteLine("ReloadNpcScript:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintSelectButton(SelectButton msg, object tag)
        {
            Console.WriteLine("SelectButton:");
            Console.WriteLine("\tButtonIndex={0}", msg.ButtonIndex); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryNpcTalk(QueryNpcTalk msg, object tag)
        {
            Console.WriteLine("QueryNpcTalk:");
            Console.WriteLine("\tBuildingID={0}", msg.BuildingID); //System.String has a toString()
            Console.WriteLine("\tNpcID={0}", msg.NpcID); //System.String has a toString()
            Console.WriteLine("\tStoryLine={0}", msg.StoryLine); //System.String has a toString()
            Console.WriteLine("\tCommand={0}", msg.Command); //System.String has a toString()
            Console.WriteLine("\tCheatPermission={0}", msg.CheatPermission); //System.Boolean has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryStoryGuide(QueryStoryGuide msg, object tag)
        {
            Console.WriteLine("QueryStoryGuide:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRankAlarmInfo(RankAlarmInfo msg, object tag)
        {
            Console.WriteLine("RankAlarmInfo:");
            Console.WriteLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
            Console.WriteLine("\tEventID={0}", msg.EventID); //System.Int32 has a toString()
            Console.WriteLine("\tRank={0}", msg.Rank); //System.Int32 has a toString()
        }

        public static void PrintRankResultInfo(RankResultInfo msg, object tag)
        {
            Console.WriteLine("RankResultInfo:");
            Console.WriteLine("\tEventID={0}", msg.EventID); //System.Int32 has a toString()
            Console.WriteLine("\tRank={0}", msg.Rank); //System.Int32 has a toString()
            Console.WriteLine("\tRankPrev={0}", msg.RankPrev); //System.Int32 has a toString()
            Console.WriteLine("\tScore={0}", msg.Score); //System.Int64 has a toString()
            Console.WriteLine("\tCharacterName={0}", msg.CharacterName); //System.String has a toString()
        }

        public static void PrintUpdateRankBasis(UpdateRankBasis msg, object tag)
        {
            Console.WriteLine("UpdateRankBasis:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tRankID={0}", msg.RankID); //System.String has a toString()
            Console.WriteLine("\tScore={0}", msg.Score); //System.Int64 has a toString()
            Console.WriteLine("\tEntityID={0}", msg.EntityID); //System.Int64 has a toString()
            Console.WriteLine("\tGuildName={0}", msg.GuildName); //System.String has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUpdateRankFavorite(UpdateRankFavorite msg, object tag)
        {
            Console.WriteLine("UpdateRankFavorite:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tRankID={0}", msg.RankID); //System.String has a toString()
            Console.WriteLine("\tIsAddition={0}", msg.IsAddition); //System.Boolean has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintCloseConnection(CloseConnection msg, object tag)
        {
            Console.WriteLine("CloseConnection:");
            Console.WriteLine("\tFrontendID={0}", msg.FrontendID); //System.Int64 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintJoinP2PGroup(JoinP2PGroup msg, object tag)
        {
            Console.WriteLine("JoinP2PGroup:");
            Console.WriteLine("\tFrontendID={0}", msg.FrontendID); //System.Int64 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintLeaveP2PGroup(LeaveP2PGroup msg, object tag)
        {
            Console.WriteLine("LeaveP2PGroup:");
            Console.WriteLine("\tFrontendID={0}", msg.FrontendID); //System.Int64 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintReserve(Reserve msg, object tag)
        {
            Console.WriteLine("Reserve:");
            Console.WriteLine("\tFrontendID={0}", msg.FrontendID); //System.Int64 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryRankGoal(QueryRankGoal msg, object tag)
        {
            Console.WriteLine("QueryRankGoal:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tEventID={0}", msg.EventID); //System.Int32 has a toString()
            Console.WriteLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
            Console.WriteLine("\tRivalName={0}", msg.RivalName); //System.String has a toString()
            Console.WriteLine("\tRivalScore={0}", msg.RivalScore); //System.Int64 has a toString()
            Console.WriteLine("\tRivalRank={0}", msg.RivalRank); //System.Int32 has a toString()
            Console.WriteLine("\tRivalRankPrev={0}", msg.RivalRankPrev); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUpdateQuestPlayCount(UpdateQuestPlayCount msg, object tag)
        {
            Console.WriteLine("UpdateQuestPlayCount:");
            Console.WriteLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            Console.WriteLine("\tStartTime={0}", msg.StartTime); //System.DateTime has a toString()
            Console.WriteLine("\tIgnoreTodayCount={0}", msg.IgnoreTodayCount); //System.Boolean has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRemoveUser(RemoveUser msg, object tag)
        {
            Console.WriteLine("RemoveUser:");
            Console.WriteLine("\tCharacterID={0}", msg.CharacterID); //System.Int64 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintInvitedPartyMember(InvitedPartyMember msg, object tag)
        {
            Console.WriteLine("InvitedPartyMember:");
            Console.WriteLine("\tSlotNum={0}", msg.SlotNum); //System.Int32 has a toString()
        }

        public static void PrintUseConsumable(UseConsumable msg, object tag)
        {
            Console.WriteLine("UseConsumable:");
            Console.WriteLine("\tHostCID={0}", msg.HostCID); //System.Int64 has a toString()
            Console.WriteLine("\tUserTag={0}", msg.UserTag); //System.Int64 has a toString()
            Console.WriteLine("\tUsedPart={0}", msg.UsedPart); //System.Int32 has a toString()
            Console.WriteLine("\tInnerSlot={0}", msg.InnerSlot); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintSetStoryLineStatus(SetStoryLineStatus msg, object tag)
        {
            Console.WriteLine("SetStoryLineStatus:");
            Console.WriteLine("\tStoryLineID={0}", msg.StoryLineID); //System.String has a toString()
            Console.WriteLine("\tStatus={0}", msg.Status); //ServiceCore.EndPointNetwork.StoryLineStatus
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintWhisper(Whisper msg, object tag)
        {
            Console.WriteLine("Whisper:");
            Console.WriteLine("\tFrom={0}", msg.From); //System.String has a toString()
            Console.WriteLine("\tTo={0}", msg.To); //System.String has a toString()
            Console.WriteLine("\tMessage={0}", msg.Message); //System.String has a toString()
            Console.WriteLine("\tFailReason={0}", msg.FailReason); //ServiceCore.TalkServiceOperations.Whisper+WhisperResult
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintModifyStoryVariable(ModifyStoryVariable msg, object tag)
        {
            Console.WriteLine("ModifyStoryVariable:");
            Console.WriteLine("\tStoryLine={0}", msg.StoryLine); //System.String has a toString()
            Console.WriteLine("\tKey={0}", msg.Key); //System.String has a toString()
            Console.WriteLine("\tValue={0}", msg.Value); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintNotifyStoryEvent(NotifyStoryEvent msg, object tag)
        {
            Console.WriteLine("NotifyStoryEvent:");
            Console.WriteLine("\ttype={0}", msg.type); //ServiceCore.StoryServiceOperations.StoryEventType
            Console.WriteLine("\tObject={0}", msg.Object); //System.String has a toString()
            Console.WriteLine("\tIsSuccess={0}", msg.IsSuccess); //System.Boolean has a toString()
            Console.WriteLine("\tStorySectorID={0}", msg.StorySectorID); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintDailyStoryProcess(DailyStoryProcess msg, object tag)
        {
            Console.WriteLine("DailyStoryProcess:");
            Console.WriteLine("\tNextOpTime={0}", msg.NextOpTime); //System.DateTime has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintExecuteServerCommand(ExecuteServerCommand msg, object tag)
        {
            Console.WriteLine("ExecuteServerCommand:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintGiveStory(GiveStory msg, object tag)
        {
            Console.WriteLine("GiveStory:");
            Console.WriteLine("\tStoryLineID={0}", msg.StoryLineID); //System.String has a toString()
            Console.WriteLine("\tFailReason={0}", msg.FailReason); //ServiceCore.StoryServiceOperations.GiveStory+FailReasonEnum
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintGiveQuest(GiveQuest msg, object tag)
        {
            Console.WriteLine("GiveQuest:");
            Console.WriteLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            Console.WriteLine("\tReveal={0}", msg.Reveal); //System.Boolean has a toString()
            Console.WriteLine("\tFailReason={0}", msg.FailReason); //ServiceCore.QuestOwnershipServiceOperations.GiveQuest+FailReasonEnum
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintJoinParty(JoinParty msg, object tag)
        {
            Console.WriteLine("JoinParty:");
            Console.WriteLine("\tCharacterID={0}", msg.CharacterID); //System.Int64 has a toString()
            Console.WriteLine("\tFrontendID={0}", msg.FrontendID); //System.Int64 has a toString()
            Console.WriteLine("\tNexonSN={0}", msg.NexonSN); //System.Int32 has a toString()
            Console.WriteLine("\tJType={0}", msg.JType); //ServiceCore.PartyServiceOperations.JoinType
            Console.WriteLine("\tPushMicroPlayInfo={0}", msg.PushMicroPlayInfo); //System.Boolean has a toString()
            Console.WriteLine("\tSlot={0}", msg.Slot); //System.Int32 has a toString()
            Console.WriteLine("\tPlayID={0}", msg.PlayID); //System.Int64 has a toString()
            Console.WriteLine("\tIsEntranceProcessSkipped={0}", msg.IsEntranceProcessSkipped); //System.Boolean has a toString()
            Console.WriteLine("\tReason={0}", msg.Reason); //System.Object has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintGoalTarget(GoalTarget msg, object tag)
        {
            Console.WriteLine("GoalTarget:");
            Console.WriteLine("\tGoalID={0}", msg.GoalID); //System.Int32 has a toString()
            Console.WriteLine("\tWeight={0}", msg.Weight); //System.Int32 has a toString()
            Console.WriteLine("\tRegex={0}", msg.Regex); //System.String has a toString()
            Console.WriteLine("\tCount={0}", msg.Count); //System.Int32 has a toString()
            Console.WriteLine("\tPositive={0}", msg.Positive); //System.Boolean has a toString()
            Console.WriteLine("\tExp={0}", msg.Exp); //System.Int32 has a toString()
            Console.WriteLine("\tBaseExp={0}", msg.BaseExp); //System.Int32 has a toString()
            Console.WriteLine("\tGold={0}", msg.Gold); //System.Int32 has a toString()
            Console.WriteLine("\tItemReward={0}", msg.ItemReward); //System.String has a toString()
            Console.WriteLine("\tItemNum={0}", msg.ItemNum); //System.Int32 has a toString()
        }

        public static void PrintPartyChat(PartyChat msg, object tag)
        {
            Console.WriteLine("PartyChat:");
            Console.WriteLine("\tSenderSlot={0}", msg.SenderSlot); //System.Int32 has a toString()
            Console.WriteLine("\tMessage={0}", msg.Message); //System.String has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }


        public static void PrintSinkShip(SinkShip msg, object tag)
        {
            Console.WriteLine("SinkShip:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintStopShipping(StopShipping msg, object tag)
        {
            Console.WriteLine("StopShipping:");
            Console.WriteLine("\tTargetState={0}", msg.TargetState); //System.Boolean has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintTransferMaster(TransferMaster msg, object tag)
        {
            Console.WriteLine("TransferMaster:");
            Console.WriteLine("\tMasterCID={0}", msg.MasterCID); //System.Int64 has a toString()
            Console.WriteLine("\tNewMasterCID={0}", msg.NewMasterCID); //System.Int64 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUpdateLatestPing(UpdateLatestPing msg, object tag)
        {
            Console.WriteLine("UpdateLatestPing:");
            Console.WriteLine("\tSlotNum={0}", msg.SlotNum); //System.Int32 has a toString()
            Console.WriteLine("\tLatestPing={0}", msg.LatestPing); //System.Int32 has a toString()
            Console.WriteLine("\tLatestFrameRate={0}", msg.LatestFrameRate); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintPvpReport(PvpReport msg, object tag)
        {
            Console.WriteLine("PvpReport:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tEvent={0}", msg.Event); //ServiceCore.EndPointNetwork.PvpReportType
            Console.WriteLine("\tSubject={0}", msg.Subject); //System.Int32 has a toString()
            Console.WriteLine("\tObject={0}", msg.Object); //System.Int32 has a toString()
            Console.WriteLine("\tArg={0}", msg.Arg); //System.String has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRegisterHostGameInfo(RegisterHostGameInfo msg, object tag)
        {
            Console.WriteLine("RegisterHostGameInfo:");
            Console.WriteLine("\tHostCID={0}", msg.HostCID); //System.Int64 has a toString()
            Console.WriteLine("\tGameInfo={0}", msg.GameInfo); //ServiceCore.EndPointNetwork.GameInfo has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRegisterPvpPlayer(RegisterPvpPlayer msg, object tag)
        {
            Console.WriteLine("RegisterPvpPlayer:");
            Console.WriteLine("\tPvpMode={0}", msg.PvpMode); //System.String has a toString()
                                                             //Console.WriteLine("\tIDList={0}",msg.IDList); //System.Collections.Generic.ICollection`1[ServiceCore.PartyServiceOperations.PvPPartyMemberInfo]
            Console.WriteLine("\tMyCID={0}", msg.MyCID); //System.Int64 has a toString()
            Console.WriteLine("\tCheat={0}", msg.Cheat); //ServiceCore.EndPointNetwork.PvpRegisterCheat
            Console.WriteLine("\tChannelID={0}", msg.ChannelID); //System.Int32 has a toString()
            Console.WriteLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUnregisterPvpPlayer(UnregisterPvpPlayer msg, object tag)
        {
            Console.WriteLine("UnregisterPvpPlayer:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintDailyQuestProcess(DailyQuestProcess msg, object tag)
        {
            Console.WriteLine("DailyQuestProcess:");
            Console.WriteLine("\tNextOpTime={0}", msg.NextOpTime); //System.DateTime has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintNotifyLevelToQuest(NotifyLevelToQuest msg, object tag)
        {
            Console.WriteLine("NotifyLevelToQuest:");
            Console.WriteLine("\tCharacterLevel={0}", msg.CharacterLevel); //System.Int32 has a toString()
            Console.WriteLine("\tCafeType={0}", msg.CafeType); //System.Nullable`1[System.Int32] has a toString()
            Console.WriteLine("\tHasVIPBonusEffect={0}", msg.HasVIPBonusEffect); //System.Nullable`1[System.Boolean] has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintNotifyPermissionsToQuest(NotifyPermissionsToQuest msg, object tag)
        {
            Console.WriteLine("NotifyPermissionsToQuest:");
            Console.WriteLine("\tCharacterPermissions={0}", msg.CharacterPermissions); //ServiceCore.LoginServiceOperations.Permissions
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintNotifyStartQuest(NotifyStartQuest msg, object tag)
        {
            Console.WriteLine("NotifyStartQuest:");
            Console.WriteLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            Console.WriteLine("\tStartTime={0}", msg.StartTime); //System.DateTime has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryAvailableQuestsByQuestSet(QueryAvailableQuestsByQuestSet msg, object tag)
        {
            Console.WriteLine("QueryAvailableQuestsByQuestSet:");
            Console.WriteLine("\tQuestSet={0}", msg.QuestSet); //System.Int32 has a toString()
            Console.WriteLine("\tDifficulty={0}", msg.Difficulty); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryQuestDigest(QueryQuestDigest msg, object tag)
        {
            Console.WriteLine("QueryQuestDigest:");
            Console.WriteLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            Console.WriteLine("\tSwearID={0}", msg.SwearID); //System.Int32 has a toString()
            Console.WriteLine("\tCheckDifficulty={0}", msg.CheckDifficulty); //System.Int32 has a toString()
            Console.WriteLine("\tCheckDisableUserShip={0}", msg.CheckDisableUserShip); //System.Boolean has a toString()
            Console.WriteLine("\tIsPracticeMode={0}", msg.IsPracticeMode); //System.Boolean has a toString()
            Console.WriteLine("\tIsUserDSMode={0}", msg.IsUserDSMode); //System.Boolean has a toString()
            Console.WriteLine("\tIsDropTest={0}", msg.IsDropTest); //System.Boolean has a toString()
            Console.WriteLine("\tQuestDigest={0}", msg.QuestDigest); //ServiceCore.QuestOwnershipServiceOperations.QuestDigest has a toString()
            Console.WriteLine("\tQuestStatus={0}", msg.QuestStatus); //System.Int32 has a toString()
            Console.WriteLine("\tFailReason={0}", msg.FailReason); //ServiceCore.QuestOwnershipServiceOperations.QuestConstraintResult
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryQuestDigestDS(QueryQuestDigestDS msg, object tag)
        {
            Console.WriteLine("QueryQuestDigestDS:");
            Console.WriteLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            Console.WriteLine("\tQuestDigest={0}", msg.QuestDigest); //ServiceCore.QuestOwnershipServiceOperations.QuestDigest has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryQuestProgress(QueryQuestProgress msg, object tag)
        {
            Console.WriteLine("QueryQuestProgress:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryQuestSettings(QueryQuestSettings msg, object tag)
        {
            Console.WriteLine("QueryQuestSettings:");
            Console.WriteLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            Console.WriteLine("\tDifficulty={0}", msg.Difficulty); //System.Int32 has a toString()
                                                                   //Console.WriteLine("\tQuestSettings={0}",msg.QuestSettings); //ServiceCore.QuestOwnershipServiceOperations.QuestSettings
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }


        public static void PrintPvPPartyMemberInfo(PvPPartyMemberInfo msg, object tag)
        {
            Console.WriteLine("PvPPartyMemberInfo:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tFID={0}", msg.FID); //System.Int64 has a toString()
            Console.WriteLine("\tNexonSN={0}", msg.NexonSN); //System.Int32 has a toString()
            Console.WriteLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            Console.WriteLine("\tBaseCharacter={0}", msg.BaseCharacter); //ServiceCore.CharacterServiceOperations.BaseCharacter
            Console.WriteLine("\tName={0}", msg.Name); //System.String has a toString()
            Console.WriteLine("\tGuildID={0}", msg.GuildID); //System.Int32 has a toString()
            Console.WriteLine("\tGuildName={0}", msg.GuildName); //System.String has a toString()
            Console.WriteLine("\tMMOLocation={0}", msg.MMOLocation); //System.Int64 has a toString()
            Console.WriteLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
            Console.WriteLine("\tRewardBonus={0}", msg.RewardBonus); //System.Int32 has a toString()
        }

        public static void PrintSharingResponse(SharingResponse msg, object tag)
        {
            Console.WriteLine("SharingResponse:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tAccept={0}", msg.Accept); //System.Boolean has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintBlockEntering(BlockEntering msg, object tag)
        {
            Console.WriteLine("BlockEntering:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintBreakSinglePlayerParty(BreakSinglePlayerParty msg, object tag)
        {
            Console.WriteLine("BreakSinglePlayerParty:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintForceAllReady(ForceAllReady msg, object tag)
        {
            Console.WriteLine("ForceAllReady:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintGameEnded(GameEnded msg, object tag)
        {
            Console.WriteLine("GameEnded:");
            Console.WriteLine("\tBreakParty={0}", msg.BreakParty); //System.Boolean has a toString()
            Console.WriteLine("\tNoCountSuccess={0}", msg.NoCountSuccess); //System.Boolean has a toString()
            Console.WriteLine("\tSuccessivePartyBonus={0}", msg.SuccessivePartyBonus); //System.Int32 has a toString()
            Console.WriteLine("\tSuccessivePartyCount={0}", msg.SuccessivePartyCount); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintGameStarted(GameStarted msg, object tag)
        {
            Console.WriteLine("GameStarted:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintGameStarting(GameStarting msg, object tag)
        {
            Console.WriteLine("GameStarting:");
            Console.WriteLine("\tFailReason={0}", msg.FailReason); //ServiceCore.PartyServiceOperations.GameStarting+FailReasonEnum
            Console.WriteLine("\tMinPartyCount={0}", msg.MinPartyCount); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintGameStartingFailed(GameStartingFailed msg, object tag)
        {
            Console.WriteLine("GameStartingFailed:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintInvitationRejected(InvitationRejected msg, object tag)
        {
            Console.WriteLine("InvitationRejected:");
            Console.WriteLine("\tInvitedCID={0}", msg.InvitedCID); //System.Int64 has a toString()
            Console.WriteLine("\tInvitedName={0}", msg.InvitedName); //System.String has a toString()
            Console.WriteLine("\tReason={0}", msg.Reason); //ServiceCore.EndPointNetwork.InvitationRejectReason
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintInvitePartyMember(InvitePartyMember msg, object tag)
        {
            Console.WriteLine("InvitePartyMember:");
            Console.WriteLine("\tInvitingCID={0}", msg.InvitingCID); //System.Int64 has a toString()
            Console.WriteLine("\tInvitedCID={0}", msg.InvitedCID); //System.Int64 has a toString()
            Console.WriteLine("\tInvitedName={0}", msg.InvitedName); //System.String has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintKickMember(KickMember msg, object tag)
        {
            Console.WriteLine("KickMember:");
            Console.WriteLine("\tMasterCID={0}", msg.MasterCID); //System.Int64 has a toString()
            Console.WriteLine("\tMemberSlot={0}", msg.MemberSlot); //System.Int32 has a toString()
            Console.WriteLine("\tNexonSN={0}", msg.NexonSN); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }
        public static void PrintQueryMicroPlayInfo(QueryMicroPlayInfo msg, object tag)
        {
            Console.WriteLine("QueryMicroPlayInfo:");
            Console.WriteLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            Console.WriteLine(QuestSectorInfosToString(msg.QuestSectorInfo, "QuestSectorInfos", 1)); //ServiceCore.EndPointNetwork.QuestSectorInfos
            Console.WriteLine("\tHardMode={0}", msg.HardMode); //System.Boolean has a toString()
            Console.WriteLine("\tSoloSector={0}", msg.SoloSector); //System.Int32 has a toString()
            Console.WriteLine("\tTimeLimit={0}", msg.TimeLimit); //System.Int32 has a toString()
            Console.WriteLine("\tQuestLevel={0}", msg.QuestLevel); //System.Int32 has a toString()
            Console.WriteLine("\tIsHuntingQuest={0}", msg.IsHuntingQuest); //System.Boolean has a toString()
            Console.WriteLine("\tInitGameTime={0}", msg.InitGameTime); //System.Int32 has a toString()
            Console.WriteLine("\tSectorMoveGameTime={0}", msg.SectorMoveGameTime); //System.Int32 has a toString()
            Console.WriteLine("\tIsGiantRaid={0}", msg.IsGiantRaid); //System.Boolean has a toString()
            Console.WriteLine("\tIsNoWaitingShip={0}", msg.IsNoWaitingShip); //System.Boolean has a toString()
            Console.WriteLine("\tItemLimit={0}", msg.ItemLimit); //System.String has a toString()
            Console.WriteLine("\tGearLimit={0}", msg.GearLimit); //System.String has a toString()
            Console.WriteLine("\tDifficulty={0}", msg.Difficulty); //System.Int32 has a toString()
            Console.WriteLine("\tIsTimerDecreasing={0}", msg.IsTimerDecreasing); //System.Boolean has a toString()
            Console.WriteLine("\tQuestStartedPlayerCount={0}", msg.QuestStartedPlayerCount); //System.Int32 has a toString()
            Console.WriteLine("\tFailReason={0}", msg.FailReason); //ServiceCore.MicroPlayServiceOperations.QueryMicroPlayInfo+FailReasonEnum
            Console.WriteLine("\tInitItemDropEntities={0}", msg.InitItemDropEntities); //System.Boolean has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRemovePlayer(RemovePlayer msg, object tag)
        {
            Console.WriteLine("RemovePlayer:");
            Console.WriteLine("\tSlotNumber={0}", msg.SlotNumber); //System.Int32 has a toString()
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }


        public static void PrintStartAutoFishing(StartAutoFishing msg, object tag)
        {
            Console.WriteLine("StartAutoFishing:");
            Console.WriteLine("\tSerialNumber={0}", msg.SerialNumber); //System.Int32 has a toString()
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            Console.WriteLine("\tArgument={0}", msg.Argument); //System.String has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintStartFailTimer(StartFailTimer msg, object tag)
        {
            Console.WriteLine("StartFailTimer:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintTransferHostInPlay(TransferHostInPlay msg, object tag)
        {
            Console.WriteLine("TransferHostInPlay:");
            Console.WriteLine("\tNewHostCID={0}", msg.NewHostCID); //System.Int64 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUseInventoryItemInQuest(UseInventoryItemInQuest msg, object tag)
        {
            Console.WriteLine("UseInventoryItemInQuest:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintChannelChat(ServiceCore.MMOChannelServiceOperations.ChannelChat msg, object tag)
        {
            Console.WriteLine("ChannelChat:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tMessage={0}", msg.Message); //System.String has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }


        public static void PrintLeaveChannel(LeaveChannel msg, object tag)
        {
            Console.WriteLine("LeaveChannel:");
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintRecommendChannel(ServiceCore.MMOChannelServiceOperations.RecommendChannel msg, object tag)
        {
            Console.WriteLine("RecommendChannel:");
            Console.WriteLine("\tChannelID={0}", msg.ChannelID); //System.Int64 has a toString()
            Console.WriteLine("\tServiceID={0}", msg.ServiceID); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintStartSharing(StartSharing msg, object tag)
        {
            Console.WriteLine("StartSharing:");
            Console.WriteLine("\tSharingInfo={0}", msg.SharingInfo); //ServiceCore.EndPointNetwork.SharingInfo has a toString()
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tTargetsCID=[{0}]", String.Join(",", msg.TargetsCID)); //System.Collections.Generic.List`1[System.Int64]
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryHostCID(QueryHostCID msg, object tag)
        {
            Console.WriteLine("QueryHostCID:");
            Console.WriteLine("\tAvailableHosts=[{0}]", String.Join(",", msg.AvailableHosts)); //System.Collections.Generic.List`1[System.Int64]
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryMemberState(QueryMemberState msg, object tag)
        {
            Console.WriteLine("QueryMemberState:");
            Console.WriteLine("\tSlot={0}", msg.Slot); //System.Int32 has a toString()
            Console.WriteLine("\tMemberState={0}", msg.MemberState); //ServiceCore.EndPointNetwork.ReadyState
            Console.WriteLine("\tPartyState={0}", msg.PartyState); //ServiceCore.EndPointNetwork.PartyInfoState
            Console.WriteLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            Console.WriteLine("\tIsInGameJoinAllowed={0}", msg.IsInGameJoinAllowed); //System.Boolean has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }
        public static void PrintQueryShipInfo(QueryShipInfo msg, object tag)
        {
            Console.WriteLine("QueryShipInfo:");
            Console.WriteLine("\tShipInfo={0}", msg.ShipInfo); //ServiceCore.EndPointNetwork.ShipInfo has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintShipLaunched(ShipLaunched msg, object tag)
        {
            Console.WriteLine("ShipLaunched:");
            Console.WriteLine("\tHostCID={0}", msg.HostCID); //System.Int64 has a toString()
            Console.WriteLine("\tMicroPlayID={0}", msg.MicroPlayID); //System.Int64 has a toString()
            Console.WriteLine("\tQuestLevel={0}", msg.QuestLevel); //System.Int32 has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintUseExtraStorageMessage(UseExtraStorageMessage msg, object tag)
        {
            Console.WriteLine("UseExtraStorageMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tStorageID={0}", msg.StorageID); //System.Byte has a toString()
        }

        public static void PrintUseInventoryItemOkMessage(UseInventoryItemOkMessage msg, object tag)
        {
            Console.WriteLine("UseInventoryItemOkMessage:");
            Console.WriteLine("\tUseItemClass={0}", msg.UseItemClass); //System.String has a toString()
        }

        public static void PrintUseInventoryItemFailedMessage(UseInventoryItemFailedMessage msg, object tag)
        {
            Console.WriteLine("UseInventoryItemFailedMessage:");
            Console.WriteLine("\tUseItemClass={0}", msg.UseItemClass); //System.String has a toString()
            Console.WriteLine("\tReason={0}", msg.Reason); //System.String has a toString()
        }

        public static void PrintVocationLearnSkillMessage(VocationLearnSkillMessage msg, object tag)
        {
            Console.WriteLine("VocationLearnSkillMessage:");
            Console.WriteLine("\tSkillID={0}", msg.SkillID); //System.String has a toString()
        }

        public static void PrintVocationLevelUpMessage(VocationLevelUpMessage msg, object tag)
        {
            Console.WriteLine("VocationLevelUpMessage:");
            Console.WriteLine("\tVocationClass={0}", msg.VocationClass); //ServiceCore.CharacterServiceOperations.VocationEnum
            Console.WriteLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
        }

        public static void PrintVocationTransformFinishedMessage(VocationTransformFinishedMessage msg, object tag)
        {
            Console.WriteLine("VocationTransformFinishedMessage:");
            Console.WriteLine("\tSlotNum={0}", msg.SlotNum); //System.Int32 has a toString()
            Console.WriteLine("\tTotalDamage={0}", msg.TotalDamage); //System.Int32 has a toString()
        }

        public static void PrintVocationTransformMessage(VocationTransformMessage msg, object tag)
        {
            Console.WriteLine("VocationTransformMessage:");
            Console.WriteLine("\tSlotNum={0}", msg.SlotNum); //System.Int32 has a toString()
            Console.WriteLine("\tTransformLevel={0}", msg.TransformLevel); //System.Int32 has a toString()
        }

        public static void PrintWaitTicketMessage(WaitTicketMessage msg, object tag)
        {
            Console.WriteLine("WaitTicketMessage:");
            Console.WriteLine("\tQueueSpeed={0}", msg.QueueSpeed); //System.Int32 has a toString()
            Console.WriteLine("\tPosition={0}", msg.Position); //System.Int32 has a toString()
        }

        public static void PrintUpdateWhisperFilterMessage(UpdateWhisperFilterMessage msg, object tag)
        {
            Console.WriteLine("UpdateWhisperFilterMessage:");
            Console.WriteLine("\tOperationType={0}", msg.OperationType); //System.Int32 has a toString()
            Console.WriteLine("\tTargetID={0}", msg.TargetID); //System.String has a toString()
            Console.WriteLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
        }

        public static void PrintRequestWhisperMessage(RequestWhisperMessage msg, object tag)
        {
            Console.WriteLine("RequestWhisperMessage:");
            Console.WriteLine("\tReceiver={0}", msg.Receiver); //System.String has a toString()
            Console.WriteLine("\tContents={0}", msg.Contents); //System.String has a toString()
        }

        public static void PrintWhisperMessage(WhisperMessage msg, object tag)
        {
            Console.WriteLine("WhisperMessage:");
            Console.WriteLine("\tSender={0}", msg.Sender); //System.String has a toString()
            Console.WriteLine("\tContents={0}", msg.Contents); //System.String has a toString()
        }

        public static void PrintWhisperFailMessage(WhisperFailMessage msg, object tag)
        {
            Console.WriteLine("WhisperFailMessage:");
            Console.WriteLine("\tToName={0}", msg.ToName); //System.String has a toString()
            Console.WriteLine("\tReason={0}", msg.Reason); //System.String has a toString()
        }

        public static void Print_CheatCommandMessage(_CheatCommandMessage msg, object tag)
        {
            Console.WriteLine("_CheatCommandMessage:");
            Console.WriteLine("\tService={0}", msg.Service); //System.String has a toString()
            Console.WriteLine("\tCommand={0}", msg.Command); //System.String has a toString()
            Console.WriteLine("\tIsEntityOp={0}", msg.IsEntityOp); //System.Boolean has a toString()
        }

        public static void Print_CheatSetCafeStatusMessage(_CheatSetCafeStatusMessage msg, object tag)
        {
            Console.WriteLine("_CheatSetCafeStatusMessage:");
            Console.WriteLine("\tCafeLevel={0}", msg.CafeLevel); //System.Int32 has a toString()
            Console.WriteLine("\tCafeType={0}", msg.CafeType); //System.Int32 has a toString()
            Console.WriteLine("\tSecureCode={0}", msg.SecureCode); //System.Int32 has a toString()
        }

        public static void Print_CheatSetLevelMessage(_CheatSetLevelMessage msg, object tag)
        {
            Console.WriteLine("_CheatSetLevelMessage:");
            Console.WriteLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            Console.WriteLine("\tExpPercent={0}", msg.ExpPercent); //System.Int32 has a toString()
        }

        public static void PrintUseConsumableMessage(UseConsumableMessage msg, object tag)
        {
            Console.WriteLine("UseConsumableMessage:");
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            Console.WriteLine("\tPart={0}", msg.Part); //System.Int32 has a toString()
            Console.WriteLine("\tSlot={0}", msg.Slot); //System.Int32 has a toString()
        }

        public static void PrintUpdateStorageStatusMessage(UpdateStorageStatusMessage msg, object tag)
        {
            Console.WriteLine("UpdateStorageStatusMessage:");
            Console.WriteLine("\tStorageNo={0}", msg.StorageNo); //System.Int32 has a toString()
            Console.WriteLine("\tStorageName={0}", msg.StorageName); //System.String has a toString()
            Console.WriteLine("\tStorageTag={0}", msg.StorageTag); //System.Int32 has a toString()
        }
        public static void PrintSwitchChannelMessage(SwitchChannelMessage msg, object tag)
        {
            Console.WriteLine("SwitchChannelMessage:");
            Console.WriteLine("\tOldChannelID={0}", msg.OldChannelID); //System.Int64 has a toString()
            Console.WriteLine("\tNewChannelID={0}", msg.NewChannelID); //System.Int64 has a toString()
        }

        public static void PrintSynthesisItemMessage(SynthesisItemMessage msg, object tag)
        {
            Console.WriteLine("SynthesisItemMessage:");
            Console.WriteLine("\tBaseItemID={0}", msg.BaseItemID); //System.Int64 has a toString()
            Console.WriteLine("\tLookItemID={0}", msg.LookItemID); //System.Int64 has a toString()
            Console.WriteLine("\tAdditionalItemClass={0}", msg.AdditionalItemClass); //System.String has a toString()
        }

        public static void PrintTeacherAssistJoin(TeacherAssistJoin msg, object tag)
        {
            Console.WriteLine("TeacherAssistJoin:");
            Console.WriteLine("\tShipID={0}", msg.ShipID); //System.Int64 has a toString()
        }

        public static void PrintTeacherRequest(TeacherRequest msg, object tag)
        {
            Console.WriteLine("TeacherRequest:");
        }

        public static void PrintTeacherRequestNotice(TeacherRequestNotice msg, object tag)
        {
            Console.WriteLine("TeacherRequestNotice:");
            Console.WriteLine("\tCharacterName={0}", msg.CharacterName); //System.String has a toString()
            Console.WriteLine("\tIsNotice={0}", msg.IsNotice); //System.Boolean has a toString()
        }

        public static void PrintTeacherRequestRespond(TeacherRequestRespond msg, object tag)
        {
            Console.WriteLine("TeacherRequestRespond:");
            Console.WriteLine("\tAccepted={0}", msg.Accepted); //System.Boolean has a toString()
        }

        public static void PrintTeacherRequestResult(TeacherRequestResult msg, object tag)
        {
            Console.WriteLine("TeacherRequestResult:");
            Console.WriteLine("\tAccepted={0}", msg.Accepted); //System.Boolean has a toString()
            Console.WriteLine("\tAcceptedUserName={0}", msg.AcceptedUserName); //System.String has a toString()
        }

        public static void PrintTestMessage(TestMessage msg, object tag)
        {
            Console.WriteLine("TestMessage:");
            Console.WriteLine("\tName={0}", msg.Name); //System.String has a toString()
            Console.WriteLine("\tID={0}", msg.ID); //System.Int32 has a toString()
        }

        public static void PrintPvpUnregisterMessage(PvpUnregisterMessage msg, object tag)
        {
            Console.WriteLine("PvpUnregisterMessage:");
        }

        public static void PrintTownCampfireEffectCSMessage(TownCampfireEffectCSMessage msg, object tag)
        {
            Console.WriteLine("TownCampfireEffectCSMessage:");
            Console.WriteLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            Console.WriteLine("\tType={0}", msg.Type); //System.Int32 has a toString()
        }

        public static void PrintTriggerEventMessage(TriggerEventMessage msg, object tag)
        {
            Console.WriteLine("TriggerEventMessage:");
            Console.WriteLine("\tEventName={0}", msg.EventName); //System.String has a toString()
            Console.WriteLine("\tArg={0}", msg.Arg); //System.String has a toString()
        }

        public static void PrintSkillCompletionMessage(SkillCompletionMessage msg, object tag)
        {
            Console.WriteLine("SkillCompletionMessage:");
            Console.WriteLine("\tSkillID={0}", msg.SkillID); //System.String has a toString()
            Console.WriteLine("\tSkillRank={0}", msg.SkillRank); //System.Int32 has a toString()
        }

        public static void PrintStartAutoFishingMessage(StartAutoFishingMessage msg, object tag)
        {
            Console.WriteLine("StartAutoFishingMessage:");
            Console.WriteLine("\tSerialNumber={0}", msg.SerialNumber); //System.Int32 has a toString()
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            Console.WriteLine("\tArgument={0}", msg.Argument); //System.String has a toString()
        }

        public static void PrintAutoFishingStartedMessage(AutoFishingStartedMessage msg, object tag)
        {
            Console.WriteLine("AutoFishingStartedMessage:");
            Console.WriteLine("\tSerialNumber={0}", msg.SerialNumber); //System.Int32 has a toString()
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            Console.WriteLine("\tModel={0}", msg.Model); //System.String has a toString()
            Console.WriteLine("\tActionTimeInSeconds={0}", msg.ActionTimeInSeconds); //System.Int32 has a toString()
            Console.WriteLine("\tTimeoutInSeconds={0}", msg.TimeoutInSeconds); //System.Int32 has a toString()
            Console.WriteLine("\tIsCaughtAtHit={0}", msg.IsCaughtAtHit); //System.Boolean has a toString()
            Console.WriteLine("\tIsCaughtAtMiss={0}", msg.IsCaughtAtMiss); //System.Boolean has a toString()
            Console.WriteLine("\tIsCaughtAtTimeout={0}", msg.IsCaughtAtTimeout); //System.Boolean has a toString()
        }

        public static void PrintCatchAutoFishMessage(CatchAutoFishMessage msg, object tag)
        {
            Console.WriteLine("CatchAutoFishMessage:");
            Console.WriteLine("\tSerialNumber={0}", msg.SerialNumber); //System.Int32 has a toString()
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            Console.WriteLine("\tCatchTimeInSeconds={0}", msg.CatchTimeInSeconds); //System.Int32 has a toString()
        }

        public static void PrintLostAutoFishMessage(LostAutoFishMessage msg, object tag)
        {
            Console.WriteLine("LostAutoFishMessage:");
            Console.WriteLine("\tSerialNumber={0}", msg.SerialNumber); //System.Int32 has a toString()
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
        }

        public static void PrintAutoFishingDeniedMessage(AutoFishingDeniedMessage msg, object tag)
        {
            Console.WriteLine("AutoFishingDeniedMessage:");
            Console.WriteLine("\tSerialNumber={0}", msg.SerialNumber); //System.Int32 has a toString()
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
        }

        public static void PrintCancelAutoFishingMessage(CancelAutoFishingMessage msg, object tag)
        {
            Console.WriteLine("CancelAutoFishingMessage:");
            Console.WriteLine("\tSerialNumber={0}", msg.SerialNumber); //System.Int32 has a toString()
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
        }

        public static void PrintStartTradeSessionMessage(StartTradeSessionMessage msg, object tag)
        {
            Console.WriteLine("StartTradeSessionMessage:");
        }

        public static void PrintStartTradeSessionResultMessage(StartTradeSessionResultMessage msg, object tag)
        {
            Console.WriteLine("StartTradeSessionResultMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
        }

        public static void PrintSharingCheckMessage(SharingCheckMessage msg, object tag)
        {
            Console.WriteLine("SharingCheckMessage:");
            Console.WriteLine("\tSharingCharacterName={0}", msg.SharingCharacterName); //System.String has a toString()
            Console.WriteLine("\tItemClassName={0}", msg.ItemClassName); //System.String has a toString()
            Console.WriteLine("\tStatusEffect={0}", msg.StatusEffect); //System.String has a toString()
            Console.WriteLine("\tEffectLevel={0}", msg.EffectLevel); //System.Int32 has a toString()
            Console.WriteLine("\tDurationSec={0}", msg.DurationSec); //System.Int32 has a toString()
        }

        public static void PrintSharingResponseMessage(SharingResponseMessage msg, object tag)
        {
            Console.WriteLine("SharingResponseMessage:");
            Console.WriteLine("\tAccepted={0}", msg.Accepted); //System.Boolean has a toString()
        }

        public static void PrintShipInfoMessage(ShipInfoMessage msg, object tag)
        {
            Console.WriteLine("ShipInfoMessage:");
            Console.WriteLine("\tShipInfo={0}", msg.ShipInfo); //ServiceCore.EndPointNetwork.ShipInfo has a toString()
        }

        public static void PrintShipNotLaunchedMessage(ShipNotLaunchedMessage msg, object tag)
        {
            Console.WriteLine("ShipNotLaunchedMessage: []");
        }

        public static void PrintServerCmdMessage(ServerCmdMessage msg, object tag)
        {
            Console.WriteLine("ServerCmdMessage:");
            Console.WriteLine("\tCommand={0}", msg.Command); //System.String has a toString()
            Console.WriteLine("\tReliable={0}", msg.Reliable); //System.Boolean has a toString()
        }

        public static void PrintSetLearningSkillMessage(SetLearningSkillMessage msg, object tag)
        {
            Console.WriteLine("SetLearningSkillMessage:");
            Console.WriteLine("\tSkillID={0}", msg.SkillID); //System.String has a toString()
        }

        public static void PrintSetRaidPartyMessage(SetRaidPartyMessage msg, object tag)
        {
            Console.WriteLine("SetRaidPartyMessage:");
            Console.WriteLine("\tIsRaidParty={0}", msg.IsRaidParty); //System.Boolean has a toString()
        }

        public static void PrintSetSecondPasswordMessage(SetSecondPasswordMessage msg, object tag)
        {
            Console.WriteLine("SetSecondPasswordMessage:");
            Console.WriteLine("\tNewPassword={0}", msg.NewPassword); //System.String has a toString()
        }

        public static void PrintSetSpSkillMessage(SetSpSkillMessage msg, object tag)
        {
            Console.WriteLine("SetSpSkillMessage:");
            Console.WriteLine("\tSlotID={0}", msg.SlotID); //System.Int32 has a toString()
            Console.WriteLine("\tSkillID={0}", msg.SkillID); //System.String has a toString()
        }

        public static void PrintQuestReinforceMessage(QuestReinforceMessage msg, object tag)
        {
            Console.WriteLine("QuestReinforceMessage:");
            Console.WriteLine("\tShipID={0}", msg.ShipID); //System.Int64 has a toString()
        }

        public static void PrintAddReinforcementMessage(AddReinforcementMessage msg, object tag)
        {
            Console.WriteLine("AddReinforcementMessage:");
            Console.WriteLine("\tUID={0}", msg.UID); //System.Int32 has a toString()
            Console.WriteLine("\tMemberInfo={0}", msg.MemberInfo); //ServiceCore.EndPointNetwork.GameJoinMemberInfo has a toString()
        }

        public static void PrintQuestReinforceGrantedMessage(QuestReinforceGrantedMessage msg, object tag)
        {
            Console.WriteLine("QuestReinforceGrantedMessage:");
            Console.WriteLine("\tGameInfo={0}", msg.GameInfo); //ServiceCore.EndPointNetwork.GameInfo has a toString()
            Console.WriteLine("\tUID={0}", msg.UID); //System.Int32 has a toString()
            Console.WriteLine("\tKey={0}", msg.Key); //System.Int32 has a toString()
        }

        public static void PrintReadyMessage(ReadyMessage msg, object tag)
        {
            Console.WriteLine("ReadyMessage:");
            Console.WriteLine("\tReady={0}", msg.Ready); //System.Byte has a toString()
        }

        public static void PrintPlayerRevivedMessage(PlayerRevivedMessage msg, object tag)
        {
            Console.WriteLine("PlayerRevivedMessage:");
            Console.WriteLine("\tCasterTag={0}", msg.CasterTag); //System.Int32 has a toString()
            Console.WriteLine("\tReviverTag={0}", msg.ReviverTag); //System.Int32 has a toString()
            Console.WriteLine("\tMethod={0}", msg.Method); //System.String has a toString()
        }

        public static void PrintReleaseLearningSkillMessage(ReleaseLearningSkillMessage msg, object tag)
        {
            Console.WriteLine("ReleaseLearningSkillMessage:");
        }

        public static void PrintReloadNpcScriptMessage(ReloadNpcScriptMessage msg, object tag)
        {
            Console.WriteLine("ReloadNpcScriptMessage:");
        }

        public static void PrintRequestCraftMessage(RequestCraftMessage msg, object tag)
        {
            Console.WriteLine("RequestCraftMessage:");
            Console.WriteLine("\tShopID={0}", msg.ShopID); //System.String has a toString()
            Console.WriteLine("\tOrder={0}", msg.Order); //System.Int32 has a toString()
            Console.WriteLine("\tCount={0}", msg.Count); //System.Int32 has a toString()
        }

        public static void PrintRequestShutDownMessage(RequestShutDownMessage msg, object tag)
        {
            Console.WriteLine("RequestShutDownMessage:");
            Console.WriteLine("\tDelay={0}", msg.Delay); //System.Int32 has a toString()
        }

        public static void PrintRequestStoryStatusMessage(RequestStoryStatusMessage msg, object tag)
        {
            Console.WriteLine("RequestStoryStatusMessage:");
        }

        public static void PrintReturnMailMessage(ReturnMailMessage msg, object tag)
        {
            Console.WriteLine("ReturnMailMessage:");
            Console.WriteLine("\tMailID={0}", msg.MailID); //System.Int64 has a toString()
        }

        public static void PrintSelectTitleMessage(SelectTitleMessage msg, object tag)
        {
            Console.WriteLine("SelectTitleMessage:");
            Console.WriteLine("\tTitle={0}", msg.Title); //System.Int32 has a toString()
        }

        public static void PrintSellItemMessage(SellItemMessage msg, object tag)
        {
            Console.WriteLine("SellItemMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tCount={0}", msg.Count); //System.Int32 has a toString()
        }

        public static void PrintMailReceivedMessage(MailReceivedMessage msg, object tag)
        {
            Console.WriteLine("MailReceivedMessage:");
            Console.WriteLine("\tFromName={0}", msg.FromName); //System.String has a toString()
            Console.WriteLine("\tMailType={0}", msg.MailType); //System.Byte has a toString()
        }

        public static void PrintMailSentMessage(MailSentMessage msg, object tag)
        {
            Console.WriteLine("MailSentMessage:");
            Console.WriteLine("\tErrorCode={0}", msg.ErrorCode); //System.Int32 has a toString()
        }

        public static void PrintMemberInfoMessage(MemberInfoMessage msg, object tag)
        {
            Console.WriteLine("MemberInfoMessage:");
            Console.WriteLine("\tUID={0}", msg.UID); //System.Int32 has a toString()
            Console.WriteLine("\tMemberInfo={0}", msg.MemberInfo); //ServiceCore.EndPointNetwork.GameJoinMemberInfo has a toString()
        }

        public static void PrintModifyStoryStatusMessage(ModifyStoryStatusMessage msg, object tag)
        {
            Console.WriteLine("ModifyStoryStatusMessage:");
            Console.WriteLine("\tToken={0}", msg.Token); //System.String has a toString()
            Console.WriteLine("\tState={0}", msg.State); //System.Int32 has a toString()
        }

        public static void PrintModifyStoryVariableMessage(ModifyStoryVariableMessage msg, object tag)
        {
            Console.WriteLine("ModifyStoryVariableMessage:");
            Console.WriteLine("\tStoryLine={0}", msg.StoryLine); //System.String has a toString()
            Console.WriteLine("\tKey={0}", msg.Key); //System.String has a toString()
            Console.WriteLine("\tValue={0}", msg.Value); //System.Int32 has a toString()
        }

        public static void PrintNoticeMessage(NoticeMessage msg, object tag)
        {
            Console.WriteLine("NoticeMessage:");
            Console.WriteLine("\tMessage={0}", msg.Message); //System.String has a toString()
        }

        public static void PrintNoticeShutDownMessage(NoticeShutDownMessage msg, object tag)
        {
            Console.WriteLine("NoticeShutDownMessage:");
            Console.WriteLine("\tDelay={0}", msg.Delay); //System.Int32 has a toString()
        }

        public static void PrintNotifyForceStartMessage(NotifyForceStartMessage msg, object tag)
        {
            Console.WriteLine("NotifyForceStartMessage:");
            Console.WriteLine("\tUntilForceStart={0}", msg.UntilForceStart); //System.Int32 has a toString()
        }

        public static void PrintPartyChatMessage(PartyChatMessage msg, object tag)
        {
            Console.WriteLine("PartyChatMessage:");
            Console.WriteLine("\tSenderName={0}", msg.SenderName); //System.String has a toString()
            Console.WriteLine("\tMessage={0}", msg.Message); //System.String has a toString()
            Console.WriteLine("\tisFakeTownChat={0}", msg.isFakeTownChat); //System.Boolean has a toString()
        }

        public static void PrintPartyChatSendMessage(PartyChatSendMessage msg, object tag)
        {
            Console.WriteLine("PartyChatSendMessage:");
            Console.WriteLine("\tMessage={0}", msg.Message); //System.String has a toString()
        }

        public static void PrintPartyInvitationAcceptFailedMessage(PartyInvitationAcceptFailedMessage msg, object tag)
        {
            Console.WriteLine("PartyInvitationAcceptFailedMessage:");
        }

        public static void PrintPartyInvitationAcceptMessage(PartyInvitationAcceptMessage msg, object tag)
        {
            Console.WriteLine("PartyInvitationAcceptMessage:");
            Console.WriteLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
        }

        public static void PrintPartyInvitationRejectedMessage(PartyInvitationRejectedMessage msg, object tag)
        {
            Console.WriteLine("PartyInvitationRejectedMessage:");
            Console.WriteLine("\tInvitedName={0}", msg.InvitedName); //System.String has a toString()
            Console.WriteLine("\tReason={0}", msg.Reason); //System.Int32 has a toString()
        }

        public static void PrintPartyInvitationRejectMessage(PartyInvitationRejectMessage msg, object tag)
        {
            Console.WriteLine("PartyInvitationRejectMessage:");
            Console.WriteLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
        }

        public static void PrintPartyInvitedMessage(PartyInvitedMessage msg, object tag)
        {
            Console.WriteLine("PartyInvitedMessage:");
            Console.WriteLine("\tHostName={0}", msg.HostName); //System.String has a toString()
            Console.WriteLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
        }

        public static void PrintPartyInviteMessage(PartyInviteMessage msg, object tag)
        {
            Console.WriteLine("PartyInviteMessage:");
            Console.WriteLine("\tInvitedName={0}", msg.InvitedName); //System.String has a toString()
        }

        public static void PrintPartyOptionMessage(PartyOptionMessage msg, object tag)
        {
            Console.WriteLine("PartyOptionMessage:");
            Console.WriteLine("\tRestTime={0}", msg.RestTime); //System.Int32 has a toString()
        }

        public static void PrintPingMessage(PingMessage msg, object tag)
        {
            Console.WriteLine("PingMessage:");
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
        }

        public static void PrintPlayerKilledMessage(PlayerKilledMessage msg, object tag)
        {
            Console.WriteLine("PlayerKilledMessage:");
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
        }

        public static void PrintPongMessage(PongMessage msg, object tag)
        {
            Console.WriteLine("PongMessage:");
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
        }

        public static void PrintQueryAPMessage(QueryAPMessage msg, object tag)
        {
            Console.WriteLine("QueryAPMessage:");
        }

        public static void PrintQueryEquipmentMessage(QueryEquipmentMessage msg, object tag)
        {
            Console.WriteLine("QueryEquipmentMessage:");
        }

        public static void PrintQueryMailListMessage(QueryMailListMessage msg, object tag)
        {
            Console.WriteLine("QueryMailListMessage:");
        }

        public static void PrintQueryNpcListMessage(QueryNpcListMessage msg, object tag)
        {
            Console.WriteLine("QueryNpcListMessage:");
        }

        public static void PrintQueryQuestsMessage(QueryQuestsMessage msg, object tag)
        {
            Console.WriteLine("QueryQuestsMessage:");
        }

        public static void PrintQuerySectorEntitiesMessage(QuerySectorEntitiesMessage msg, object tag)
        {
            Console.WriteLine("QuerySectorEntitiesMessage:");
            Console.WriteLine("\tSector={0}", msg.Sector); //System.String has a toString()
        }

        public static void PrintQueryShipInfoMessage(QueryShipInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryShipInfoMessage:");
            Console.WriteLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
        }

        public static void PrintQueryShipListMessage(QueryShipListMessage msg, object tag)
        {
            Console.WriteLine("QueryShipListMessage:");
        }

        public static void PrintQuerySkillListMessage(QuerySkillListMessage msg, object tag)
        {
            Console.WriteLine("QuerySkillListMessage:");
        }

        public static void PrintQueryStatMessage(QueryStatMessage msg, object tag)
        {
            Console.WriteLine("QueryStatMessage:");
        }

        public static void PrintQueryStoryGuideMessage(QueryStoryGuideMessage msg, object tag)
        {
            Console.WriteLine("QueryStoryGuideMessage:");
        }

        public static void PrintQueryStoryLinesMessage(QueryStoryLinesMessage msg, object tag)
        {
            Console.WriteLine("QueryStoryLinesMessage:");
        }

        public static void PrintQueryStoryVariablesMessage(QueryStoryVariablesMessage msg, object tag)
        {
            Console.WriteLine("QueryStoryVariablesMessage:");
        }

        public static void PrintLoginFailMessage(LoginFailMessage msg, object tag)
        {
            Console.WriteLine("LoginFailMessage:");
            Console.WriteLine("\tReason={0}", msg.Reason); //System.Int32 has a toString()
            Console.WriteLine("\tBannedReason={0}", msg.BannedReason); //System.String has a toString()
        }

        public static void PrintCreateCharacterFailMessage(CreateCharacterFailMessage msg, object tag)
        {
            Console.WriteLine("CreateCharacterFailMessage:");
            Console.WriteLine("\tErrorCode={0}", msg.ErrorCode); //System.Int32 has a toString()
        }

        public static void PrintPurchaseCharacterSlotMessage(PurchaseCharacterSlotMessage msg, object tag)
        {
            Console.WriteLine("PurchaseCharacterSlotMessage:");
            Console.WriteLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
            Console.WriteLine("\tIsPremiumSlot={0}", msg.IsPremiumSlot); //System.Boolean has a toString()
            Console.WriteLine("\tIsCredit={0}", msg.IsCredit); //System.Boolean has a toString()
        }

        public static void PrintQueryCharacterNameChangeMessage(QueryCharacterNameChangeMessage msg, object tag)
        {
            Console.WriteLine("QueryCharacterNameChangeMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tRequestName={0}", msg.RequestName); //System.String has a toString()
            Console.WriteLine("\tIsTrans={0}", msg.IsTrans); //System.Boolean has a toString()
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
        }

        public static void PrintCharacterNameChangeMessage(CharacterNameChangeMessage msg, object tag)
        {
            Console.WriteLine("CharacterNameChangeMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tName={0}", msg.Name); //System.String has a toString()
            Console.WriteLine("\tIsTrans={0}", msg.IsTrans); //System.Boolean has a toString()
        }

        public static void PrintLoggedOutMessage(LoggedOutMessage msg, object tag)
        {
            Console.WriteLine("LoggedOutMessage:");
        }

        public static void PrintHackShieldRespondMessage(HackShieldRespondMessage msg, object tag)
        {
            Console.WriteLine("HackShieldRespondMessage:");
            //Console.WriteLine("\tRespond={0}",msg.Respond); //System.Byte[]
            Console.WriteLine("\tHackCode={0}", msg.HackCode); //System.Int32 has a toString()
            Console.WriteLine("\tHackParam={0}", msg.HackParam); //System.String has a toString()
            Console.WriteLine("\tCheckSum={0}", msg.CheckSum); //System.Int64 has a toString()
        }

        public static void PrintHostInfoMessage(HostInfoMessage msg, object tag)
        {
            Console.WriteLine("HostInfoMessage:");
            Console.WriteLine("\tGameInfo={0}", msg.GameInfo); //ServiceCore.EndPointNetwork.GameInfo has a toString()
            Console.WriteLine("\tUID={0}", msg.UID); //System.Int32 has a toString()
            Console.WriteLine("\tKey={0}", msg.Key); //System.Int32 has a toString()
            Console.WriteLine("\tSkipLobby={0}", msg.SkipLobby); //System.Boolean has a toString()
            Console.WriteLine("\tIsTransferToDS={0}", msg.IsTransferToDS); //System.Boolean has a toString()
        }

        public static void PrintJoinShipMessage(JoinShipMessage msg, object tag)
        {
            Console.WriteLine("JoinShipMessage:");
            Console.WriteLine("\tShipID={0}", msg.ShipID); //System.Int64 has a toString()
            Console.WriteLine("\tIsAssist={0}", msg.IsAssist); //System.Boolean has a toString()
            Console.WriteLine("\tIsInTownWithShipInfo={0}", msg.IsInTownWithShipInfo); //System.Boolean has a toString()
            Console.WriteLine("\tIsDedicatedServer={0}", msg.IsDedicatedServer); //System.Boolean has a toString()
            Console.WriteLine("\tIsNewbieRecommend={0}", msg.IsNewbieRecommend); //System.Boolean has a toString()
        }

        public static void PrintKickMessage(KickMessage msg, object tag)
        {
            Console.WriteLine("KickMessage:");
            Console.WriteLine("\tSlot={0}", msg.Slot); //System.Int32 has a toString()
            Console.WriteLine("\tNexonSN={0}", msg.NexonSN); //System.Int32 has a toString()
        }

        public static void PrintKilledMessage(KilledMessage msg, object tag)
        {
            Console.WriteLine("KilledMessage:");
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            Console.WriteLine("\tTargetKind={0}", msg.TargetKind); //System.Int32 has a toString()
            Console.WriteLine("\tTargetName={0}", msg.TargetName); //System.String has a toString()
        }

        public static void PrintLearningSkillChangedMessage(LearningSkillChangedMessage msg, object tag)
        {
            Console.WriteLine("LearningSkillChangedMessage:");
            Console.WriteLine("\tSkillID={0}", msg.SkillID); //System.String has a toString()
            Console.WriteLine("\tAP={0}", msg.AP); //System.Int32 has a toString()
        }

        public static void PrintDestroyItemMessage(DestroyItemMessage msg, object tag)
        {
            Console.WriteLine("DestroyItemMessage:");
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            Console.WriteLine("\tNum={0}", msg.Num); //System.Int32 has a toString()
        }

        public static void PrintGameStartedMessage(GameStartedMessage msg, object tag)
        {
            Console.WriteLine("GameStartedMessage: []");
        }

        public static void PrintReturnToShipMessage(ReturnToShipMessage msg, object tag)
        {
            Console.WriteLine("ReturnToShipMessage:");
        }

        public static void PrintCompleteSkillMessage(CompleteSkillMessage msg, object tag)
        {
            Console.WriteLine("CompleteSkillMessage:");
        }

        public static void PrintCreateItemMessage(CreateItemMessage msg, object tag)
        {
            Console.WriteLine("CreateItemMessage:");
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            Console.WriteLine("\tItemNum={0}", msg.ItemNum); //System.Int32 has a toString()
        }

        public static void PrintCreateShipMessage(CreateShipMessage msg, object tag)
        {
            Console.WriteLine("CreateShipMessage:");
            Console.WriteLine("\tQuestName={0}", msg.QuestName); //System.String has a toString()
        }

        public static void PrintCustomSectorQuestMessage(CustomSectorQuestMessage msg, object tag)
        {
            Console.WriteLine("CustomSectorQuestMessage:");
            Console.WriteLine("\tTargetSector={0}", msg.TargetSector); //System.String has a toString()
        }

        public static void PrintDeleteCharacterMessage(DeleteCharacterMessage msg, object tag)
        {
            Console.WriteLine("DeleteCharacterMessage:");
            Console.WriteLine("\tName={0}", msg.Name); //System.String has a toString()
            Console.WriteLine("\tCharacterSN={0}", msg.CharacterSN); //System.Int32 has a toString()
        }

        public static void PrintPvpRegisterMessage(PvpRegisterMessage msg, object tag)
        {
            Console.WriteLine("PvpRegisterMessage:");
            Console.WriteLine("\tPvpMode={0}", msg.PvpMode); //System.String has a toString()
            Console.WriteLine("\tCheat={0}", msg.Cheat); //ServiceCore.EndPointNetwork.PvpRegisterCheat
            Console.WriteLine("\tChannelID={0}", msg.ChannelID); //System.Int32 has a toString()
        }

        public static void PrintPvpMemberInfoMessage(PvpMemberInfoMessage msg, object tag)
        {
            Console.WriteLine("PvpMemberInfoMessage:");
            Console.WriteLine("\tMemberInfo={0}", msg.MemberInfo); //ServiceCore.EndPointNetwork.GameJoinMemberInfo has a toString()
            Console.WriteLine("\tTeamID={0}", msg.TeamID); //System.Int32 has a toString()
        }

        public static void PrintPvpStartGameMessage(PvpStartGameMessage msg, object tag)
        {
            Console.WriteLine("PvpStartGameMessage:");
        }

        public static void PrintUnequippablePartsInfoMessage(UnequippablePartsInfoMessage msg, object tag)
        {
            Console.WriteLine("UnequippablePartsInfoMessage:");
            Console.WriteLine("\tParts=[{0}]",String.Join(",",msg.Parts)); //System.Collections.Generic.ICollection`1[System.Int32]
        }

        public static void PrintGetMailItemCompletedMessage(GetMailItemCompletedMessage msg, object tag)
        {
            Console.WriteLine("GetMailItemCompletedMessage:");
            Console.WriteLine("\tMailID={0}", msg.MailID); //System.Int64 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Byte has a toString()
        }

        public static void PrintInsertTradeOrderMessage(InsertTradeOrderMessage msg, object tag)
        {
            Console.WriteLine("InsertTradeOrderMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tNum={0}", msg.Num); //System.Int32 has a toString()
            Console.WriteLine("\tDurationMin={0}", msg.DurationMin); //System.Int32 has a toString()
            Console.WriteLine("\tUnitPrice={0}", msg.UnitPrice); //System.Int32 has a toString()
            Console.WriteLine("\tTradeType={0}", msg.TradeType); //System.Byte has a toString()
        }

        public static void PrintInsertTradeOrderResultMessage(InsertTradeOrderResultMessage msg, object tag)
        {
            Console.WriteLine("InsertTradeOrderResultMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
        }

        public static void PrintInsertTradeTirOrderMessage(InsertTradeTirOrderMessage msg, object tag)
        {
            Console.WriteLine("InsertTradeTirOrderMessage:");
            Console.WriteLine("\tNum={0}", msg.Num); //System.Int32 has a toString()
            Console.WriteLine("\tDurationMin={0}", msg.DurationMin); //System.Int32 has a toString()
            Console.WriteLine("\tUnitPrice={0}", msg.UnitPrice); //System.Int32 has a toString()
        }

        public static void PrintJoinChannelMessage(JoinChannelMessage msg, object tag)
        {
            Console.WriteLine("JoinChannelMessage:");
            Console.WriteLine("\tChannelID={0}", msg.ChannelID); //System.Int64 has a toString()
        }

        public static void PrintLeaveChannelMessage(LeaveChannelMessage msg, object tag)
        {
            Console.WriteLine("LeaveChannelMessage:");
            Console.WriteLine("\tChannelID={0}", msg.ChannelID); //System.Int64 has a toString()
        }

        public static void PrintPayCoinMessage(PayCoinMessage msg, object tag)
        {
            Console.WriteLine("PayCoinMessage:");
            Console.WriteLine("\tCoinType={0}", msg.CoinType); //ServiceCore.EndPointNetwork.CoinType
            Console.WriteLine("\tReceiverSlot={0}", msg.ReceiverSlot); //System.Int32 has a toString()
            Console.WriteLine("\tCoinSlot={0}", msg.CoinSlot); //System.Int32 has a toString()
            Console.WriteLine("\tIsInsert={0}", msg.IsInsert); //System.Boolean has a toString()
        }

        public static void PrintPvpEndGameMessage(PvpEndGameMessage msg, object tag)
        {
            Console.WriteLine("PvpEndGameMessage:");
        }

        public static void PrintNexonSNByNameMessage(NexonSNByNameMessage msg, object tag)
        {
            Console.WriteLine("NexonSNByNameMessage:");
            Console.WriteLine("\tQueryID={0}", msg.QueryID); //System.Int64 has a toString()
            Console.WriteLine("\tUID={0}", msg.UID); //System.Int32 has a toString()
        }

        public static void PrintQueryAddBattleInventoryMessage(QueryAddBattleInventoryMessage msg, object tag)
        {
            Console.WriteLine("QueryAddBattleInventoryMessage:");
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            Console.WriteLine("\tSlotNum={0}", msg.SlotNum); //System.Int32 has a toString()
            Console.WriteLine("\tIsFree={0}", msg.IsFree); //System.Boolean has a toString()
        }

        public static void PrintQueryCashShopGiftSenderMessage(QueryCashShopGiftSenderMessage msg, object tag)
        {
            Console.WriteLine("QueryCashShopGiftSenderMessage:");
            Console.WriteLine("\tOrderNo={0}", msg.OrderNo); //System.Int32 has a toString()
        }

        public static void PrintQueryFishingResultMessage(QueryFishingResultMessage msg, object tag)
        {
            Console.WriteLine("QueryFishingResultMessage:");
        }

        public static void PrintQueryNewRecipesMessage(QueryNewRecipesMessage msg, object tag)
        {
            Console.WriteLine("QueryNewRecipesMessage:");
        }

        public static void PrintQueryNexonSNByNameMessage(QueryNexonSNByNameMessage msg, object tag)
        {
            Console.WriteLine("QueryNexonSNByNameMessage:");
            Console.WriteLine("\tQueryID={0}", msg.QueryID); //System.Int64 has a toString()
            Console.WriteLine("\tcName={0}", msg.cName); //System.String has a toString()
        }

        public static void PrintClientCmdMessage(ClientCmdMessage msg, object tag)
        {
            Console.WriteLine("ClientCmdMessage:");
            Console.WriteLine("\tCommand={0}", msg.Command); //System.String has a toString()
        }

        public static void PrintDeleteCharacterResultMessage(DeleteCharacterResultMessage msg, object tag)
        {
            Console.WriteLine("DeleteCharacterResultMessage:");
            Console.WriteLine("\tCharacterSN={0}", msg.CharacterSN); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.DeleteCharacterResult
            Console.WriteLine("\tRemainTimeSec={0}", msg.RemainTimeSec); //System.Int32 has a toString()
        }

        public static void PrintDyeItemMessage(DyeItemMessage msg, object tag)
        {
            Console.WriteLine("DyeItemMessage:");
            Console.WriteLine("\tStartNewSession={0}", msg.StartNewSession); //System.Boolean has a toString()
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tIsPremium={0}", msg.IsPremium); //System.Boolean has a toString()
        }

        public static void PrintRequestSetRankFavoriteInfoMessage(RequestSetRankFavoriteInfoMessage msg, object tag)
        {
            Console.WriteLine("RequestSetRankFavoriteInfoMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tRankID={0}", msg.RankID); //System.String has a toString()
            Console.WriteLine("\tIsAddition={0}", msg.IsAddition); //System.Boolean has a toString()
        }

        public static void PrintRequestSetRankGoalInfoMessage(RequestSetRankGoalInfoMessage msg, object tag)
        {
            Console.WriteLine("RequestSetRankGoalInfoMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tRankID={0}", msg.RankID); //System.String has a toString()
        }

        public static void PrintRemoveExpiredCashItemMessage(RemoveExpiredCashItemMessage msg, object tag)
        {
            Console.WriteLine("RemoveExpiredCashItemMessage:");
        }

        public static void PrintQueryRankAllInfoMessage(QueryRankAllInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryRankAllInfoMessage:");
            Console.WriteLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintQueryRankCharacterInfoMessage(QueryRankCharacterInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryRankCharacterInfoMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintQueryRankFavoritesInfoMessage(QueryRankFavoritesInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryRankFavoritesInfoMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintQueryRankGoalInfoMessage(QueryRankGoalInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryRankGoalInfoMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tEventID={0}", msg.EventID); //System.Int32 has a toString()
            Console.WriteLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintQueryRankHeavyFavoritesInfoMessage(QueryRankHeavyFavoritesInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryRankHeavyFavoritesInfoMessage:");
        }

        public static void PrintManufactureLevelUpMessage(ManufactureLevelUpMessage msg, object tag)
        {
            Console.WriteLine("ManufactureLevelUpMessage:");
            Console.WriteLine("\tMID={0}", msg.MID); //System.String has a toString()
            Console.WriteLine("\tGrade={0}", msg.Grade); //System.Int32 has a toString()
        }

        public static void PrintIdentifyFailed(IdentifyFailed msg, object tag)
        {
            Console.WriteLine("IdentifyFailed:");
            Console.WriteLine("\tErrorMessage={0}", msg.ErrorMessage); //System.String has a toString()
        }

        public static void PrintChannelChanged(ChannelChanged msg, object tag)
        {
            Console.WriteLine("ChannelChanged:");
            Console.WriteLine("\tChannelID={0}", msg.ChannelID); //System.Int64 has a toString()
        }

        public static void PrintChat(Chat msg, object tag)
        {
            Console.WriteLine("Chat:");
            Console.WriteLine("\tMessage={0}", msg.Message); //System.String has a toString()
        }

        public static void PrintLeave(Leave msg, object tag)
        {
            Console.WriteLine("Leave:");
        }

        public static void PrintNotifyChat(NotifyChat msg, object tag)
        {
            Console.WriteLine("NotifyChat:");
            Console.WriteLine("\tName={0}", msg.Name); //System.String has a toString()
            Console.WriteLine("\tMessage={0}", msg.Message); //System.String has a toString()
        }

        public static void PrintPvpCommandMessage(PvpCommandMessage msg, object tag)
        {
            Console.WriteLine("PvpCommandMessage:");
            Console.WriteLine("\tCommandInt={0}", msg.CommandInt); //System.Int32 has a toString()
            Console.WriteLine("\tArg={0}", msg.Arg); //System.String has a toString()
        }

        public static void PrintHostRestartedMessage(HostRestartedMessage msg, object tag)
        {
            Console.WriteLine("HostRestartedMessage:");
            Console.WriteLine("\tGameInfo={0}", msg.GameInfo); //ServiceCore.EndPointNetwork.GameInfo has a toString()
        }

        public static void PrintHostRestartingMessage(HostRestartingMessage msg, object tag)
        {
            Console.WriteLine("HostRestartingMessage:");
        }

        public static void PrintEnchantItemMessage(EnchantItemMessage msg, object tag)
        {
            Console.WriteLine("EnchantItemMessage:");
            Console.WriteLine("\tOpenNewSession={0}", msg.OpenNewSession); //System.Boolean has a toString()
            Console.WriteLine("\tTargetItemID={0}", msg.TargetItemID); //System.Int64 has a toString()
            Console.WriteLine("\tEnchantScrollItemID={0}", msg.EnchantScrollItemID); //System.Int64 has a toString()
            Console.WriteLine("\tIndestructibleScrollItemID={0}", msg.IndestructibleScrollItemID); //System.Int64 has a toString()
            Console.WriteLine("\tDiceItemClass={0}", msg.DiceItemClass); //System.String has a toString()
        }

        public static void PrintTradePurchaseItemMessage(TradePurchaseItemMessage msg, object tag)
        {
            Console.WriteLine("TradePurchaseItemMessage:");
            Console.WriteLine("\tTID={0}", msg.TID); //System.Int64 has a toString()
            Console.WriteLine("\tPurchaseCount={0}", msg.PurchaseCount); //System.Int32 has a toString()
            Console.WriteLine("\tUniqueNumber={0}", msg.UniqueNumber); //System.Int32 has a toString()
        }

        public static void PrintTradePurchaseItemResultMessage(TradePurchaseItemResultMessage msg, object tag)
        {
            Console.WriteLine("TradePurchaseItemResultMessage:");
            Console.WriteLine("\tUniqueNumber={0}", msg.UniqueNumber); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
            Console.WriteLine("\tLeftNumber={0}", msg.LeftNumber); //System.Int32 has a toString()
        }

        public static void PrintTradeRequestMyItemMessage(TradeRequestMyItemMessage msg, object tag)
        {
            Console.WriteLine("TradeRequestMyItemMessage:");
        }

        public static void PrintMovePetSlotMessage(MovePetSlotMessage msg, object tag)
        {
            Console.WriteLine("MovePetSlotMessage:");
            Console.WriteLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
            Console.WriteLine("\tDestSlotNo={0}", msg.DestSlotNo); //System.Int32 has a toString()
        }

        public static void PrintRemovePetMessage(RemovePetMessage msg, object tag)
        {
            Console.WriteLine("RemovePetMessage:");
            Console.WriteLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
        }

        public static void PrintSelectPetMessage(SelectPetMessage msg, object tag)
        {
            Console.WriteLine("SelectPetMessage:");
            Console.WriteLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
        }

        public static void PrintQueryPetListMessage(QueryPetListMessage msg, object tag)
        {
            Console.WriteLine("QueryPetListMessage:");
        }

        public static void PrintPetRevivedMessage(PetRevivedMessage msg, object tag)
        {
            Console.WriteLine("PetRevivedMessage:");
            Console.WriteLine("\tCasterTag={0}", msg.CasterTag); //System.Int32 has a toString()
            Console.WriteLine("\tReviverTag={0}", msg.ReviverTag); //System.Int32 has a toString()
            Console.WriteLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
            Console.WriteLine("\tMethod={0}", msg.Method); //System.String has a toString()
        }

        public static void PrintPetKilledMessage(PetKilledMessage msg, object tag)
        {
            Console.WriteLine("PetKilledMessage:");
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            Console.WriteLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
        }

        public static void PrintRemovePetSkillMessage(RemovePetSkillMessage msg, object tag)
        {
            Console.WriteLine("RemovePetSkillMessage:");
            Console.WriteLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
            Console.WriteLine("\tPetSkillID={0}", msg.PetSkillID); //System.Int32 has a toString()
        }

        public static void PrintPetCreateNameCheckMessage(PetCreateNameCheckMessage msg, object tag)
        {
            Console.WriteLine("PetCreateNameCheckMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tPetName={0}", msg.PetName); //System.String has a toString()
        }

        public static void PrintPetCreateNameCheckResultMessage(PetCreateNameCheckResultMessage msg, object tag)
        {
            Console.WriteLine("PetCreateNameCheckResultMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tPetName={0}", msg.PetName); //System.String has a toString()
            Console.WriteLine("\tIsSuccess={0}", msg.IsSuccess); //System.Boolean has a toString()
        }

        public static void PrintPetCreateMessage(PetCreateMessage msg, object tag)
        {
            Console.WriteLine("PetCreateMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tPetName={0}", msg.PetName); //System.String has a toString()
        }

        public static void PrintUpdateCurrentPetMessage(UpdateCurrentPetMessage msg, object tag)
        {
            Console.WriteLine("UpdateCurrentPetMessage:");
            Console.WriteLine("\tUID={0}", msg.UID); //System.Int32 has a toString()
            Console.WriteLine("\tCurrentPet={0}", msg.CurrentPet); //ServiceCore.EndPointNetwork.PetStatusInfo has a toString()
        }

        public static void PrintRequestPetFoodShareMessage(RequestPetFoodShareMessage msg, object tag)
        {
            Console.WriteLine("RequestPetFoodShareMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
        }

        public static void PrintRequestPetFoodUnshareMessage(RequestPetFoodUnshareMessage msg, object tag)
        {
            Console.WriteLine("RequestPetFoodUnshareMessage:");
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
        }

        public static void PrintPetChangeNameCheckMessage(PetChangeNameCheckMessage msg, object tag)
        {
            Console.WriteLine("PetChangeNameCheckMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
            Console.WriteLine("\tPetName={0}", msg.PetName); //System.String has a toString()
        }

        public static void PrintPetChangeNameCheckResultMessage(PetChangeNameCheckResultMessage msg, object tag)
        {
            Console.WriteLine("PetChangeNameCheckResultMessage:");
            Console.WriteLine("\tIsSuccess={0}", msg.IsSuccess); //System.Boolean has a toString()
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
            Console.WriteLine("\tPetName={0}", msg.PetName); //System.String has a toString()
            Console.WriteLine("\tResultType={0}", msg.ResultType); //System.String has a toString()
        }

        public static void PrintPetChangeNameMessage(PetChangeNameMessage msg, object tag)
        {
            Console.WriteLine("PetChangeNameMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
            Console.WriteLine("\tPetName={0}", msg.PetName); //System.String has a toString()
        }

        public static void PrintTcProtectRespondMessage(TcProtectRespondMessage msg, object tag)
        {
            Console.WriteLine("TcProtectRespondMessage:");
            Console.WriteLine("\tMd5Check={0}", msg.Md5Check); //System.Int32 has a toString()
            Console.WriteLine("\tImpressCheck={0}", msg.ImpressCheck); //System.Int32 has a toString()
        }

        public static void PrintTradeCancelMyItemMessage(TradeCancelMyItemMessage msg, object tag)
        {
            Console.WriteLine("TradeCancelMyItemMessage:");
            Console.WriteLine("\tTID={0}", msg.TID); //System.Int64 has a toString()
            Console.WriteLine("\tUniqueNumber={0}", msg.UniqueNumber); //System.Int32 has a toString()
        }

        public static void PrintTradeCancelMyItemResultMessage(TradeCancelMyItemResultMessage msg, object tag)
        {
            Console.WriteLine("TradeCancelMyItemResultMessage:");
            Console.WriteLine("\tUniqueNumber={0}", msg.UniqueNumber); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
        }

        public static void PrintDetailOption(DetailOption msg, object tag)
        {
            Console.WriteLine("DetailOption:");
            Console.WriteLine("\tKey={0}", msg.Key); //System.String has a toString()
            Console.WriteLine("\tValue={0}", msg.Value); //System.Int32 has a toString()
            Console.WriteLine("\tSearchType={0}", msg.SearchType); //System.Byte has a toString()
        }

        public static void PrintResetSkillMessage(ResetSkillMessage msg, object tag)
        {
            Console.WriteLine("ResetSkillMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tSkillID={0}", msg.SkillID); //System.String has a toString()
            Console.WriteLine("\tAfterRank={0}", msg.AfterRank); //System.Int32 has a toString()
        }

        public static void PrintUseMegaphoneMessage(UseMegaphoneMessage msg, object tag)
        {
            Console.WriteLine("UseMegaphoneMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tMessageType={0}", msg.MessageType); //System.Int32 has a toString()
            Console.WriteLine("\tMessage={0}", msg.Message); //System.String has a toString()
        }

        public static void PrintMegaphoneMessage(MegaphoneMessage msg, object tag)
        {
            Console.WriteLine("MegaphoneMessage:");
            Console.WriteLine("\tMessageType={0}", msg.MessageType); //System.Int32 has a toString()
            Console.WriteLine("\tSenderName={0}", msg.SenderName); //System.String has a toString()
            Console.WriteLine("\tMessage={0}", msg.Message); //System.String has a toString()
        }

        public static void PrintGuildNoticeEditMessage(GuildNoticeEditMessage msg, object tag)
        {
            Console.WriteLine("GuildNoticeEditMessage:");
            Console.WriteLine("\tText={0}", msg.Text); //System.String has a toString()
        }

        public static void PrintGuildStoryRequestMessage(GuildStoryRequestMessage msg, object tag)
        {
            Console.WriteLine("GuildStoryRequestMessage:");
        }

        public static void PrintDyeAmpleUsedMessage(DyeAmpleUsedMessage msg, object tag)
        {
            Console.WriteLine("DyeAmpleUsedMessage:");
            Console.WriteLine("\tColorTable={0}", msg.ColorTable); //System.String has a toString()
            Console.WriteLine("\tSeed1={0}", msg.Seed1); //System.Int32 has a toString()
            Console.WriteLine("\tSeed2={0}", msg.Seed2); //System.Int32 has a toString()
            Console.WriteLine("\tSeed3={0}", msg.Seed3); //System.Int32 has a toString()
            Console.WriteLine("\tSeed4={0}", msg.Seed4); //System.Int32 has a toString()
        }

        public static void PrintDyeItemCashMessage(DyeItemCashMessage msg, object tag)
        {
            Console.WriteLine("DyeItemCashMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tAmpleID={0}", msg.AmpleID); //System.Int64 has a toString()
            Console.WriteLine("\tPart={0}", msg.Part); //System.Int32 has a toString()
        }

        public static void PrintDeleteCharacterCancelMessage(DeleteCharacterCancelMessage msg, object tag)
        {
            Console.WriteLine("DeleteCharacterCancelMessage:");
            Console.WriteLine("\tName={0}", msg.Name); //System.String has a toString()
            Console.WriteLine("\tCharacterSN={0}", msg.CharacterSN); //System.Int32 has a toString()
        }

        public static void PrintDeleteCharacterCancelResultMessage(DeleteCharacterCancelResultMessage msg, object tag)
        {
            Console.WriteLine("DeleteCharacterCancelResultMessage:");
            Console.WriteLine("\tCharacterSN={0}", msg.CharacterSN); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.CharacterList.DeleteCharacterCancelResult
        }

        public static void PrintChatReportMessage(ChatReportMessage msg, object tag)
        {
            Console.WriteLine("ChatReportMessage:");
            Console.WriteLine("\tm_Name={0}", msg.m_Name); //System.String has a toString()
            Console.WriteLine("\tm_Type={0}", msg.m_Type); //System.Int32 has a toString()
            Console.WriteLine("\tm_Reason={0}", msg.m_Reason); //System.String has a toString()
            Console.WriteLine("\tm_ChatLog={0}", msg.m_ChatLog); //System.String has a toString()
        }

        public static void PrintCompletedMissionInfoMessage(CompletedMissionInfoMessage msg, object tag)
        {
            Console.WriteLine("CompletedMissionInfoMessage:");
        }

        public static void PrintDSCommandMessage(DSCommandMessage msg, object tag)
        {
            Console.WriteLine("DSCommandMessage:");
            Console.WriteLine("\tCommandType={0}", msg.CommandType); //System.Int32 has a toString()
            Console.WriteLine("\tCommand={0}", msg.Command); //System.String has a toString()
            Console.WriteLine("\tDSCommandType={0}", msg.DSCommandType); //ServiceCore.EndPointNetwork.DS.DSCommandType
        }

        public static void PrintDSPlayerStatusMessage(DSPlayerStatusMessage msg, object tag)
        {
            Console.WriteLine("DSPlayerStatusMessage:");
            Console.WriteLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
            Console.WriteLine("\tStatus={0}", msg.Status); //ServiceCore.EndPointNetwork.DS.DSPlayerStatus
            Console.WriteLine("\tRegisterTimeDiff={0}", msg.RegisterTimeDiff); //System.Int32 has a toString()
            Console.WriteLine("\tOrderCount={0}", msg.OrderCount); //System.Int32 has a toString()
            Console.WriteLine("\tMemberCount={0}", msg.MemberCount); //System.Int32 has a toString()
            Console.WriteLine("\tPartyID={0}", msg.PartyID); //System.Int64 has a toString()
            Console.WriteLine("\tReason={0}", msg.Reason); //System.String has a toString()
            Console.WriteLine("\tIsGiantRaid={0}", msg.IsGiantRaid); //System.Boolean has a toString()
        }

        public static void PrintRegisterDSQueueMessage(RegisterDSQueueMessage msg, object tag)
        {
            Console.WriteLine("RegisterDSQueueMessage:");
            Console.WriteLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
        }

        public static void PrintUnregisterDSQueueMessage(UnregisterDSQueueMessage msg, object tag)
        {
            Console.WriteLine("UnregisterDSQueueMessage:");
            Console.WriteLine("\tQuestID={0}", msg.QuestID); //System.String has a toString()
        }

        public static void PrintDyeAmpleCompleteMessage(DyeAmpleCompleteMessage msg, object tag)
        {
            Console.WriteLine("DyeAmpleCompleteMessage:");
            Console.WriteLine("\tColor={0}", msg.Color); //System.Int32 has a toString()
        }

        public static void PrintEffectTelepathyMessage(EffectTelepathyMessage msg, object tag)
        {
            Console.WriteLine("EffectTelepathyMessage:");
            Console.WriteLine("\tIsEffectFail={0}", msg.IsEffectFail); //System.Boolean has a toString()
            Console.WriteLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            Console.WriteLine("\tEffectName={0}", msg.EffectName); //System.String has a toString()
        }

        public static void PrintExpandExpirationDateItemMessage(ExpandExpirationDateItemMessage msg, object tag)
        {
            Console.WriteLine("ExpandExpirationDateItemMessage:");
            Console.WriteLine("\tMessageType={0}", msg.MessageType); //ServiceCore.EndPointNetwork.ExpandExpirationDateItemMessageType
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tExpanderName={0}", msg.ExpanderName); //System.String has a toString()
            Console.WriteLine("\tExpanderPrice={0}", msg.ExpanderPrice); //System.Int32 has a toString()
            Console.WriteLine("\tIsCredit={0}", msg.IsCredit); //System.Boolean has a toString()
        }

        public static void PrintFreeMatchReportMessage(FreeMatchReportMessage msg, object tag)
        {
            Console.WriteLine("FreeMatchReportMessage:");
            Console.WriteLine("\tWinnerTag={0}", msg.WinnerTag); //System.Int32 has a toString()
            Console.WriteLine("\tLoserTag={0}", msg.LoserTag); //System.Int32 has a toString()
        }

        public static void PrintCloseGuildMessage(CloseGuildMessage msg, object tag)
        {
            Console.WriteLine("CloseGuildMessage:");
        }

        public static void PrintInviteGuildMessage(InviteGuildMessage msg, object tag)
        {
            Console.WriteLine("InviteGuildMessage:");
            Console.WriteLine("\tName={0}", msg.Name); //System.String has a toString()
        }

        public static void PrintJoinGuildMessage(JoinGuildMessage msg, object tag)
        {
            Console.WriteLine("JoinGuildMessage:");
            Console.WriteLine("\tGuildSN={0}", msg.GuildSN); //System.Int32 has a toString()
        }

        public static void PrintLeaveGuildMessage(LeaveGuildMessage msg, object tag)
        {
            Console.WriteLine("LeaveGuildMessage:");
        }

        public static void PrintReconnectGuildMessage(ReconnectGuildMessage msg, object tag)
        {
            Console.WriteLine("ReconnectGuildMessage:");
        }

        public static void PrintMegaphoneFailMessage(MegaphoneFailMessage msg, object tag)
        {
            Console.WriteLine("MegaphoneFailMessage:");
            Console.WriteLine("\tErrorCode={0}", msg.ErrorCode); //System.Int32 has a toString()
        }

        public static void PrintNotifyPlayerReconnectMessage(NotifyPlayerReconnectMessage msg, object tag)
        {
            Console.WriteLine("NotifyPlayerReconnectMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tPlayerName={0}", msg.PlayerName); //System.String has a toString()
        }

        public static void PrintAnswerPlayerReconnectMessage(AnswerPlayerReconnectMessage msg, object tag)
        {
            Console.WriteLine("AnswerPlayerReconnectMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tPlayerName={0}", msg.PlayerName); //System.String has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
        }

        public static void PrintQueryBuybackListMessage(QueryBuybackListMessage msg, object tag)
        {
            Console.WriteLine("QueryBuybackListMessage: []");
        }

        public static void PrintBuybackListResultMessage(BuybackListResultMessage msg, object tag)
        {
            Console.WriteLine(ListToString<BuybackInfo>(msg.BuybackList, "BuybackListResultMessage", 0));
        }

        public static void PrintQueryCashShopItemPickUpMessage(QueryCashShopItemPickUpMessage msg, object tag)
        {
            Console.WriteLine("QueryCashShopItemPickUpMessage:");
        }

        public static void PrintQueryCashShopPurchaseItemMessage(QueryCashShopPurchaseItemMessage msg, object tag)
        {
            Console.WriteLine("QueryCashShopPurchaseItemMessage:");
        }

        public static void PrintQueryCashShopPurchaseGiftMessage(QueryCashShopPurchaseGiftMessage msg, object tag)
        {
            Console.WriteLine("QueryCashShopPurchaseGiftMessage:");
        }

        public static void PrintCashShopFailMessage(CashShopFailMessage msg, object tag)
        {
            Console.WriteLine("CashShopFailMessage:");
        }

        public static void PrintRequestGoddessProtectionMessage(RequestGoddessProtectionMessage msg, object tag)
        {
            Console.WriteLine("RequestGoddessProtectionMessage:");
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
        }

        public static void PrintDirectPurchaseCashShopItemMessage(DirectPurchaseCashShopItemMessage msg, object tag)
        {
            Console.WriteLine("DirectPurchaseCashShopItemMessage:");
            Console.WriteLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
            Console.WriteLine("\tcashType={0}", msg.cashType); //System.Int32 has a toString()
        }

        public static void PrintBuyItemMessage(BuyItemMessage msg, object tag)
        {
            Console.WriteLine("BuyItemMessage:");
            Console.WriteLine("\tShopID={0}", msg.ShopID); //System.String has a toString()
            Console.WriteLine("\tOrder={0}", msg.Order); //System.Int32 has a toString()
            Console.WriteLine("\tCount={0}", msg.Count); //System.Int32 has a toString()
        }

        public static void PrintBuyItemResultMessage(BuyItemResultMessage msg, object tag)
        {
            Console.WriteLine("BuyItemResultMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            Console.WriteLine("\tItemCount={0}", msg.ItemCount); //System.Int32 has a toString()
            Console.WriteLine("\tPriceItemClass={0}", msg.PriceItemClass); //System.String has a toString()
            Console.WriteLine("\tPriceItemCount={0}", msg.PriceItemCount); //System.Int32 has a toString()
            Console.WriteLine("\tRestrictionItemOrder={0}", msg.RestrictionItemOrder); //System.Int32 has a toString()
            Console.WriteLine("\tRestrictionItemCount={0}", msg.RestrictionItemCount); //System.Int32 has a toString()
        }

        public static void PrintRejoinCombatSuccessMessage(RejoinCombatSuccessMessage msg, object tag)
        {
            Console.WriteLine("RejoinCombatSuccessMessage:");
        }

        public static void PrintAcceptAssistMessage(AcceptAssistMessage msg, object tag)
        {
            Console.WriteLine("AcceptAssistMessage:");
            Console.WriteLine("\tSlot={0}", msg.Slot); //System.Int32 has a toString()
        }

        public static void PrintAdditionalMicroPlayContentsMessage(AdditionalMicroPlayContentsMessage msg, object tag)
        {
            Console.WriteLine("AdditionalMicroPlayContentsMessage:");
            Console.WriteLine("\tHostCommandString={0}", msg.HostCommandString); //System.String has a toString()
        }

        public static void PrintDestroyMicroPlayContentsMessage(DestroyMicroPlayContentsMessage msg, object tag)
        {
            Console.WriteLine("DestroyMicroPlayContentsMessage:");
            Console.WriteLine("\tEntityID={0}", msg.EntityID); //System.String has a toString()
        }

        public static void PrintWishListDeleteResponseMessage(WishListDeleteResponseMessage msg, object tag)
        {
            Console.WriteLine("WishListDeleteResponseMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.WishListResult
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
        }

        public static void PrintUseFeverPointMessage(UseFeverPointMessage msg, object tag)
        {
            Console.WriteLine("UseFeverPointMessage:");
        }

        public static void PrintUseInventoryItemWithTargetMessage(UseInventoryItemWithTargetMessage msg, object tag)
        {
            Console.WriteLine("UseInventoryItemWithTargetMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tTargetName={0}", msg.TargetName); //System.String has a toString()
        }

        public static void PrintUserCareItemOpenMessage(UserCareItemOpenMessage msg, object tag)
        {
            Console.WriteLine("UserCareItemOpenMessage:");
            Console.WriteLine("\tIndex={0}", msg.Index); //System.Int32 has a toString()
        }

        public static void PrintUserDSHostConnectionQuery(UserDSHostConnectionQuery msg, object tag)
        {
            Console.WriteLine("UserDSHostConnectionQuery:");
        }

        public static void PrintUserDSLaunchMessage(UserDSLaunchMessage msg, object tag)
        {
            Console.WriteLine("UserDSLaunchMessage:");
        }

        public static void PrintUserDSProcessEndMessage(UserDSProcessEndMessage msg, object tag)
        {
            Console.WriteLine("UserDSProcessEndMessage:");
        }

        public static void PrintUserCareStateUpdateMessage(UserCareStateUpdateMessage msg, object tag)
        {
            Console.WriteLine("UserCareStateUpdateMessage:");
            Console.WriteLine("\tUserCareType={0}", msg.UserCareType); //System.Int32 has a toString()
            Console.WriteLine("\tUserCareNextState={0}", msg.UserCareNextState); //System.Int32 has a toString()
        }

        public static void PrintUserPunishNotifyMessage(UserPunishNotifyMessage msg, object tag)
        {
            Console.WriteLine("UserPunishNotifyMessage:");
            Console.WriteLine("\tType={0}", msg.Type); //System.Byte has a toString()
            Console.WriteLine("\tMessage={0}", msg.Message); //System.String has a toString()
            Console.WriteLine("\tRemainSeconds={0}", msg.RemainSeconds); //System.Int64 has a toString()
        }

        public static void PrintUserPunishReloadMessage(UserPunishReloadMessage msg, object tag)
        {
            Console.WriteLine("UserPunishReloadMessage:");
        }

        public static void PrintUseTiticoreItemMessage(UseTiticoreItemMessage msg, object tag)
        {
            Console.WriteLine("UseTiticoreItemMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tTargetItemClass={0}", msg.TargetItemClass); //System.String has a toString()
        }

        public static void PrintTownEffectMessage(TownEffectMessage msg, object tag)
        {
            Console.WriteLine("TownEffectMessage:");
            Console.WriteLine("\tEffectID={0}", msg.EffectID); //System.Int32 has a toString()
            Console.WriteLine("\tDuration={0}", msg.Duration); //System.Int32 has a toString()
        }

        public static void PrintCouponShopItemInfoQueryMessage(CouponShopItemInfoQueryMessage msg, object tag)
        {
            Console.WriteLine("CouponShopItemInfoQueryMessage:");
            Console.WriteLine("\tShopVersion={0}", msg.ShopVersion); //System.Int16 has a toString()
        }

        public static void PrintMoveSharedItemMessage(MoveSharedItemMessage msg, object tag)
        {
            Console.WriteLine("MoveSharedItemMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tStorage={0}", msg.Storage); //System.Byte has a toString()
            Console.WriteLine("\tTarget={0}", msg.Target); //System.Int32 has a toString()
        }

        public static void PrintQueryRestoreItemListMessage(QueryRestoreItemListMessage msg, object tag)
        {
            Console.WriteLine("QueryRestoreItemListMessage:");
        }

        public static void PrintQueryCIDByNameMessage(QueryCIDByNameMessage msg, object tag)
        {
            Console.WriteLine("QueryCIDByNameMessage:");
            Console.WriteLine("\tRequestName={0}", msg.RequestName); //System.String has a toString()
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
        }

        public static void PrintQueryTiticoreDisplayItemsMessage(QueryTiticoreDisplayItemsMessage msg, object tag)
        {
            Console.WriteLine("QueryTiticoreDisplayItemsMessage:");
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            Console.WriteLine("\tTargetItemClass={0}", msg.TargetItemClass); //System.String has a toString()
        }

        public static void PrintPvpQueryChannelListMessage(PvpQueryChannelListMessage msg, object tag)
        {
            Console.WriteLine("PvpQueryChannelListMessage:");
            Console.WriteLine("\tPvpMode={0}", msg.PvpMode); //System.String has a toString()
            Console.WriteLine("\tKey={0}", msg.Key); //System.String has a toString()
        }

        public static void PrintUseAdvancedFeatherMessage(UseAdvancedFeatherMessage msg, object tag)
        {
            Console.WriteLine("UseAdvancedFeatherMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tTargetName={0}", msg.TargetName); //System.String has a toString()
        }


        public static void PrintAskFinishQuestMessage(AskFinishQuestMessage msg, object tag)
        {
            Console.WriteLine("AskFinishQuestMessage:");
            Console.WriteLine("\tShowPendingDialog={0}", msg.ShowPendingDialog); //System.Boolean has a toString()
            Console.WriteLine("\tMessage={0}", msg.Message); //System.String has a toString()
            Console.WriteLine("\tTimeOut={0}", msg.TimeOut); //System.Int32 has a toString()
        }

        public static void PrintDSHostConnectionQuery(DSHostConnectionQuery msg, object tag)
        {
            Console.WriteLine("DSHostConnectionQuery:");
        }

        public static void PrintQueryFeverPointMessage(QueryFeverPointMessage msg, object tag)
        {
            Console.WriteLine("QueryFeverPointMessage:");
        }

        public static void PrintQueryMileagePointMessage(QueryMileagePointMessage msg, object tag)
        {
            Console.WriteLine("QueryMileagePointMessage:");
        }

        public static void PrintQuestTimerInfoMessage(QuestTimerInfoMessage msg, object tag)
        {
            Console.WriteLine("QuestTimerInfoMessage:");
            Console.WriteLine("\tQuestTime={0}", msg.QuestTime); //System.Int32 has a toString()
            Console.WriteLine("\tIsTimerDecreasing={0}", msg.IsTimerDecreasing); //System.Boolean has a toString()
        }

        public static void PrintOpenInGameCashShopUIMessage(OpenInGameCashShopUIMessage msg, object tag)
        {
            Console.WriteLine("OpenInGameCashShopUIMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
        }

        public static void PrintOpenRandomBoxMessage(OpenRandomBoxMessage msg, object tag)
        {
            Console.WriteLine("OpenRandomBoxMessage:");
            Console.WriteLine("\tGroupID={0}", msg.GroupID); //System.Int32 has a toString()
            Console.WriteLine("\tRandomBoxName={0}", msg.RandomBoxName); //System.String has a toString()
        }

        public static void PrintOpenTiticoreUIMessage(OpenTiticoreUIMessage msg, object tag)
        {
            Console.WriteLine("OpenTiticoreUIMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tTargetItemClass={0}", msg.TargetItemClass); //System.String has a toString()
        }

        public static void PrintOpenTreasureBoxMessage(OpenTreasureBoxMessage msg, object tag)
        {
            Console.WriteLine("OpenTreasureBoxMessage:");
            Console.WriteLine("\tGroupID={0}", msg.GroupID); //System.Int32 has a toString()
            Console.WriteLine("\tTreasureBoxName={0}", msg.TreasureBoxName); //System.String has a toString()
        }

        public static void PrintPvpConfirmJoinMessage(PvpConfirmJoinMessage msg, object tag)
        {
            Console.WriteLine("PvpConfirmJoinMessage:");
            Console.WriteLine("\tPvpMode={0}", msg.PvpMode); //System.String has a toString()
        }

        public static void PrintQueryCharacterNameByCIDListMessage(QueryCharacterNameByCIDListMessage msg, object tag)
        {
            Console.WriteLine("QueryCharacterNameByCIDListMessage:");
            Console.WriteLine("\tCIDList=[{0}]",String.Join(",",msg.CIDList)); //System.Collections.Generic.List`1[System.Int64]
        }

        public static void PrintQueryCharacterNameByCIDListResultMessage(QueryCharacterNameByCIDListResultMessage msg, object tag)
        {
            Console.WriteLine("QueryCharacterNameByCIDListResultMessage:");
            Console.WriteLine("\tNameList=[{0}]",String.Join(",",msg.NameList)); //System.Collections.Generic.List`1[System.String]
        }

        public static void PrintIncreasePvpRankMessage(IncreasePvpRankMessage msg, object tag)
        {
            Console.WriteLine("IncreasePvpRankMessage:");
            Console.WriteLine("\tRankID={0}", msg.RankID); //System.String has a toString()
            Console.WriteLine("\tRankScore={0}", msg.RankScore); //System.Int32 has a toString()
            Console.WriteLine("\tTestGuildID={0}", msg.TestGuildID); //System.Int32 has a toString()
            Console.WriteLine("\tTestGuildName={0}", msg.TestGuildName); //System.String has a toString()
        }

        public static void PrintQueryRandomRankInfoMessage(QueryRandomRankInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryRandomRankInfoMessage:");
        }

        public static void PrintRequestBingoRewardMessage(RequestBingoRewardMessage msg, object tag)
        {
            Console.WriteLine("RequestBingoRewardMessage:");
        }

        public static void PrintCheckCharacterNameResultMessage(CheckCharacterNameResultMessage msg, object tag)
        {
            Console.WriteLine("CheckCharacterNameResultMessage:");
            Console.WriteLine("\tValid={0}", msg.Valid); //System.Boolean has a toString()
            Console.WriteLine("\tName={0}", msg.Name); //System.String has a toString()
            Console.WriteLine("\tErrorMsg={0}", msg.ErrorMsg); //System.String has a toString()
        }

        public static void PrintRestoreItemInfo(RestoreItemInfo msg, object tag)
        {
            Console.WriteLine("RestoreItemInfo:");
            Console.WriteLine("\tSlot={0}", msg.Slot); //ServiceCore.ItemServiceOperations.SlotInfo has a toString()
            Console.WriteLine("\tPriceType={0}", msg.PriceType); //System.String has a toString()
            Console.WriteLine("\tPriceValue={0}", msg.PriceValue); //System.Int32 has a toString()
        }

        public static void PrintRestoreItemListMessage(RestoreItemListMessage msg, object tag)
        {
            Console.WriteLine("RestoreItemListMessage:");
            foreach (RestoreItemInfo item in msg.RestoreItemList) {
                Console.WriteLine("\tSlot={0} Price={1}{2}",item.Slot,item.PriceValue,item.PriceType);
            }
        }

        public static void PrintRequestRouletteBoardMessage(RequestRouletteBoardMessage msg, object tag)
        {
            Console.WriteLine("RequestRouletteBoardMessage:");
            Console.WriteLine("\tType={0}", msg.Type); //System.Int32 has a toString()
        }

        public static void PrintRequestRoulettePickSlotMessage(RequestRoulettePickSlotMessage msg, object tag)
        {
            Console.WriteLine("RequestRoulettePickSlotMessage:");
        }

        public static void PrintSetFreeTitleNameMessage(SetFreeTitleNameMessage msg, object tag)
        {
            Console.WriteLine("SetFreeTitleNameMessage:");
            Console.WriteLine("\tFreeTitleName={0}", msg.FreeTitleName); //System.String has a toString()
        }

        public static void PrintPickSharedItemMessage(PickSharedItemMessage msg, object tag)
        {
            Console.WriteLine("PickSharedItemMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tAmount={0}", msg.Amount); //System.Int32 has a toString()
            Console.WriteLine("\tTargetTab={0}", msg.TargetTab); //System.Byte has a toString()
            Console.WriteLine("\tTargetSlot={0}", msg.TargetSlot); //System.Int32 has a toString()
        }

        public static void PrintCouponShopItemBuyMessage(CouponShopItemBuyMessage msg, object tag)
        {
            Console.WriteLine("CouponShopItemBuyMessage:");
            Console.WriteLine("\tShopID={0}", msg.ShopID); //System.Int16 has a toString()
            Console.WriteLine("\tOrder={0}", msg.Order); //System.Int16 has a toString()
            Console.WriteLine("\tCount={0}", msg.Count); //System.Int32 has a toString()
        }

        public static void PrintVocationResetMessage(VocationResetMessage msg, object tag)
        {
            Console.WriteLine("VocationResetMessage:");
            Console.WriteLine("\tVocationClass={0}", msg.VocationClass); //System.Int32 has a toString()
        }

        public static void PrintShowCharacterNameChangeDialogMessage(ShowCharacterNameChangeDialogMessage msg, object tag)
        {
            Console.WriteLine("ShowCharacterNameChangeDialogMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tName={0}", msg.Name); //System.String has a toString()
        }

        public static void PrintShowTreasureBoxMessage(ShowTreasureBoxMessage msg, object tag)
        {
            Console.WriteLine("ShowTreasureBoxMessage:");
            Console.WriteLine("\tGroupID={0}", msg.GroupID); //System.Int32 has a toString()
            Console.WriteLine("\tTreasureBoxName={0}", msg.TreasureBoxName); //System.String has a toString()
        }

        public static void PrintSkillEnhanceSessionStartMessage(SkillEnhanceSessionStartMessage msg, object tag)
        {
            Console.WriteLine("SkillEnhanceSessionStartMessage:");
            Console.WriteLine("\tSkillName={0}", msg.SkillName); //System.String has a toString()
            Console.WriteLine("\tSkillEnhanceStoneItemID={0}", msg.SkillEnhanceStoneItemID); //System.Int64 has a toString()
        }

        public static void PrintSkillEnhanceSessionEndMessage(SkillEnhanceSessionEndMessage msg, object tag)
        {
            Console.WriteLine("SkillEnhanceSessionEndMessage:");
            Console.WriteLine("\tSkillName={0}", msg.SkillName); //System.String has a toString()
        }

        public static void PrintSkillEnhanceUseErgMessage(SkillEnhanceUseErgMessage msg, object tag)
        {
            Console.WriteLine("SkillEnhanceUseErgMessage:");
            Console.WriteLine("\tErgItemID={0}", msg.ErgItemID); //System.Int64 has a toString()
            Console.WriteLine("\tUseCount={0}", msg.UseCount); //System.Int32 has a toString()
        }

        public static void PrintSkillEnhanceUseErgResultMessage(SkillEnhanceUseErgResultMessage msg, object tag)
        {
            Console.WriteLine("SkillEnhanceUseErgResultMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
            Console.WriteLine("\tCurrentPercentage={0}", msg.CurrentPercentage); //System.Int32 has a toString()
            Console.WriteLine("\tMaxPercentage={0}", msg.MaxPercentage); //System.Int32 has a toString()
        }

        public static void PrintSkillEnhanceRestoreDurabilityMessage(SkillEnhanceRestoreDurabilityMessage msg, object tag)
        {
            Console.WriteLine("SkillEnhanceRestoreDurabilityMessage:");
            Console.WriteLine("\tEnhanceStoneItemID={0}", msg.EnhanceStoneItemID); //System.Int64 has a toString()
            Console.WriteLine("\tUseCount={0}", msg.UseCount); //System.Int32 has a toString()
            Console.WriteLine("\tSkillName={0}", msg.SkillName); //System.String has a toString()
        }

        public static void PrintSpiritInjectionItemMessage(SpiritInjectionItemMessage msg, object tag)
        {
            Console.WriteLine("SpiritInjectionItemMessage:");
            Console.WriteLine("\tSpiritStoneID={0}", msg.SpiritStoneID); //System.Int64 has a toString()
            Console.WriteLine("\tTargetItemID={0}", msg.TargetItemID); //System.Int64 has a toString()
        }

        public static void PrintSpiritInjectionItemResultMessage(SpiritInjectionItemResultMessage msg, object tag)
        {
            Console.WriteLine("SpiritInjectionItemResultMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.SpiritInjectionItemResult
            Console.WriteLine("\tStatName={0}", msg.StatName); //System.String has a toString()
            Console.WriteLine("\tValue={0}", msg.Value); //System.Int32 has a toString()
        }

        public static void PrintVocationResetResultMessage(VocationResetResultMessage msg, object tag)
        {
            Console.WriteLine("VocationResetResultMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
        }

        public static void PrintRequestWebVerificationMessage(RequestWebVerificationMessage msg, object tag)
        {
            Console.WriteLine("RequestWebVerificationMessage:");
            Console.WriteLine("\tSessionNo={0}", msg.SessionNo); //System.Int32 has a toString()
        }

        public static void PrintWebVerificationMessage(WebVerificationMessage msg, object tag)
        {
            Console.WriteLine("WebVerificationMessage:");
            Console.WriteLine("\tSessionNo={0}", msg.SessionNo); //System.Int32 has a toString()
            Console.WriteLine("\tIsSuccessfullyGenerated={0}", msg.IsSuccessfullyGenerated); //System.Boolean has a toString()
            Console.WriteLine("\tPasscode={0}", msg.Passcode); //System.Int64 has a toString()
        }

        public static void PrintWishItemInfo(WishItemInfo msg, object tag)
        {
            Console.WriteLine("WishItemInfo:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
            Console.WriteLine("\tProductName={0}", msg.ProductName); //System.String has a toString()
        }

        public static void PrintWishListInsertResponseMessage(WishListInsertResponseMessage msg, object tag)
        {
            Console.WriteLine("WishListInsertResponseMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.WishListResult
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
        }

        public static void Print_CheatJoinPartyByUserID(_CheatJoinPartyByUserID msg, object tag)
        {
            Console.WriteLine("_CheatJoinPartyByUserID:");
            Console.WriteLine("\tUserID={0}", msg.UserID); //System.String has a toString()
        }

        public static void PrintWishListSelectMessage(WishListSelectMessage msg, object tag)
        {
            Console.WriteLine("WishListSelectMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
        }

        public static void PrintXignCodeReadyMessage(XignCodeReadyMessage msg, object tag)
        {
            Console.WriteLine("XignCodeReadyMessage:");
            Console.WriteLine("\tUserID={0}", msg.UserID); //System.String has a toString()
        }

        public static void PrintUseExtraSharedStorageMessage(UseExtraSharedStorageMessage msg, object tag)
        {
            Console.WriteLine("UseExtraSharedStorageMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tStorageID={0}", msg.StorageID); //System.Byte has a toString()
        }

        public static void PrintAddStatusEffect(ServiceCore.EndPointNetwork.AddStatusEffect msg, object tag)
        {
            Console.WriteLine("AddStatusEffect:");
            Console.WriteLine("\tTag={0}", msg.Tag); //System.Int32 has a toString()
            Console.WriteLine("\tType={0}", msg.Type); //System.String has a toString()
            Console.WriteLine("\tLevel={0}", msg.Level); //System.Int32 has a toString()
            Console.WriteLine("\tTimeSec={0}", msg.TimeSec); //System.Int32 has a toString()
        }

        public static void PrintArmorBrokenMessage(ArmorBrokenMessage msg, object tag)
        {
            Console.WriteLine("ArmorBrokenMessage:");
            Console.WriteLine("\tOwner={0}", msg.Owner); //System.Int32 has a toString()
            Console.WriteLine("\tPart={0}", msg.Part); //System.Int32 has a toString()
        }

        public static void PrintCafeLoginMessage(CafeLoginMessage msg, object tag)
        {
            Console.WriteLine("CafeLoginMessage:");
        }

        public static void PrintCashShopGiftSenderMessage(CashShopGiftSenderMessage msg, object tag)
        {
            Console.WriteLine("CashShopGiftSenderMessage:");
            Console.WriteLine("\tOrderNo={0}", msg.OrderNo); //System.Int32 has a toString()
            Console.WriteLine("\tSenderName={0}", msg.SenderName); //System.String has a toString()
        }

        public static void PrintCashShopBalanceMessage(CashShopBalanceMessage msg, object tag)
        {
            Console.WriteLine("CashShopBalanceMessage:");
            Console.WriteLine("\tBalance={0}", msg.Balance); //System.Int32 has a toString()
            Console.WriteLine("\tRefundless={0}", msg.Refundless); //System.Int32 has a toString()
        }

        public static void PrintQueryCashShopInventoryMessage(QueryCashShopInventoryMessage msg, object tag)
        {
            Console.WriteLine("QueryCashShopInventoryMessage:");
        }

        public static void PrintDirectPurchaseCashShopItemResultMessage(DirectPurchaseCashShopItemResultMessage msg, object tag)
        {
            Console.WriteLine("DirectPurchaseCashShopItemResultMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
            Console.WriteLine("\tReason={0}", msg.Reason); //ServiceCore.EndPointNetwork.DirectPurchaseCashShopItemResultMessage+DirectPurchaseItemFailReason
        }

        public static void PrintCashShopProductListMessage(CashShopProductListMessage msg, object tag)
        {
            Console.WriteLine("CashShopProductListMessage:");
        }

        public static void PrintCashShopCategoryListMessage(CashShopCategoryListMessage msg, object tag)
        {
            Console.WriteLine("CashShopCategoryListMessage:");
        }

        public static void PrintQueryBeautyShopInfoMessage(QueryBeautyShopInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryBeautyShopInfoMessage:");
            Console.WriteLine("\tcharacterType={0}", msg.characterType); //System.Int32 has a toString()
        }

        public static void PrintDirectPurchaseTiticoreItemMessage(DirectPurchaseTiticoreItemMessage msg, object tag)
        {
            Console.WriteLine("DirectPurchaseTiticoreItemMessage:");
            Console.WriteLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
            Console.WriteLine("\tIsCredit={0}", msg.IsCredit); //System.Boolean has a toString()
        }

        public static void PrintDirectPurchaseTiticoreItemResultMessage(DirectPurchaseTiticoreItemResultMessage msg, object tag)
        {
            Console.WriteLine("DirectPurchaseTiticoreItemResultMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
            Console.WriteLine("\tReason={0}", msg.Reason); //ServiceCore.EndPointNetwork.DirectPurchaseCashShopItemResultMessage+DirectPurchaseItemFailReason
            Console.WriteLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
        }

        public static void PrintQueryCashShopRefundMessage(QueryCashShopRefundMessage msg, object tag)
        {
            Console.WriteLine("QueryCashShopRefundMessage:");
            Console.WriteLine("\tOrderNo={0}", msg.OrderNo); //System.Int32 has a toString()
            Console.WriteLine("\tProductNo={0}", msg.ProductNo); //System.Int32 has a toString()
        }

        public static void PrintResetSkillResultMessage(ResetSkillResultMessage msg, object tag)
        {
            Console.WriteLine("ResetSkillResultMessage:");
            Console.WriteLine("\tResetResult={0}", msg.ResetResult); //System.Int32 has a toString()
            Console.WriteLine("\tSkillID={0}", msg.SkillID); //System.String has a toString()
            Console.WriteLine("\tSkillRank={0}", msg.SkillRank); //System.Int32 has a toString()
            Console.WriteLine("\tReturnAP={0}", msg.ReturnAP); //System.Int32 has a toString()
        }

        public static void PrintRequestSecuredOperationMessage(RequestSecuredOperationMessage msg, object tag)
        {
            Console.WriteLine("RequestSecuredOperationMessage:");
            Console.WriteLine("\tOperation={0}", msg.Operation); //ServiceCore.EndPointNetwork.SecuredOperationType
        }

        public static void PrintTradeItemInfo(TradeItemInfo msg, object tag)
        {
            Console.WriteLine("TradeItemInfo:");
            Console.WriteLine("\tTID={0}", msg.TID); //System.Int64 has a toString()
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tChracterName={0}", msg.ChracterName); //System.String has a toString()
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            Console.WriteLine("\tItemCount={0}", msg.ItemCount); //System.Int64 has a toString()
            Console.WriteLine("\tItemPrice={0}", msg.ItemPrice); //System.Int64 has a toString()
            Console.WriteLine("\tCloseDate={0}", msg.CloseDate); //System.Int32 has a toString()
            Console.WriteLine("\tHasAttribute={0}", msg.HasAttribute); //System.Boolean has a toString()
            Console.WriteLine("\tAttributeEX={0}", msg.AttributeEX); //System.String has a toString()
            Console.WriteLine("\tMaxArmorCondition={0}", msg.MaxArmorCondition); //System.Int32 has a toString()
            Console.WriteLine("\tcolor1={0}", msg.color1); //System.Int32 has a toString()
            Console.WriteLine("\tcolor2={0}", msg.color2); //System.Int32 has a toString()
            Console.WriteLine("\tcolor3={0}", msg.color3); //System.Int32 has a toString()
            Console.WriteLine("\tMinPrice={0}", msg.MinPrice); //System.Int32 has a toString()
            Console.WriteLine("\tMaxPrice={0}", msg.MaxPrice); //System.Int32 has a toString()
            Console.WriteLine("\tAvgPrice={0}", msg.AvgPrice); //System.Int32 has a toString()
            Console.WriteLine("\tTradeType={0}", msg.TradeType); //System.Byte has a toString()
        }

        public static void PrintPetChangeNameResultMessage(PetChangeNameResultMessage msg, object tag)
        {
            Console.WriteLine("PetChangeNameResultMessage:");
            Console.WriteLine("\tIsSuccess={0}", msg.IsSuccess); //System.Boolean has a toString()
            Console.WriteLine("\tPetName={0}", msg.PetName); //System.String has a toString()
            Console.WriteLine("\tResultType={0}", msg.ResultType); //System.String has a toString()
        }

        public static void PrintPetOperationMessage(PetOperationMessage msg, object tag)
        {
            Console.WriteLine("PetOperationMessage:");
            Console.WriteLine("\tOperationCode={0}", msg.OperationCode); //ServiceCore.EndPointNetwork.PetOperationType
            Console.WriteLine("\tPetID={0}", msg.PetID); //System.Int64 has a toString()
            Console.WriteLine("\tArg={0}", msg.Arg); //System.String has a toString()
            Console.WriteLine("\tValue1={0}", msg.Value1); //System.Int32 has a toString()
            Console.WriteLine("\tValue2={0}", msg.Value2); //System.Int32 has a toString()
            Console.WriteLine("\tValue3={0}", msg.Value3); //System.Int32 has a toString()
        }

        public static void PrintTransferPartyMasterMessage(TransferPartyMasterMessage msg, object tag)
        {
            Console.WriteLine("TransferPartyMasterMessage:");
            Console.WriteLine("\tNewMasterName={0}", msg.NewMasterName); //System.String has a toString()
        }

        public static void PrintOpenGuildMessage(OpenGuildMessage msg, object tag)
        {
            Console.WriteLine("OpenGuildMessage:");
            Console.WriteLine("\tGuildName={0}", msg.GuildName); //System.String has a toString()
            Console.WriteLine("\tGuildNameID={0}", msg.GuildNameID); //System.String has a toString()
            Console.WriteLine("\tGuildIntro={0}", msg.GuildIntro); //System.String has a toString()
        }

        public static void PrintQueryGuildListMessage(QueryGuildListMessage msg, object tag)
        {
            Console.WriteLine("QueryGuildListMessage:");
            Console.WriteLine("\tQueryType={0}", msg.QueryType); //System.Int32 has a toString()
            Console.WriteLine("\tSearchKey={0}", msg.SearchKey); //System.String has a toString()
            Console.WriteLine("\tPage={0}", msg.Page); //System.Int32 has a toString()
            Console.WriteLine("\tPageSize={0}", msg.PageSize); //System.Byte has a toString()
        }

        public static void PrintRandomMissionMessage(RandomMissionMessage msg, object tag)
        {
            Console.WriteLine("RandomMissionMessage:");
            Console.WriteLine("\tMissionCommand={0}", msg.MissionCommand); //System.Int32 has a toString()
            Console.WriteLine("\tID={0}", msg.ID); //System.Int64 has a toString()
            Console.WriteLine("\tArgs={0}", msg.Args); //System.String has a toString()
            Console.WriteLine("\tArgs2={0}", msg.Args2); //System.Int64 has a toString()
            Console.WriteLine("\tMID={0}", msg.MID); //System.String has a toString()
        }

        public static void PrintEnchantItemResultMessage(EnchantItemResultMessage msg, object tag)
        {
            Console.WriteLine("EnchantItemResultMessage:");
            Console.WriteLine("\tCurrentValue={0}", msg.CurrentValue); //System.Int32 has a toString()
            Console.WriteLine("\tGoalValue={0}", msg.GoalValue); //System.Int32 has a toString()
            Console.WriteLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
            Console.WriteLine("\tRolledDice={0}", msg.RolledDice); //System.String has a toString()
            Console.WriteLine("\tCurrentSuccessRatio={0}", msg.CurrentSuccessRatio); //System.Int32 has a toString()
        }

        public static void PrintEnhanceItemMessage(EnhanceItemMessage msg, object tag)
        {
            Console.WriteLine("EnhanceItemMessage:");
            Console.WriteLine("\tItemID={0}", msg.ItemID); //System.Int64 has a toString()
            Console.WriteLine("\tMaterial1={0}", msg.Material1); //System.String has a toString()
            Console.WriteLine("\tMaterial2={0}", msg.Material2); //System.String has a toString()
            Console.WriteLine("\tAdditionalMaterial={0}", msg.AdditionalMaterial); //System.String has a toString()
            Console.WriteLine("\tIsEventEnhanceAShot={0}", msg.IsEventEnhanceAShot); //System.Boolean has a toString()
        }

        public static void PrintItemFailMessage(ItemFailMessage msg, object tag)
        {
            Console.WriteLine("ItemFailMessage:");
            Console.WriteLine("\tMessage={0}", msg.Message); //ServiceCore.EndPointNetwork.HeroesString has a toString()
        }

        public static void PrintPvpKickedMessage(PvpKickedMessage msg, object tag)
        {
            Console.WriteLine("PvpKickedMessage:");
        }

        public static void PrintPvpRegisterResultMessage(PvpRegisterResultMessage msg, object tag)
        {
            Console.WriteLine("PvpRegisterResultMessage:");
            Console.WriteLine("\tPvpMode={0}", msg.PvpMode); //System.String has a toString()
            Console.WriteLine("\tUsePendingDialog={0}", msg.UsePendingDialog); //System.Boolean has a toString()
            Console.WriteLine("\tStartWaitingTimer={0}", msg.StartWaitingTimer); //System.Boolean has a toString()
            Console.WriteLine("\tState={0}", msg.State); //System.Int32 has a toString()
            Console.WriteLine("\tMessage={0}", msg.Message); //ServiceCore.EndPointNetwork.HeroesString has a toString()
        }

        public static void PrintPvpReportMessage(PvpReportMessage msg, object tag)
        {
            Console.WriteLine("PvpReportMessage:");
            Console.WriteLine("\tEventInt={0}", msg.EventInt); //System.Int32 has a toString()
            Console.WriteLine("\tSubject={0}", msg.Subject); //System.Int32 has a toString()
            Console.WriteLine("\tObject={0}", msg.Object); //System.Int32 has a toString()
            Console.WriteLine("\tArg={0}", msg.Arg); //System.String has a toString()
            Console.WriteLine("\tEvent={0}", msg.Event); //ServiceCore.EndPointNetwork.PvpReportType
        }

        public static void PrintQueryRankInfoMessage(QueryRankInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryRankInfoMessage:");
            Console.WriteLine("\tRankID={0}", msg.RankID); //System.String has a toString()
            Console.WriteLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintQueryRankOtherCharacterInfoMessage(QueryRankOtherCharacterInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryRankOtherCharacterInfoMessage:");
            Console.WriteLine("\tRequesterCID={0}", msg.RequesterCID); //System.Int64 has a toString()
            Console.WriteLine("\tCharacterName={0}", msg.CharacterName); //System.String has a toString()
            Console.WriteLine("\tPeriodType={0}", msg.PeriodType); //System.Int32 has a toString()
        }

        public static void PrintRemoveMissionSuccessMessage(RemoveMissionSuccessMessage msg, object tag)
        {
            Console.WriteLine("RemoveMissionSuccessMessage:");
            Console.WriteLine("\tID={0}", msg.ID); //System.Int64 has a toString()
        }

        public static void PrintRemoveServerStatusEffectMessage(RemoveServerStatusEffectMessage msg, object tag)
        {
            Console.WriteLine("RemoveServerStatusEffectMessage:");
            Console.WriteLine("\tType={0}", msg.Type); //System.String has a toString()
        }

        public static void PrintRequestSortInventoryMessage(RequestSortInventoryMessage msg, object tag)
        {
            Console.WriteLine("RequestSortInventoryMessage:");
            Console.WriteLine("\tStorageNo={0}", msg.StorageNo); //System.Int32 has a toString()
        }

        public static void PrintDyeItemResultMessage(DyeItemResultMessage msg, object tag)
        {
            Console.WriteLine("DyeItemResultMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //System.Int32 has a toString()
            Console.WriteLine("\tItemClass={0}", msg.ItemClass); //System.String has a toString()
            Console.WriteLine("\tColor1={0}", msg.Color1); //System.Int32 has a toString()
            Console.WriteLine("\tColor2={0}", msg.Color2); //System.Int32 has a toString()
            Console.WriteLine("\tColor3={0}", msg.Color3); //System.Int32 has a toString()
            Console.WriteLine("\tTriesLeft={0}", msg.TriesLeft); //System.Int32 has a toString()
        }

        public static void PrintHotSpringAddPotionResultMessage(HotSpringAddPotionResultMessage msg, object tag)
        {
            Console.WriteLine("HotSpringAddPotionResultMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.HotSpringAddPotionResult
            Console.WriteLine("\tTownID={0}", msg.TownID); //System.Int32 has a toString()
            Console.WriteLine("\tPrevPotionItemClass={0}", msg.PrevPotionItemClass); //System.String has a toString()
            HotSpringPotionEffectInfo h = msg.hotSpringPotionEffectInfos;
            Console.WriteLine("\tPotionItemClass={0}",h.PotionItemClass);
            Console.WriteLine("\tCharacterName={0}",h.CharacterName);
            Console.WriteLine("\tGuildName={0}",h.GuildName);
            Console.WriteLine("\tExpiredTime={0}",h.ExpiredTime);
            Console.WriteLine("\tOtherPotionUsableTime={0}",h.OtherPotionUsableTime);
        }

        public static void PrintWishListInsertMessage(WishListInsertMessage msg, object tag)
        {
            Console.WriteLine("WishListInsertMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tList_Wish={0}",String.Join(",",msg.List_Wish)); //System.Collections.Generic.ICollection`1[System.Int32]
        }

        public static void PrintSpawnMonsterMessage(SpawnMonsterMessage msg, object tag)
        {
            Console.WriteLine("SpawnMonsterMessage:");
            foreach (SpawnedMonster m in msg.MonsterList) {
                Console.WriteLine("\tSpawnedMonster:");
                Console.WriteLine("\t\tEntityID={0}",m.EntityID);
                Console.WriteLine("\t\tPoint={0}",m.Point);
                Console.WriteLine("\t\tModel={0}",m.Model);
                Console.WriteLine("\t\tVarianceBefore={0}",m.VarianceBefore);
                Console.WriteLine("\t\tVarianceAfter={0}",m.VarianceAfter);
                Console.WriteLine("\t\tSpeed={0}",m.Speed);
                Console.WriteLine("\t\tMonsterType={0}",m.MonsterType);
                Console.WriteLine("\t\tAIType={0}",m.AIType);
                Console.WriteLine("\t\tMonsterVariance={0}",m.MonsterVariance);
            }
        }

        public static void PrintDropGoldCoreMessage(DropGoldCoreMessage msg, object tag)
        {
            Console.WriteLine("DropGoldCoreMessage:");
            Console.WriteLine("\tMonsterEntityName={0}", msg.MonsterEntityName); //System.String has a toString()
            Console.WriteLine("\tDropImmediately={0}", msg.DropImmediately); //System.Int32 has a toString()
            Console.WriteLine("\tEvilCores:");
            foreach (EvilCoreInfo e in msg.EvilCores) {
                Console.WriteLine("\t\tEvilCore:");
                Console.WriteLine("\t\t\tEvilCoreEntityName={0}",e.EvilCoreEntityName);
                Console.WriteLine("\t\t\tEvilCoreType={0}",e.EvilCoreType);
                Console.WriteLine("\t\t\tWinner={0}",e.Winner);
                Console.WriteLine("\t\t\tAdditionalRareCoreTagList=[{0}]",String.Join(",",e.AdditionalRareCoreTagList));
            }
        }

        

        public static void PrintGuildListMessage(GuildListMessage msg, object tag)
        {
            //TODO: db connect
            Console.WriteLine("GuildListMessage:");
            Console.WriteLine("\tPage={0}", msg.Page); //System.Int32 has a toString()
            Console.WriteLine("\tTotalPage={0}", msg.TotalPage); //System.Int32 has a toString()
            Console.WriteLine("\tGuildList:");
            foreach (InGameGuildInfo g in msg.GuildList) {
                Console.WriteLine("\t\tInGameGuildInfo:");
                Console.WriteLine("\t\t\tGuildSN={0}", g.GuildSN); //System.Int32 has a toString()
                Console.WriteLine("\t\t\tGuildName={0}",g.GuildName); //System.String has a toString()
                Console.WriteLine("\t\t\tGuildLevel={0}", g.GuildLevel); //System.Int32 has a toString()
                Console.WriteLine("\t\t\tMemberCount={0}", g.MemberCount); //System.Int32 has a toString()
                Console.WriteLine("\t\t\tMasterName={0}", g.MasterName); //System.String has a toString()
                Console.WriteLine("\t\t\tMaxMemberCount={0}", g.MaxMemberCount); //System.Int32 has a toString()
                Console.WriteLine("\t\t\tIsNewbieRecommend={0}", g.IsNewbieRecommend); //System.Boolean has a toString()
                Console.WriteLine("\t\t\tGuildPoint={0}", g.GuildPoint); //System.Int64 has a toString()
                Console.WriteLine("\t\t\tGuildNotice={0}", g.GuildNotice); //System.String has a toString()
                Console.WriteLine("\t\t\tDailyGainGP={0}",g.DailyGainGP); //System.Collections.Generic.Dictionary`2[System.Byte,System.Int32]
            }

        }

        public static void PrintUpdateSharedInventoryInfoMessage(UpdateSharedInventoryInfoMessage msg, object tag)
        {
            Console.WriteLine("UpdateSharedInventoryInfoMessage:");
            ICollection<SlotInfo> slotInfos = GetPrivateProperty<ICollection<SlotInfo>>(msg, "slotInfos");
            foreach (SlotInfo s in slotInfos) {
                Console.WriteLine(SlotInfoToString(s,"SlotInfo",1));
            }
        }

        public static void PrintWishListDeleteMessage(WishListDeleteMessage msg, object tag)
        {
            Console.WriteLine("WishListDeleteMessage:");
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tProductNo=[{0}]",String.Join(",",msg.ProductNo)); //System.Collections.Generic.IList`1[System.Int32]
        }

        public static void PrintStartTrialEventMessage(StartTrialEventMessage msg, object tag)
        {
            Console.WriteLine("StartTrialEventMessage:");
            Console.WriteLine("\tSectorGroupID={0}", msg.SectorGroupID); //System.Int32 has a toString()
            Console.WriteLine("\tFactorName={0}", msg.FactorName); //System.String has a toString()
            Console.WriteLine("\tTimeLimit={0}", msg.TimeLimit); //System.Int32 has a toString()
            Console.WriteLine("\tActorsIndex=[{0}]",String.Join(",",msg.ActorsIndex)); //System.Collections.Generic.List`1[System.Int32]
        }

        public static void PrintSkillEnhanceMessage(SkillEnhanceMessage msg, object tag)
        {
            Console.WriteLine("SkillEnhanceMessage:");
            Console.WriteLine("\tAdditionalItemIDs=[{0}]",String.Join(",",msg.AdditionalItemIDs)); //System.Collections.Generic.List`1[System.Int64]
        }

        public static void PrintFrameRateMessage(FrameRateMessage msg, object tag)
        {
            Console.WriteLine("FrameRateMessage:");
            Console.WriteLine("\tFrameRate={0}", msg.FrameRate); //System.Int32 has a toString()
        }

        public static void PrintQueryCharacterViewInfoMessage(QueryCharacterViewInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryCharacterViewInfoMessage:");
            Console.WriteLine("\tQueryID={0}", msg.QueryID); //System.Int64 has a toString()
            Console.WriteLine("\tname={0}", msg.name); //System.String has a toString()
        }

        public static void PrintChangeMissionStatusMessage(ChangeMissionStatusMessage msg, object tag)
        {
            Console.WriteLine("ChangeMissionStatusMessage:");
            Console.WriteLine("\tMissionList:");
            foreach (MissionMessage m in msg.MissionList)
            {
                Console.WriteLine(MissionMessageToString(m, "MissionMessage", 1));
            }
        }

        public static void PrintAssignMissionSuccessMessage(AssignMissionSuccessMessage msg, object tag)
        {
            Console.WriteLine("AssignMissionSuccessMessage:");
            Console.WriteLine("\tMissionList:");
            foreach (MissionMessage m in msg.MissionList)
            {
                Console.WriteLine(MissionMessageToString(m, "MissionMessage", 1));
            }
        }

        public static void PrintPostingInfoMessage(PostingInfoMessage msg, object tag)
        {
            Console.WriteLine("PostingInfoMessage:");
            Console.WriteLine("\tRemainTimeToNextPostingTime={0}", msg.RemainTimeToNextPostingTime); //System.Int32 has a toString()
            Console.WriteLine("\tMissionList:");
            foreach (MissionMessage m in msg.MissionList)
            {
                Console.WriteLine(MissionMessageToString(m,"MissionMessage",1));
            }
        }

        public static void PrintTcProtectRequestMessage(TcProtectRequestMessage msg, object tag)
        {
            Console.WriteLine("TcProtectRequestMessage:");
            Console.WriteLine("\tMd5OfDll={0}",BitConverter.ToString(msg.Md5OfDll)); //System.Byte[]
            Console.WriteLine("\tEncodedBlock={0}", BitConverter.ToString(msg.EncodedBlock)); //System.Byte[]
        }

        public static void PrintGuildOperationMessage(GuildOperationMessage msg, object tag)
        {
            Console.WriteLine("GuildOperationMessage:");
            foreach (GuildOperationInfo g in msg.Operations) {
                Console.WriteLine("\tGuildOperationInfo: Command={0} Target={1} Value={2}",g.Command,g.Target,g.Value);
            }
        }

        public static void PrintBeautyShopInfoMessage(BeautyShopInfoMessage msg, object tag)
        {
            Console.WriteLine("BeautyShopInfoMessage:");
            ICollection<CashShopCategoryListElement> CategoryList = GetPrivateProperty<ICollection<CashShopCategoryListElement>>(msg, "CategoryList");
            Console.WriteLine("\tCategoryList:");
            foreach (CashShopCategoryListElement e in CategoryList) {
                Console.WriteLine("CashShopCategoryListElement: CategoryNo={0} CategoryName={1} ParentCategoryNo={2} DisplayNo={3}",e.CategoryNo,e.CategoryName,e.ParentCategoryNo,e.DisplayNo);
            }
            ICollection<CashShopProductListElement> ProductList = GetPrivateProperty<ICollection<CashShopProductListElement>>(msg, "ProductList");
            Console.WriteLine("\tProductList:");
            foreach (CashShopProductListElement e in ProductList) {
                Console.WriteLine("\t\tCashShopProductListElement:");
                Console.WriteLine("\t\t\tProductNo={0}",e.ProductNo);
                Console.WriteLine("\t\t\tProductExpire={0}",e.ProductExpire);
                Console.WriteLine("\t\t\tProductPieces={0}",e.ProductPieces);
                Console.WriteLine("\t\t\tProductID={0}",e.ProductID);
                Console.WriteLine("\t\t\tProductGUID={0}",e.ProductGUID);
                Console.WriteLine("\t\t\tPaymentType={0}",e.PaymentType);
                Console.WriteLine("\t\t\tProductType={0}",e.ProductType);
                Console.WriteLine("\t\t\tSalePrice={0}",e.SalePrice);
                Console.WriteLine("\t\t\tCategoryNo={0}",e.CategoryNo);
                Console.WriteLine("\t\t\tStatus={0}",e.Status);
            }
            Console.WriteLine("\tCouponList:");
            ICollection<BeautyShopCouponListElement> CouponList = GetPrivateProperty <ICollection<BeautyShopCouponListElement>> (msg, "CouponList");
            foreach (BeautyShopCouponListElement e in CouponList) {
                Console.WriteLine("BeautyShopCouponListElement: Category={0} ItemClass={1} Weight={2}",e.Category,e.ItemClass,e.Weight);
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
            Console.WriteLine("GuildMemberListMessage:");
            Console.WriteLine("\tIsFullUpdate={0}", msg.IsFullUpdate); //System.Boolean has a toString()
            Console.WriteLine("\tMembers:");
            int i = 0;
            foreach (GuildMemberInfo g in msg.Members) {
                string name = String.Format("GuildMemberInfo[{0}]", i++);
                Console.WriteLine(GuildMemberInfoToString(g, name, 2));
            }
        }

        public static void PrintUpdateStoryStatusMessage(UpdateStoryStatusMessage msg, object tag)
        {
            Console.WriteLine("UpdateStoryStatusMessage:");
            Console.WriteLine("\tIsFullInformation={0}", msg.IsFullInformation); //System.Boolean has a toString()
            Console.WriteLine(DictToString<string,int>(msg.StoryStatus,"StoryStatus",1)); //System.Collections.Generic.IDictionary`2[System.String,System.Int32]
        }

        public static void PrintGuildStorageLogsMessage(GuildStorageLogsMessage msg, object tag)
        {
            Console.WriteLine("GuildStorageLogsMessage:");
            Console.WriteLine("\tIsTodayLog={0}", msg.IsTodayLog); //System.Boolean has a toString()
            foreach (GuildStorageBriefLogElement e in msg.BriefLogs) {
                Console.WriteLine("\tGuildStorageBriefLogElement:");
                Console.WriteLine("\t\tCharacterName={0}", e.CharacterName); //System.String has a toString()
                Console.WriteLine("\t\tOperationType={0}", e.OperationType); //ServiceCore.EndPointNetwork.GuildService.GuildStorageOperationType
                Console.WriteLine("\t\tAddCount={0}", e.AddCount); //System.Int32 has a toString()
                Console.WriteLine("\t\tPickCount={0}", e.PickCount); //System.Int32 has a toString()
                Console.WriteLine("\t\tDatestamp={0}", e.Datestamp); //System.Int32 has a toString()
                Console.WriteLine("\t\tTimestamp={0}", e.Timestamp); //System.Int32 has a toString()
            }
            foreach (GuildStorageItemLogElement e in msg.ItemLogs) {
                Console.WriteLine("\tGuildStorageItemLogElement:");
                Console.WriteLine("\t\tCharacterName={0}", e.CharacterName); //System.String has a toString()
                Console.WriteLine("\t\tIsAddItem={0}", e.IsAddItem); //System.Boolean has a toString()
                Console.WriteLine("\t\tItemClass={0}", e.ItemClass); //System.String has a toString()
                Console.WriteLine("\t\tCount={0}", e.Count); //System.Int32 has a toString()
                Console.WriteLine("\t\tDatestamp={0}", e.Datestamp); //System.Int32 has a toString()
                Console.WriteLine("\t\tTimestamp={0}", e.Timestamp); //System.Int32 has a toString()
                Console.WriteLine("\t\tColor1={0}", e.Color1); //System.Int32 has a toString()
                Console.WriteLine("\t\tColor2={0}", e.Color2); //System.Int32 has a toString()
                Console.WriteLine("\t\tColor3={0}", e.Color3); //System.Int32 has a toString()
            }
        }

        public static void PrintCashShopInventoryMessage(CashShopInventoryMessage msg, object tag)
        {
            Console.WriteLine("CashShopInventoryMessage:");
            foreach (CashShopInventoryElement e in msg.Inventory) {
                Console.WriteLine("\tCashShopInventoryElement:");
                Console.WriteLine("\t\tOrderNo={0}", e.OrderNo); //System.Int32 has a toString()
                Console.WriteLine("\t\tProductNo={0}", e.ProductNo); //System.Int32 has a toString()
                Console.WriteLine("\t\tItemClass={0}", e.ItemClass); //System.String has a toString()
                Console.WriteLine("\t\tItemClassEx={0}", e.ItemClassEx); //System.String has a toString()
                Console.WriteLine("\t\tCount={0}", e.Count); //System.Int16 has a toString()
                Console.WriteLine("\t\tr={0}", e.r); //System.String has a toString()
                Console.WriteLine("\t\tg={0}", e.g); //System.String has a toString()
                Console.WriteLine("\t\tb={0}", e.b); //System.String has a toString()
                Console.WriteLine("\t\tExpire={0}", e.Expire); //System.Int16 has a toString()
                Console.WriteLine("\t\tIsGift={0}", e.IsGift); //System.Boolean has a toString()
                Console.WriteLine("\t\tSenderMessage={0}", e.SenderMessage); //System.String has a toString()
                Console.WriteLine("\t\tRemainQuantity={0}", e.RemainQuantity); //System.Int16 has a toString()
            }
        }

        public static void PrintPvpChannelListMessage(PvpChannelListMessage msg, object tag)
        {
            Console.WriteLine("PvpChannelListMessage:");
            Console.WriteLine("\tPvpMode={0}", msg.PvpMode); //System.String has a toString()
            Console.WriteLine("\tKey={0}", msg.Key); //System.String has a toString()
            Console.WriteLine("\tPvpChannelInfos:");
            foreach (PvpChannelInfo p in msg.PvpChannelInfos) {
                Console.WriteLine("\tChannelID={0} Desc={1}",p.ChannelID,p.Desc);
            }
        }

        public static void PrintPvpInfoMessage(PvpInfoMessage msg, object tag)
        {
            Console.WriteLine("PvpInfoMessage:");
            int i = 0;
            foreach (PvpResultInfo p in msg.PvpResultList) {
                Console.WriteLine("\tPvpResultInfo[{0}]: PvpType={1} Win={2} Draw={3} Lose={4}",i++,p.PvpType,p.Win,p.Draw,p.Lose);
            }
        }

        public static void PrintCharacterViewInfoMessage(CharacterViewInfoMessage msg, object tag)
        {
            Console.WriteLine("CharacterViewInfoMessage:");
            Console.WriteLine("\tQueryID={0}", msg.QueryID); //System.Int64 has a toString()
            Console.WriteLine("\tSummary={0}", msg.Summary); //ServiceCore.CharacterServiceOperations.CharacterSummary has a toString()

            Console.WriteLine(DictToString<string,int>(msg.Stat,"Stat",1)); //System.Collections.Generic.IDictionary`2[System.String,System.Int32]
            Console.WriteLine("\tQuickSlotInfo={0}", msg.QuickSlotInfo); //ServiceCore.EndPointNetwork.QuickSlotInfo has a toString()
            int i = 0;
            Console.WriteLine("\tEquipment:");
            foreach (KeyValuePair<int,ColoredEquipment> entry in msg.Equipment)
            {
                ColoredEquipment e = entry.Value;
                Console.WriteLine("\t\t{0}=ColoredEquipment: ItemClass={1} Color1={2} Color2={3} Color3={4}",i++ ,e.ItemClass, e.Color1, e.Color2, e.Color3);
            }

            Console.WriteLine("\tSilverCoin={0}", msg.SilverCoin); //System.Int32 has a toString()
            Console.WriteLine("\tPlatinumCoin={0}", msg.PlatinumCoin); //System.Int32 has a toString()
            Console.WriteLine("\tDurability:");
            foreach (KeyValuePair<int, DurabilityEquipment> entry in msg.Durability) {
                DurabilityEquipment e = entry.Value;
                Console.WriteLine("\t\t{0}=DurabilityEquipment: MaxDurabilityBonus={1} DiffDurability={2}",entry.Key,e.MaxDurabilityBonus,e.DiffDurability);
            }
        }

        public static void PrintSkillEnhanceUseDurabilityMessage(SkillEnhanceUseDurabilityMessage msg, object tag)
        {
            Console.WriteLine("SkillEnhanceUseDurabilityMessage:");
            Console.WriteLine("\tSlot={0}", msg.Slot); //System.Int32 has a toString()
            Console.WriteLine(DictToString<string,int>(msg.UseDurability,"UseDurability",1)); //System.Collections.Generic.Dictionary`2[System.String,System.Int32]
        }

        public static void PrintWishListSelectResponseMessage(WishListSelectResponseMessage msg, object tag)
        {
            Console.WriteLine("WishListSelectResponseMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.WishListResult
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tProductInfo:");
            foreach (WishItemInfo w in msg.ProductInfo) {
                Console.WriteLine("\t\tWishItemInfo: CID={0} ProductNo={1} ProductName={2}",w.CID,w.ProductNo,w.ProductName);
            }
        }

        public static void PrintSkillEnhanceChangedMessage(SkillEnhanceChangedMessage msg, object tag)
        {
            Console.WriteLine("SkillEnhanceChangedMessage:");
            foreach (KeyValuePair<string, BriefSkillEnhance> entry in msg.SkillEnhanceChanged) {
                BriefSkillEnhance e = entry.Value;
                Console.WriteLine("\t{0}=BriefSkillEnhance: GroupKey={1} IndexKey={2} Type={3} ReduceDurability={4} MaxDurabilityBonus={5}",entry.Key,e.GroupKey,e.IndexKey,e.Type,e.ReduceDurability,e.MaxDurabilityBonus);
            }
        }

        public static void PrintSkillEnhanceResultMessage(SkillEnhanceResultMessage msg, object tag)
        {
            Console.WriteLine("SkillEnhanceResultMessage:");
            Console.WriteLine("\tSkillName={0}", msg.SkillName); //System.String has a toString()
            Console.WriteLine("\tSkillEnhanceStoneItem={0}", msg.SkillEnhanceStoneItem); //System.String has a toString()
            Console.WriteLine("\tSuccessRatio={0}", msg.SuccessRatio); //System.Int32 has a toString()
            Console.WriteLine("\tAdditionalItemClasses=[{0}]",String.Join(",",msg.AdditionalItemClasses)); //System.Collections.Generic.List`1[System.String]
            Console.WriteLine("\tResult={0}", msg.Result); //System.Boolean has a toString()
            Console.WriteLine("\tIsAdditionalItemDestroyed={0}", msg.IsAdditionalItemDestroyed); //System.Boolean has a toString()
            Console.WriteLine("\tIsEnhanceStoneProtected={0}", msg.IsEnhanceStoneProtected); //System.Boolean has a toString()
            Console.WriteLine("\tEnhances:");
            foreach (BriefSkillEnhance e in msg.Enhances) {
                Console.WriteLine("\t\tBriefSkillEnhance: GroupKey={0} IndexKey={1} Type={2} ReduceDurability={3} MaxDurabilityBonus={3}", e.GroupKey, e.IndexKey, e.Type, e.ReduceDurability, e.MaxDurabilityBonus);
            }
        }

        public static void PrintRouletteBoardResultMessage(RouletteBoardResultMessage msg, object tag)
        {
            Console.WriteLine("RouletteBoardResultMessage:");
            Console.WriteLine("\tResult={0}", msg.Result); //ServiceCore.EndPointNetwork.RouletteBoardResultMessage+RouletteBoardResult
            Console.WriteLine("\tCID={0}", msg.CID); //System.Int64 has a toString()
            Console.WriteLine("\tRemindsSeconds={0}", msg.RemindsSeconds); //System.Int32 has a toString()
            Console.WriteLine("\tSlotInfos:");
            foreach (RouletteSlotInfo r in msg.SlotInfos) {
                string c1 = IntToRGB(r.Color1);
                string c2 = IntToRGB(r.Color2);
                string c3 = IntToRGB(r.Color3);
                Console.WriteLine("\t\tRouletteSlotInfo: ItemClassEx={0} ItemCount={1} Color1={2} Color2={3} Color3={4} Grade={5}",r.ItemClassEx,r.ItemCount,c1,c2,c3,r.Grade);
            }
        }

        public static void PrintShopItemInfoResultMessage(ShopItemInfoResultMessage msg, object tag)
        {
            Console.WriteLine("ShopItemInfoResultMessage:");
            Console.WriteLine("\tShopID={0}", msg.ShopID); //System.String has a toString()
            Console.WriteLine("\tRestrictionCountDic:");
            foreach (KeyValuePair<short, ShopTimeRestrictedResult> entry in msg.RestrictionCountDic) {
                Console.WriteLine("\t\t{0}=ShopTimeRestrictedResult: BuyableCount={1} NextResetTicksDiff={2}",entry.Key,entry.Value.BuyableCount,entry.Value.NextResetTicksDiff);
            }
        }

        public static void PrintCouponShopItemInfoResultMessage(CouponShopItemInfoResultMessage msg, object tag)
        {
            Console.WriteLine("CouponShopItemInfoResultMessage:");
            Console.WriteLine("\tShopVersion={0}", msg.ShopVersion); //System.Int16 has a toString()
            Console.WriteLine("\tRestrictionCountDic:");
            foreach (KeyValuePair<short, ShopTimeRestrictedResult> entry in msg.RestrictionCountDic)
            {
                Console.WriteLine("\t\t{0}=ShopTimeRestrictedResult: BuyableCount={1} NextResetTicksDiff={2}", entry.Key, entry.Value.BuyableCount, entry.Value.NextResetTicksDiff);
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
            Console.WriteLine(ColoredItemListToString(ResultItemList, "TiticoreResultMessage", 0));
        }

        public static void PrintCaptchaRequestMessage(CaptchaRequestMessage msg, object tag)
        {
            Console.WriteLine("CaptchaRequestMessage:");
            Console.WriteLine("\tAuthCode={0}", msg.AuthCode); //System.Int32 has a toString()
            Console.WriteLine("\tImage={0}",BitConverter.ToString(msg.Image.ToArray())); //System.Collections.Generic.List`1[System.Byte]
            Console.WriteLine("\tRemain={0}", msg.Remain); //System.Int32 has a toString()
            Console.WriteLine("\tRecaptcha={0}", msg.Recaptcha); //System.Boolean has a toString()
        }

        public static void PrintTiticoreDisplayItemsMessage(TiticoreDisplayItemsMessage msg, object tag)
        {
            Console.WriteLine("TiticoreDisplayItemsMessage:");
            ICollection<ColoredItem> TiticoreRareDisplayItems = GetPrivateProperty <ICollection<ColoredItem>> (msg, "TiticoreRareDisplayItems");
            Console.WriteLine(ColoredItemListToString(TiticoreRareDisplayItems, "TiticoreRareDisplayItems", 1));

            ICollection<ColoredItem> TiticoreNormalDisplayItems = GetPrivateProperty <ICollection<ColoredItem>> (msg, "TiticoreNormalDisplayItems");
            Console.WriteLine(ColoredItemListToString(TiticoreNormalDisplayItems, "TiticoreNormalDisplayItems", 1));

            ICollection<ColoredItem> TiticoreKeyItems = GetPrivateProperty <ICollection<ColoredItem>> (msg, "TiticoreKeyItems");
            Console.WriteLine(ColoredItemListToString(TiticoreKeyItems, "TiticoreKeyItems", 1));
        }
    }
}

