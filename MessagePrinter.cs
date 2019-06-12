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

namespace PacketCap
{
    class MessagePrinter
    {
        private Dictionary<Guid, int> categoryDict = new Dictionary<Guid, int>();
        private MessageHandlerFactory mf;
        public void registerPrinters(MessageHandlerFactory mf, Dictionary<int, Guid> getGuid) {
            this.mf = mf;
            foreach (KeyValuePair<int, Guid> entry in getGuid) {
                categoryDict[entry.Value] = entry.Key;
            }
            
            foreach (MethodInfo m in typeof(MessagePrinter).GetMethods(BindingFlags.NonPublic|BindingFlags.Static)) {
                if (m.Name.StartsWith("Print")) {
                    RegisterGeneric(m);
                }
                else
                {
                    //Console.WriteLine("Not registering method {0}",m.Name);
                }
            }
        }

        private void RegisterGeneric(MethodInfo m)
        {
            ParameterInfo[] paramList = m.GetParameters();
            /*Console.Write("Registering {0}(",m.Name);
            foreach (ParameterInfo info in paramList) {
                
                Console.Write("{0} {1}, ", info.ParameterType.Name, info.Name);
            }
            Console.WriteLine(")");
            */
            Type msgType = m.GetParameters()[0].ParameterType;//...Message Type
            Type[] typeArgs = {msgType, typeof(object)};
            Type generic = typeof(Action<,>);
            Type t = generic.MakeGenericType(typeArgs);//Action<...Message,object> Type

            MethodInfo register = typeof(MessagePrinter).GetMethod("Register", BindingFlags.NonPublic | BindingFlags.Instance);
            Delegate d = CreateDelegate(m);
            MethodInfo regGen = register.MakeGenericMethod(msgType);
            regGen.Invoke(this,new object[] {d});
        }

        static Delegate CreateDelegate(MethodInfo method)
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

        private void Register<T>(Action<T,object> printer) {
            if (categoryDict.ContainsKey(typeof(T).GUID)) {
                mf.Register<T>(printer,categoryDict[typeof(T).GUID]);
            }
        }

        private static List<CharacterSummary> characters = null;
        

        private static void PrintCharacterListMessage(CharacterListMessage msg, object tag)
        {
            Console.WriteLine("CharacterListMessage:");
            Console.WriteLine("\tMaxFreeCharacterCount={0}", msg.MaxFreeCharacterCount);
            Console.WriteLine("\tMaxPurchasedCharacterCount={0}", msg.MaxPurchasedCharacterCount);
            Console.WriteLine("\tMaxPremiumCharacters={0}", msg.MaxPremiumCharacters);
            Console.WriteLine("\tProloguePlayed={0}", msg.ProloguePlayed);
            Console.WriteLine("\tPresetUsedCharacterCount={0}", msg.PresetUsedCharacterCount);
            Console.WriteLine("\tLoginPartyState=[{0}]", String.Join(",", msg.LoginPartyState));
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


        private static String MakeColorFromDict(IDictionary<int, int> dict, int key) {
            StringBuilder sb = new StringBuilder();
            int val = -1;
            if (dict == null) {
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

            foreach (KeyValuePair<int, int> entry in dict) {
                keys.Add(entry.Key / 3);
            }
            if (otherDict != null) {
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
                if (color != otherColor) {
                    sb.Append("\n");
                    sb.Append(t);
                    sb.Append(color);
                }
            }
            if (sb.Length == startLen)
            {
                return "";
            }
            else {
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
                if (otherBodyShapeInfo != null && otherBodyShapeInfo.TryGetValue(entry.Key, out float val) && val == entry.Value) {
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
            else {
                return sb.ToString();
            }
            
        }

        private static Dictionary<string, CostumeInfo> costumeInfos = new Dictionary<string, CostumeInfo>();

        private static String CostumeInfoToString(CostumeInfo c, int numTabs, String name, String printName)
        {
            //TODO: send to database
            if (c == null)
            {
                return "";
            }
            costumeInfos.TryGetValue(name, out CostumeInfo l);
            if (l == null) {
                costumeInfos[name] = c;
            }
            String t1 = new string('\t', numTabs);
            StringBuilder sb = new StringBuilder();
            sb.Append(t1);
            sb.Append(printName);
            sb.Append(":");
            int startLen = sb.Length;
            String t = "\n" + new string('\t', numTabs + 1);
            if (l == null || c.Shineness != l.Shineness) {
                sb.Append(t);
                sb.Append("Shineness=");
                sb.Append(c.Shineness);
                
            }
            if(l == null || c.Height != l.Height) {
                sb.Append(t);
                sb.Append("Height=");
                sb.Append(c.Height);
            }
            if (l == null || c.Bust != l.Bust) {
                sb.Append(t);
                sb.Append("Bust=");
                sb.Append(c.Bust);
            }
            if (l == null || c.PaintingPosX != l.PaintingPosX) {
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
            String temp = CharacterDictToString<int>(c.CostumeTypeInfo, "CostumeTypeInfo", numTabs, l?.CostumeTypeInfo);
            if (temp.Length != 0) {
                sb.Append("\n");
                sb.Append(temp);
            }
            temp = ColorDictToString(c.ColorInfo, "ColorInfo", numTabs, l?.ColorInfo);
            if (temp.Length != 0) {
                sb.Append("\n");
                sb.Append(temp);
            }
            temp = CharacterDictToString<bool>(c.AvatarInfo, "AvatarInfo", numTabs, l?.AvatarInfo);
            if (temp.Length != 0) {
                sb.Append("\n");
                sb.Append(temp);
            }
            
            if (l?.AvatarHideInfo != null && c.AvatarHideInfo.Count != 0)
            {
                temp = CharacterDictToString<int>(c.AvatarHideInfo, "AvatarHideInfo", numTabs, l?.AvatarHideInfo);
                if (temp.Length != 0) {
                    sb.Append("\n");
                    sb.Append(temp);
                }
            }
            temp = CharacterDictToString<byte>(c.PollutionInfo, "PollutionInfo", numTabs, l?.PollutionInfo);
            if (temp.Length != 0) {
                sb.Append("\n");
                sb.Append(temp);
            }
            temp = CharacterDictToString<int>(c.EffectInfo, "EffectInfo", numTabs, l?.EffectInfo);
            if (temp.Length != 0) {
                sb.Append("\n");
                sb.Append(temp);
            }

            
            StringBuilder sb2 = new StringBuilder();

            foreach (KeyValuePair<int, int> entry in c.DecorationInfo) {
                if (l != null && l.DecorationInfo.TryGetValue(entry.Key, out int val) && val == entry.Value) {
                    continue;
                }
                sb2.Append(t);
                sb2.Append(IntToDecorationSlot(entry.Key));
                sb2.Append("=");
                sb2.Append(entry.Value);
            }

            if (sb2.Length != 0) {
                sb.Append("\n");
                sb.Append(t1);
                sb.Append("DecorationInfo:");
                sb.Append(sb2.ToString());
            }

            sb2 = new StringBuilder();
            foreach (KeyValuePair<int, int> entry in c.DecorationColorInfo)
            {
                if (l != null && l.DecorationColorInfo.TryGetValue(entry.Key, out int val) && val == entry.Value) {
                    continue;
                }
                sb2.Append(t);
                sb2.Append(IntToDecorationColorSlot(entry.Key));
                sb2.Append("=");
                sb2.Append(IntToRGB(entry.Value));
                
            }
            
            if (sb2.Length != 0) {
                sb.Append("\n");
                sb.Append(t1);
                sb.Append("DecorationColorInfo:");
                sb.Append(sb2.ToString());
            }
            sb.Append("\n");
            sb.Append(BodyShapeInfoToString(c.BodyShapeInfo, numTabs,l?.BodyShapeInfo));

            
            costumeInfos[name] = c;
            if (startLen == sb.Length)
            {
                return "";
            }
            else {
                return sb.ToString();
            }
        }

        private static Dictionary<string, CharacterSummary> characterDict = new Dictionary<string, CharacterSummary>();

        private static string CharacterSummaryToString(CharacterSummary c, string name, int numTabs)
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
            sb.Append(c.CharacterID.ToString());
            if (b == null) {
                sb.Append(t);
                sb.Append("BaseCharacter=");
                sb.Append(BaseCharacterToString(c.BaseCharacter));
            }
            int startLen = sb.Length;
            if (b == null || c.Level != b.Level) {
                sb.Append(t);
                sb.Append("Level=");
                sb.Append(c.Level);
            }
            if (b == null || c.Title != b.Title) {
                sb.Append(t);
                sb.Append("Title=");
                sb.Append(c.Title.ToString());
            }
            if (b == null || c.TitleCount != b.TitleCount) {
                sb.Append(t);
                sb.Append("TitleCount=");
                sb.Append(c.TitleCount.ToString());
            }

            String temp = CostumeInfoToString(c.Costume, numTabs + 1, c.CharacterID.ToString(), "CostumeInfo");
            if (temp.Length != 0) {
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
            if (b == null || c.VocationClass != b.VocationClass) {
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
            else {
                return sb.ToString();
            }
        }

        private static void PrintMailListMessage(MailListMessage msg, object tag)
        {
            Console.WriteLine("MailListMessage:");
            if (msg.ReceivedMailList.Count != 0)
            {
                Console.WriteLine(ListToString<BriefMailInfo>(msg.ReceivedMailList, "ReceivedMailList", 1));
            }
            if (msg.SentMailList.Count != 0)
            {
                Console.WriteLine(ListToString<BriefMailInfo>(msg.SentMailList, "SentMailList", 1));
            }
        }

        private static List<StatusEffectElement> lastStatusEffects = null;
        private static void PrintStatusEffectUpdated(StatusEffectUpdated msg, object tag)
        {
            if (lastStatusEffects != null && lastStatusEffects.Count == 0 && msg.StatusEffects.Count == 0) {
                lastStatusEffects = msg.StatusEffects;
                return;
            }

            Console.WriteLine("StatusEffectUpdated:");
            Console.WriteLine("\tCharacterName={0}", msg.CharacterName);
            if (msg.StatusEffects != null && msg.StatusEffects.Count != 0)
            {
                Console.WriteLine("\tStatusEffects:");
                foreach (StatusEffectElement e in msg.StatusEffects)
                {
                    Console.WriteLine("\t\tType={0} Level={1} RemainTime={2} CombatCount={3}", e.Type, e.Level, e.RemainTime, e.CombatCount);
                }
            }
            lastStatusEffects = msg.StatusEffects;
        }

        private static void PrintQuestProgressMessage(QuestProgressMessage msg, object tag)
        {
            Console.WriteLine("QuestProgressMessage:");
            Console.WriteLine(ListToString<QuestProgressInfo>(msg.QuestProgress, "QuestProgress", 1));
            if (msg.AchievedGoals.Count != 0)
            {
                Console.WriteLine(ListToString<AchieveGoalInfo>(msg.AchievedGoals, "AchievedGoals", 1));
            }

        }

        private static string IntToEquipmentSlot(int key) {
            switch (key) {
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

        private static IDictionary<int,long> lastEquipmentInfo = null;

        private static void PrintInventoryInfoMessage(InventoryInfoMessage msg, object tag)
        {
            //TODO: db connect to share inventory
            Console.WriteLine("InventoryInfoMessage:");
            Console.WriteLine("\tStorageInfos:");
            foreach (StorageInfo info in msg.StorageInfos)
            {
                Console.WriteLine("\t\tstorageID={0} isAvailable={1} storageName={2} storageTag={3}", info.StorageID, info.IsAvailable, info.StorageName, info.StorageTag);
            }
            Console.WriteLine(ListToString<SlotInfo>(msg.SlotInfos, "SlotInfos", 1));
            Console.WriteLine(DictToString<int, long>(msg.EquipmentInfo, "EquipmentInfo", 1, lastEquipmentInfo));
            foreach (KeyValuePair<int, string> entry in msg.QuickSlotInfo.SlotItemClasses)
            {
                if (entry.Value != null && entry.Value.Length != 0)
                {
                    Console.WriteLine("\t\t{0}={1}", IntToEquipmentSlot(entry.Key), entry.Value);
                }
            }
            lastEquipmentInfo = msg.EquipmentInfo;
            Console.WriteLine("\tUnequippableParts=[{0}]", String.Join(",", msg.UnequippableParts));

        }

        private static void PrintTitleListMessage(TitleListMessage msg, object tag)
        {
            Console.WriteLine("TitleListMessage:");
            if (msg.AccountTitles.Count != 0)
            {
                Console.WriteLine(ListToString<TitleSlotInfo>(msg.AccountTitles, "AccountTitles", 1));
            }
            if (msg.Titles.Count != 0)
            {
                Console.WriteLine(ListToString<TitleSlotInfo>(msg.Titles, "Titles", 1));
            }
        }

        private static void PrintRandomRankInfoMessage(RandomRankInfoMessage msg, object tag)
        {
            Console.WriteLine("RandomRankInfoMessage:");
            foreach (RandomRankResultInfo info in msg.RandomRankResult)
            {
                Console.WriteLine("\tEventID={0}", info.EventID);
                Console.WriteLine("\tPeriodType={0}", info.PeriodType);
                if (info.RandomRankResult != null && info.RandomRankResult.Count != 0)
                {
                    Console.WriteLine(ListToString<RankResultInfo>(info.RandomRankResult, "RandomRankResult", 1));
                }
            }
        }

        private static void PrintManufactureInfoMessage(ManufactureInfoMessage msg, object tag)
        {
            Console.WriteLine("ManufactureInfoMessage:");
            if (msg.ExpDictionary.Count != 0)
            {
                Console.WriteLine(DictToString<string, int>(msg.ExpDictionary, "ExpDictionary", 1));
            }
            if (msg.GradeDictionary.Count != 0)
            {
                Console.WriteLine(DictToString<string, int>(msg.GradeDictionary, "GradeDictionary", 1));
            }
            if (msg.Recipes.Count != 0)
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
            if (a.Position.X != b.Position.X || a.Position.Y != b.Position.Y || a.Position.Z != b.Position.Z) {
                sb.Append(t);
                sb.Append(Vector3DToString(a.Position, "Position"));
            }
            
            if (a.Velocity.X != b.Velocity.X || a.Velocity.Y != b.Velocity.Y || a.Velocity.Z != b.Velocity.Z) {
                sb.Append(t);
                sb.Append(Vector3DToString(a.Velocity, "Velocity"));
            }
            if (a.Yaw != b.Yaw) {
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
            
            if (a.StartTime != b.StartTime) {
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

        private static void PrintNotifyAction(NotifyAction msg, object tag)
        {
            Console.WriteLine("NotifyAction:");
            Console.WriteLine("\tID={0}", msg.ID);
            ActionSync last = lastNotifyAction == null ? emptyActionSync : lastNotifyAction.Action;
            Console.WriteLine(ActionSyncToString(msg.Action, "Action", 1, last));
            lastNotifyAction = msg;
        }

        private static void PrintDisappeared(Disappeared msg, object tag)
        {
            Console.WriteLine("Disappeared: ID={0}", msg.ID);
        }

        private static void PrintUserLoginMessage(UserLoginMessage msg, object tag)
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


        private static void PrintClientLogMessage(ClientLogMessage msg, object tag)
        {
            String logType = ((object)(ClientLogMessage.LogTypes)msg.LogType).ToString();
            Console.WriteLine("ClientLogMessage: {0} {1}={2}", logType, msg.Key, msg.Value);
        }

        private static void PrintEnterRegion(EnterRegion msg, object tag)
        {
            Console.WriteLine("EnterRegion: RegionCode={0}", msg.RegionCode);
        }

        private static void PrintQueryCharacterCommonInfoMessage(QueryCharacterCommonInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryCharacterCommonInfoMessage: [QueryID={0} CID={1}]", msg.QueryID, msg.CID);
        }

        private static void PrintRequestJoinPartyMessage(RequestJoinPartyMessage msg, object tag)
        {
            Console.WriteLine("RequestJoinPartyMessage: RequestType={0}",msg.RequestType);
        }

        private static ActionSync emptyActionSync = new ActionSync();

        private static void PrintEnterChannel(EnterChannel msg, object tag)
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

        private static void PrintQueryRankAlarmInfoMessage(QueryRankAlarmInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryRankAlarmInfoMessage: CID={0}", msg.CID);
        }

        private static void PrintQueryNpcTalkMessage(QueryNpcTalkMessage msg, object tag)
        {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintQueryBattleInventoryInTownMessage(QueryBattleInventoryInTownMessage msg, object tag)
        {
            Console.WriteLine("QueryBattleInventoryInTownMessage: []");
        }

        private static void PrintIdentify(ServiceCore.EndPointNetwork.Identify msg, object tag)
        {
            Console.WriteLine("Identify: ID={0} Key={1}", msg.ID, msg.Key);
        }

        private static void PrintHotSpringRequestInfoMessage(HotSpringRequestInfoMessage msg, object tag)
        {
            if (msg == null) {
                return;
            }
            Console.WriteLine("HotSpringRequestInfoMessage: Channel={0} TownID={1}", msg.Channel, msg.TownID);
        }

        private static void PrintMovePartition(MovePartition msg, object tag)
        {
            Console.WriteLine("MovePartition: TargetPartitionID={0}", msg.TargetPartitionID);
        }

        private static void PrintTradeItemClassListSearchMessage(TradeItemClassListSearchMessage msg, object tag)
        {
            Console.WriteLine("TradeItemClassListSearchMessage:");
            Console.WriteLine("\tuniqueNumber={0}", msg.uniqueNumber);
            Console.WriteLine("\tChunkPageNumber={0}", msg.ChunkPageNumber);
            Console.WriteLine("\tOrder={0}", msg.Order.ToString());
            Console.WriteLine("\tisDescending={0}", msg.isDescending);
            Console.WriteLine(ListToString<string>(msg.ItemClassList, "ItemClassList", 1));
            Console.WriteLine("\tDetailOptions:");
            foreach (DetailOption d in msg.DetailOptions)
            {
                Console.WriteLine("\t\t{0}={1} SearchType={2}", d.Key, d.Value, d.SearchType);
            }
        }

        private static UpdateAction lastUpdateAction = null;

        private static void PrintUpdateAction(UpdateAction msg, object tag)
        {
            ActionSync last = lastUpdateAction == null ? emptyActionSync : lastUpdateAction.Data;
            Console.WriteLine(ActionSyncToString(msg.Data, "UpdateAction", 0, last));
            lastUpdateAction = msg;
        }

        private static void PrintTradeCategorySearchMessage(TradeCategorySearchMessage msg, object tag)
        {
            Console.WriteLine("TradeCategorySearchMessage:");
            Console.WriteLine("\ttradeCategory={0}", msg.tradeCategory);
            Console.WriteLine("\ttradeCategorySub={0}", msg.tradeCategorySub);
            Console.WriteLine("\tminLevel={0}", msg.minLevel);
            Console.WriteLine("\tmaxLevel={0}", msg.maxLevel);
            Console.WriteLine("\tuniqueNumber={0}", msg.uniqueNumber);
            Console.WriteLine("\tChunkPageNumber={0}", msg.ChunkPageNumber);
            Console.WriteLine("\tOrder={0}", msg.Order.ToString());
            Console.WriteLine("\tisDescending={0}", msg.isDescending);
            Console.WriteLine("\tDetailOptions:");
            foreach (DetailOption d in msg.DetailOptions)
            {
                Console.WriteLine("\t\t{0}={1} SearchType={2}", d.Key, d.Value, d.SearchType);
            }
        }

        private static void PrintCreateCharacterMessage(CreateCharacterMessage msg, object tag)
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
        private static void PrintCheckCharacterNameMessage(CheckCharacterNameMessage msg, object tag)
        {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintQueryReservedInfoMessage(QueryReservedInfoMessage msg, object tag)
        {
            Console.WriteLine("QueryReservedInfoMessage: []");
        }

        private static void Print_UpdatePlayState(_UpdatePlayState msg, object tag)
        {
            Console.WriteLine("_UpdatePlayState: State={0}", msg.State);
        }

        private static void PrintLogOutMessage(LogOutMessage msg, object tag)
        {
            Console.WriteLine("LogOutMessage: []");
        }







        private static void PrintSyncFeatureMatrixMessage(SyncFeatureMatrixMessage msg, object tag) {
            Console.WriteLine("SyncFeatureMatrixMessage:");
            Console.WriteLine(DictToString<String, String>(msg.FeatureDic, "FeatureDic", 1));
        }
        private static void PrintGiveAPMessage(GiveAPMessage msg, object tag) {
            Console.WriteLine(msg);
        }

        private static void PrintUserIDMessage(UserIDMessage msg, object tag) {
            Console.WriteLine("UserIDMessage: {0}", msg.UserID);
        }

        private static void PrintAnswerFinishQuestMessage(AnswerFinishQuestMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintExchangeMileageResultMessage(ExchangeMileageResultMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }
        private static void PrintSecuredOperationMessage(SecuredOperationMessage msg, object tag) {
            StringBuilder sb = new StringBuilder();
            sb.Append("SecuredOperationMessage: Operation=");
            sb.Append(msg.Operation.ToString());
            sb.Append("LockedTime=");
            sb.Append(msg.LockedTimeInSeconds);
            Console.WriteLine(sb.ToString());
        }
        private static void PrintUseInventoryItemWithCountMessage(UseInventoryItemWithCountMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }
        private static void PrintAllUserJoinCompleteMessage(AllUserJoinCompleteMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintRequestMarbleProcessNodeMessage(RequestMarbleProcessNodeMessage msg, object tag) {
            Console.WriteLine("RequestMarbleProcessNodeMessage: CurrentIndex={0}", msg.CurrentIndex);
        }
        private static void PrintQuerySharedInventoryMessage(QuerySharedInventoryMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintUpdateHistoryBookMessage(UpdateHistoryBookMessage msg, object tag) {
            Console.WriteLine("UpdateHistoryBookMessage:");
            Console.WriteLine("\tType={0}",msg.Type.ToString());

            String arr = msg.HistoryBooks != null ? String.Join(",", msg.HistoryBooks) : "";
            Console.WriteLine("\tHistoryBooks=[{0}]", arr);
        }

        private static void PrintRequestItemCombinationMessage(RequestItemCombinationMessage msg, object tag) {
            Console.WriteLine("RequestItemCombinationMessage:");
            Console.WriteLine("\tcombinedEquipItemClass={0}", msg.combinedEquipItemClass);
            Console.WriteLine("\tpartsIDList=[{0}]", String.Join(",", msg.partsIDList));
            
        }
        private static void PrintGiveCashShopDiscountCouponResultMessage(GiveCashShopDiscountCouponResultMessage msg, object tag) {
            Console.WriteLine("GiveCashShopDiscountCouponResultMessage: result={0}", msg.ToString());
        }

        private static void PrintUpdateHousingPropsMessage(UpdateHousingPropsMessage msg, object tag) {
            Console.WriteLine("UpdateHousingPropsMessage:");
            foreach (HousingPropInfo info in msg.PropList) {
                if (info != null) {
                    Console.WriteLine("\t{0}", info.ToString());
                }
            }
        }

        private static String BaseCharacterToString(BaseCharacter m) {
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

        private static string GameJoinMemberInfoToString(GameJoinMemberInfo m, int tabs) {
            if (m == null) {
                return "";
            }
            String t = new string('\t', tabs);
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}Name={1}", t, m.Name);
            sb.AppendFormat("{0}Level={1}", t, m.Level);
            String c = BaseCharacterToString((BaseCharacter)m.BaseClass);

            sb.AppendFormat("{0}Class={1}", t, c);
            sb.AppendFormat("{0}TitleID={1}", t, m.TitleID);//translate using heroes.db3 and translation xml
            sb.AppendFormat("{0}TitleCount={1}", t, m.TitleCount);


            sb.AppendFormat(DictToString<string, int>(m.Stats, "Stats", 1+tabs));
            sb.AppendFormat(CostumeInfoToString(m.CostumeInfo, tabs, m.Name, "CostumeInfo"));
            sb.AppendFormat("{0}EquippedItems:",t);
            sb.AppendFormat(DictToString<int,string>(m.EquippedItems, "EquippedItems", 1+tabs));

            if (m.Pet != null) {
                Console.WriteLine("{0}Pet: Name={1}, Type={2}", t, m.Pet.PetName, m.Pet.PetType);
            }

            sb.AppendFormat("{0}...", t);//much more info if needed
            return sb.ToString();
        }

        private static void PrintHousingMemberInfoMessage(HousingMemberInfoMessage msg, object tag) {
            //TODO: connect to database to collect avatar info
            Console.WriteLine("HousingMemberInfoMessage:");
            Console.WriteLine(GameJoinMemberInfoToString(msg.MemberInfo,1));
        }

        private static void PrintHousingKickMessage(HousingKickMessage msg, object tag) {
            Console.WriteLine("HousingKickMessage: Slot={0}, NexonSN={1}",msg.Slot,msg.NexonSN);
        }

        private static void PrintHousingInvitedMessage(HousingInvitedMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintHousingHostRestartingMessage(HousingHostRestartingMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintEnterHousingMessage(EnterHousingMessage msg, object tag) {
            Console.WriteLine("EnterHousingMessage: CharacterName={0} HousingIndex={1} EnterType={2} HousingPlayID={3}", msg.CharacterName, msg.HousingIndex, msg.EnterType.ToString(), msg.HousingPlayID);
        }
        private static void PrintEndPendingDialogMessage(EndPendingDialogMessage msg, object tag) {
            Console.WriteLine("EndPendingDialogMessage []");
        }

        private static void PrintCreateHousingMessage(CreateHousingMessage msg, object tag) {
            Console.WriteLine("CreateHousingMessage: OpenLevel={0} Desc={1}",msg.OpenLevel,msg.Desc);
        }

        private static void PrintHotSpringRequestPotionEffectMessage(HotSpringRequestPotionEffectMessage msg, object tag) {
            Console.WriteLine("HotSpringRequestPotionEffectMessage: Channel={0} TownID={1} PotionItemClass={2}", msg.Channel, msg.TownID, msg.PotionItemClass);
        }

        private static void PrintHotSpringAddPotionMessage(HotSpringAddPotionMessage msg, object tag) {
            Console.WriteLine("HotSpringAddPotionMessage: Channel={0} TownID={1} ItemID={2}", msg.Channel, msg.TownID, msg.ItemID);
        }

        private static void PrintBurnItemsMessage(BurnItemsMessage msg, object tag) {
            Console.WriteLine("BurnItemsMessage:");
            foreach (BurnItemInfo info in msg.BurnItemList) {
                Console.WriteLine("\tItemID={0} Count={1}",info.ItemID,info.Count);
            }
        }

        private static void PrintFreeTitleNameCheckMessage(FreeTitleNameCheckMessage msg, object tag) {
            Console.WriteLine("FreeTitleNameCheckMessage: ItemID={0} FreeTitleName={1}", msg.ItemID, msg.FreeTitleName);
        }

        private static void PrintBurnRewardItemsMessage(BurnRewardItemsMessage msg, object tag) {
            Console.WriteLine("BurnRewardItemsMessage:");
            Console.WriteLine(DictToString<string, int>(msg.RewardItems, "RewardItems", 1));
            Console.WriteLine(DictToString<string, int>(msg.RewardMailItems, "RewardMailItems", 1));
        }

        private static void PrintAllUserGoalEventModifyMessage(AllUserGoalEventModifyMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintAvatarSynthesisItemMessage(AvatarSynthesisItemMessage msg, object tag) {
            Console.WriteLine("AvatarSynthesisItemMessage: Material1ID={0} Material2ID={1} Material3ID={2}", msg.Material1ID, msg.Material2ID, msg.Material3ID);
        }

        private static void PrintGetFriendshipPointMessage(GetFriendshipPointMessage msg, object tag) {
            Console.WriteLine("GetFriendshipPointMessage []");
        }

        private static void PrintExchangeMileageMessage(ExchangeMileageMessage msg, object tag) {
            Console.WriteLine("ExchangeMileageMessage []");
        }

        private static void PrintCaptchaResponseMessage(CaptchaResponseMessage msg, object tag) {
            Console.WriteLine("CaptchaResponseMessage: AuthCode={0} Response={1}", msg.AuthCode, msg.Response);
        }

        private static void PrintGuildChatMessage(GuildChatMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintChangeMasterMessage(ChangeMasterMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintGuildGainGPMessage(GuildGainGPMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintGuildLevelUpMessage(GuildLevelUpMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static string ListToString<T>(ICollection<T> list, String name, int numTabs) {
            StringBuilder sb = new StringBuilder();
            String t = new string('\t', numTabs);
            if (name != null && name.Length != 0) {
                sb.Append(t);
                sb.Append(name);
                sb.Append(":\n");
            }
            if (list == null) {
                return sb.ToString();
            }
            t = new string('\t', numTabs+1);
            foreach (T element in list) {
                if (element == null) {
                    continue;
                }
                sb.Append(t);
                sb.Append(element);
                sb.Append("\n");
            }
            return sb.ToString();
        }

        private static void PrintHousingRoomListMessage(HousingRoomListMessage msg, object tag) {
            Console.WriteLine("HousingRoomListMessage:");
            Console.WriteLine(ListToString<HousingRoomInfo>(msg.HousingRoomList, "HousingRoomList", 1));
        }

        private static void PrintHotSpringRequestInfoResultMessage(HotSpringRequestInfoResultMessage msg, object tag)
        {
            Console.WriteLine("HotSpringRequestInfoResultMessage: TownID={0}",msg.TownID);
            foreach (HotSpringPotionEffectInfo info in msg.HotSpringPotionEffectInfos) {
                Console.WriteLine("\tPotionItemClass={0} CharacterName={1} GuildName={2} ExpiredTime={3} OtherPotionUsableTime={4}",info.PotionItemClass,info.CharacterName,info.GuildName,info.ExpiredTime,info.OtherPotionUsableTime);
            }
        }
        private static void PrintBurnJackpotMessage(BurnJackpotMessage msg, object tag) {
            Console.WriteLine("BurnJackpotMessage: CID={0}", msg.CID);
        }

        private static void PrintRequestMarbleCastDiceMessage(RequestMarbleCastDiceMessage msg, object tag) {
            Console.WriteLine("RequestMarbleCastDiceMessage: DiceID={0}", msg.DiceID);
        }

        private static void PrintUpdateHousingItemsMessage(UpdateHousingItemsMessage msg, object tag) {
            Console.WriteLine("UpdateHousingItemsMessage: ClearInven={0}",msg.ClearInven);
            foreach (HousingItemInfo info in msg.ItemList) {
                Console.WriteLine("\t{0}", info);
            }

        }
        private static void PrintHousingPartyInfoMessage(HousingPartyInfoMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintAddFriendShipResultMessage(AddFriendShipResultMessage msg, object tag) {
            String result = ((AddFriendShipResultMessage.AddFriendShipResult)msg.Result).ToString();
            Console.WriteLine("AddFriendShipResultMessage: friendName={0} Result={1}",msg.friendName,result);
        }

        private static void PrintHousingInvitationRejectMessage(HousingInvitationRejectMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintEnhanceSuccessRatioDebugMessage(EnhanceSuccessRatioDebugMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintHasSecondPasswordMessage(HasSecondPasswordMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintGetUserIDMessage(GetUserIDMessage msg, object type) {
            Console.WriteLine("GetUserIDMessage []");
        }

        private static void PrintMakeNamedRingMessage(MakeNamedRingMessage msg, object type) {
            Console.WriteLine("MakeNamedRingMessage: ItemID={0} UserName={1}",msg.ItemID,msg.UserName);
        }

        private static void PrintMarbleInfoResultMessage(MarbleInfoResultMessage msg, object type) {
            Console.WriteLine("MarbleInfoResultMessage:");
            Console.WriteLine("\tMarbleID={0}",msg.MarbleID);
            Console.WriteLine("\tCurrentIndex={0}",msg.CurrentIndex);
            Console.WriteLine("\tNodeList:");
            foreach (MarbleNode node in msg.NodeList) {
                Console.WriteLine("\t\tMarbleNode:");
                Console.WriteLine("\t\t\tNodeIndex={0}", node.NodeIndex);
                Console.WriteLine("\t\t\tNodeType={0}", node.NodeType);
                Console.WriteLine("\t\t\tNodeGrade={0}", node.NodeGrade);
                Console.WriteLine("\t\t\tArg=[{0}]",String.Join(",",node.Arg));
                Console.WriteLine("\t\t\tDesc={0}", node.Desc);
            }
            Console.WriteLine("\tIsFirst={0}",msg.IsFirst);
            Console.WriteLine("\tIsProcessed={0}",msg.IsProcessed);
        }

        private static void PrintRequestPartChangingMessage(RequestPartChangingMessage msg, object type) {
            Console.WriteLine("RequestPartChangingMessage: combinedEquipItemID={0} targetIndex={1} partID={2}", msg.combinedEquipItemID, msg.targetIndex, msg.partID);
        }

        private static void PrintCaptchaResponseResultMessage(CaptchaResponseResultMessage msg, object tag) {
            Console.WriteLine("CaptchaResponseResultMessage: Result=?");
        }

        private static void PrintJoinGuildChatRoomMessage(JoinGuildChatRoomMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintHousingListMessage(HousingListMessage msg, object tag) {
            Console.WriteLine("HousingListMessage: HousingList=[{0}]",String.Join(",",msg.HousingList));
        }

        private static void PrintAvatarSynthesisMaterialRecipesMessage(AvatarSynthesisMaterialRecipesMessage msg, object tag) {
            Console.WriteLine("AvatarSynthesisMaterialRecipesMessage: MaterialRecipies=[{0}]",String.Join(",",msg.MaterialRecipes));
        }

        private static void PrintAvatarSynthesisRequestMessage(AvatarSynthesisRequestMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintGameResourceRespondMessage(GameResourceRespondMessage msg, object tag) {
            Console.WriteLine("GameResourceRespondMessage: ResourceRespond={0}",msg.ResourceRespond);
        }

        private static void PrintAllUserGoalEventMessage(AllUserGoalEventMessage msg, object tag) {
            Console.WriteLine("AllUserGoalEventMessage:");
            Console.WriteLine(DictToString<int, int>(msg.AllUserGoalInfo, "AllUserGoalInfo", 1));
        }

        private static void PrintLeaveHousingMessage(LeaveHousingMessage msg, object tag) {
            Console.WriteLine("LeaveHousingMessage []");
        }

        private static void PrintDecomposeItemResultMessage(DecomposeItemResultMessage msg, object tag) {
            Console.WriteLine("DecomposeItemResultMessage: ResultEXP={0}", msg.ResultEXP.ToString());
            Console.WriteLine(ListToString<string>(msg.GiveItemClassList, "GiveItemClassList", 1));
            
        }

        private static void PrintQueryAvatarSynthesisMaterialRecipesMessage(QueryAvatarSynthesisMaterialRecipesMessage msg, object tag) {
            Console.WriteLine("QueryAvatarSynthesisMaterialRecipesMessage: []");
        }

        private static void PrintSearchHousingRoomMessage(SearchHousingRoomMessage msg, object tag) {
            Console.WriteLine("SearchHousingRoomMessage: Option={0} Keyword={1}",msg.Option,msg.Keyword);
        }

        private static void PrintMarbleSetTimerMessage(MarbleSetTimerMessage msg, object tag) {
            Console.WriteLine("MarbleSetTimerMessage: Time={0}", msg.Time);
        }

        private static void PrintFreeTitleNameCheckResultMessage(FreeTitleNameCheckResultMessage msg, object tag) {
            Console.WriteLine("FreeTitleNameCheckResultMessage:");
            Console.WriteLine("\tItemID={0}",msg.ItemID);
            Console.WriteLine("\tFreeTitleName={0}",msg.FreeTitleName);
            Console.WriteLine("\tIsSuccess={0}",msg.IsSuccess);
            Console.WriteLine("\tHasFreeTitle={0}",msg.HasFreeTitle);
        }

        private static void PrintInsertBlessStoneCompleteMessage(InsertBlessStoneCompleteMessage msg, object tag) {
            Console.WriteLine("InsertBlessStoneCompleteMessage:");
            Console.WriteLine("\tSlot={0}",msg.Slot);
            Console.WriteLine("\tOwnerList=[{0}]", String.Join(",",msg.OwnerList));
            Console.WriteLine("\tTypeList=[{0}]", String.Join(",",msg.TypeList));
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

        private static void PrintEnchantLimitlessMessage(EnchantLimitlessMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintRequestBraceletCombinationMessage(RequestBraceletCombinationMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintSwapHousingItemMessage(SwapHousingItemMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintMarbleProcessNodeResultMessage(MarbleProcessNodeResultMessage msg, object tag) {
            Console.WriteLine("MarbleProcessNodeResultMessage: Type={0} IsChance={1}",msg.Type,msg.IsChance);
        }

        private static void PrintBurnGaugeRequestMessage(BurnGaugeRequestMessage msg, object tag) {
            Console.WriteLine("BurnGaugeRequestMessage: []");
        }

        private static void PrintSetQuoteMessage(SetQuoteMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintRequestAttendanceRewardMessage(RequestAttendanceRewardMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintHousingGameHostedMessage(HousingGameHostedMessage msg, object tag) {
            Console.WriteLine("HousingGameHostedMessage: Map={0} IsOwner={1}",msg.Map,msg.IsOwner);
            Console.WriteLine(ListToString<HousingPropInfo>(msg.HousingProps, "HousingProps", 1));
            Console.WriteLine("\tHostInfo:");
            Console.WriteLine(GameJoinMemberInfoToString(msg.HostInfo, 2));
        }

        private static void PrintHousingKickedMessage(HousingKickedMessage msg, object tag) {
            Console.WriteLine("HousingKickedMessage: []");
        }

        private static void PrintBuyIngameCashshopUseTirMessage(BuyIngameCashshopUseTirMessage msg, object tag) {
            Console.WriteLine("BuyIngameCashshopUseTirMessage: Products=[{0}]",String.Join(",",msg.Products));
        }

        private static void PrintRequestAddPeerMessage(RequestAddPeerMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintMaxDurabilityRepairItemMessage(MaxDurabilityRepairItemMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintSecondPasswordResultMessage(SecondPasswordResultMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static string IntToDecorationSlot(int key) {
            switch (key) {
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

        private static string IntToSlot(int key) {
            switch (key) {
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

        private static string IntToDecorationColorSlot(int key) {
            switch (key) {
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
                if (otherDict != null && otherDict.TryGetValue(entry.Key, out T val) && Comparer<T>.Default.Compare(val, entry.Value) == 0) {
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
            else {
                return sb.ToString();
            }
        }

        private static String DictToString<T1, T2>(IDictionary<T1, T2> dict, String name, int numTabs, IDictionary<T1, T2> lastDict)
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
            else {
                return sb.ToString();
            }
        }

        private static String DictToString<T1,T2>(IDictionary<T1, T2> dict, String name, int numTabs) {
            String t = new string('\t', numTabs);
            StringBuilder sb = new StringBuilder();
            sb.Append(t);
            t = new string('\t', numTabs+1);
            sb.Append(name);
            sb.Append(":\n");
            if (dict == null) {
                return sb.ToString();
            }
            foreach (KeyValuePair<T1, T2> entry in dict) {
                sb.Append(t);
                sb.Append(entry.Key);
                sb.Append("=");
                sb.Append(entry.Value);
                sb.Append("\n");
            }
            sb.Remove(sb.Length-1,1);
            return sb.ToString();
        }

        private static void PrintCharacterCommonInfoMessage(CharacterCommonInfoMessage msg, object tag) {
            Console.WriteLine(CharacterSummaryToString(msg.Info, "CharacterCommonInfoMessage", 0));
        }

        private static void PrintChannelServerAddress(ChannelServerAddress msg, object tag) {
            Console.WriteLine("ChannelServerAddress: ChannelID={0} Address={1} Port={2} Key={3}",msg.ChannelID,msg.Address,msg.Port,msg.Key);
        }

        private static void PrintSystemMessage(SystemMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintNpcTalkMessage(NpcTalkMessage msg, object tag) {
            Console.WriteLine(ListToString<NpcTalkEntity>(msg.Content, "NpcTalkMessage", 0));
        }
        private static void PrintHousingStartGrantedMessage(HousingStartGrantedMessage msg, object tag) {
            if (msg == null)
            {
                Console.WriteLine("HousingStartGrantedMessage: [null]", msg.NewSlot, msg.NewKey);
            }
            else {
                Console.WriteLine("HousingStartGrantedMessage: [NewSlot={0} NewKey={1}]", msg.NewSlot, msg.NewKey);
            }
        }

        private static void PrintUpdateStoryGuideMessage(UpdateStoryGuideMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintAddFriendshipInfoMessage(AddFriendshipInfoMessage msg, object tag) {
            Console.WriteLine("AddFriendshipInfoMessage: FriendID={0} FriendLimitCount={1}", msg.FriendID, msg.FriendLimitCount);
        }

        private static void PrintSkillListMessage(SkillListMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintLoginOkMessage(LoginOkMessage msg, object tag) {
            Console.WriteLine("LoginOkMessage: Time={0}",msg.Time);
            //TODO: find out what user care stuff does
        }

        private static void PrintTodayMissionInitializeMessage(TodayMissionInitializeMessage msg, object tag) {
            Console.WriteLine("TodayMissionInitializeMessage:");
            Console.WriteLine("\t{0}", String.Join("\n\t", msg.ToString().Split('\n')));
        }

        private static void PrintAPMessage(APMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintGuildResultMessage(GuildResultMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintCostumeUpdateMessage(CostumeUpdateMessage msg, object tag) {
            //TODO: db connect
            Console.WriteLine(CostumeInfoToString(msg.CostumeInfo, 0, character.CharacterID, "CostumeUpdateMessage"));
        }

        private static IDictionary<int, long> lastEquipInfos = null;

        private static void PrintEquipmentInfoMessage(EquipmentInfoMessage msg, object tag) {
            Console.WriteLine(DictToString<int, long>(msg.EquipInfos, "EquipmentInfoMessage", 0, lastEquipInfos));
            lastEquipInfos = msg.EquipInfos;
        }

        private static IDictionary<string, int> lastStats = null;
        private static void PrintUpdateStatMessage(UpdateStatMessage msg, object tag) {
            Console.WriteLine(DictToString<string, int>(msg.Stat, "UpdateStatMessage",0,lastStats));
            lastStats = msg.Stat;
        }

        private static void PrintUpdateInventoryInfoMessage(UpdateInventoryInfoMessage msg, object tag) {
            Console.WriteLine("UpdateInventoryInfoMessage: [?]");
        }

        private static void PrintFriendshipInfoListMessage(FriendshipInfoListMessage msg, object tag) {
            Console.WriteLine("FriendshipInfoListMessage: FriendList=[{0}]", String.Join(",", msg.FriendList));
        }

        private static void PrintNpcListMessage(NpcListMessage msg, object tag) {
            Console.WriteLine("NpcListMessage:");
            if (msg.Buildings == null) {
                return;
            }
            foreach (BuildingInfo b in msg.Buildings) {
                Console.WriteLine("\tBuildingID={0} Npcs=[{1}]", b.BuildingID, String.Join(",", b.Npcs));
            }
        }

        private static void PrintTradeSearchResult(TradeSearchResult msg, object tag) {
            //TODO: send to database
            Console.WriteLine("TradeSearchResult:");
            Console.WriteLine("\tUniqueNumber={0}", msg.UniqueNumber);
            Console.WriteLine("\tIsMoreResult={0}", msg.IsMoreResult);
            Console.WriteLine("\tresult={0}", msg.result);
            Console.WriteLine("\tTradeItemList:");
            if (msg.TradeItemList == null) {
                return;
            }
            foreach (TradeItemInfo i in msg.TradeItemList) {
                Console.WriteLine("\t\tTID={0} CID={1} ChracterName={2} ItemClass={3} ItemCount={4} ItemPrice={5} CloseDate={6} HasAttribute={7} MaxArmorCondition={8} color1={9} color2={10} color3={11}",i.TID,i.CID,i.ChracterName,i.ItemClass,i.ItemCount,i.ItemPrice,i.CloseDate,i.HasAttribute,i.MaxArmorCondition,i.color1,i.color2,i.color3);
            }
        }

        private static void PrintAskSecondPasswordMessage(AskSecondPasswordMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintNoticeGameEnvironmentMessage(NoticeGameEnvironmentMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintSpSkillMessage(SpSkillMessage msg, object tag) {
            Console.WriteLine(DictToString<int, string>(msg.SpSkills, "SpSkillMessage", 0));
        }

        private static void PrintVocationSkillListMessage(VocationSkillListMessage msg, object tag) {
            Console.WriteLine(DictToString<string, int>(msg.SkillList, "VocationSkillListMessage", 0));
        }

        private static void PrintWhisperFilterListMessage(WhisperFilterListMessage msg, object tag) {
            Console.WriteLine(DictToString<string, int>(msg.Filter, "WhisperFilterListMessage", 0));
        }

        private static void PrintGetCharacterMissionStatusMessage(GetCharacterMissionStatusMessage msg, object tag) {
            Console.WriteLine("GetCharacterMissionStatusMessage:");
            Console.WriteLine("\tMissionCompletionCount={0}",msg.MissionCompletionCount);
            Console.WriteLine("\tRemainTimeToCleanMissionCompletionCount={0}", msg.RemainTimeToCleanMissionCompletionCount);
            Console.WriteLine("\tMissionList:");
            foreach (MissionMessage m in msg.MissionList) {
                Console.WriteLine("\t\tMID={0} Title={1} Location={2} Description={3}", m.MID, m.Title, m.Location, m.Description);
            }
        }

        private static void PrintSelectPatternMessage(SelectPatternMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintQueryHousingItemsMessage(QueryHousingItemsMessage msg, object tag) {
            Console.WriteLine("QueryHousingItemsMessage: []");
        }

        private static void PrintFishingResultMessage(FishingResultMessage msg, object tag) {
            Console.WriteLine(ListToString<FishingResultInfo>(msg.FishingResult, "FishingResultMessage", 0));
        }

        private static void PrintPetListMessage(PetListMessage msg, object tag) {
            Console.WriteLine("PetListMessage:");
            Console.WriteLine("\tIsTotalPetList={0}", msg.IsTotalPetList);
            Console.WriteLine(ListToString<PetStatusInfo>(msg.PetList, "PetList", 1));
        }

        private static PetFeedListMessage lastPetFeedMsg = null;

        private static void PrintPetFeedListMessage(PetFeedListMessage msg, object tag) {
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
                    if (!dict.TryGetValue(e.Type, out int val) || val != e.Count) {
                        exit = false;
                    }
                }
                if (exit) {
                    return;
                }
            }
            Console.WriteLine("PetFeedListMessage:");
            Console.WriteLine("\tIsTotalPetList={0}", msg.IsTotalPetList);
            Console.WriteLine(ListToString(msg.PetFeedList, "PetFeedList", 1));
            lastPetFeedMsg = msg;
        }

        private static void PrintSharedInventoryInfoMessage(SharedInventoryInfoMessage msg, object tag) {
            Console.WriteLine("SharedInventoryInfoMessage:");
            if (msg.StorageInfos.Count != 0) {
                Console.WriteLine("\tStorageInfos:");
                foreach (StorageInfo info in msg.StorageInfos)
                {
                    Console.WriteLine("\t\tstorageID={0} isAvailable={1} storageName={2} storageTag={3}", info.StorageID, info.IsAvailable, info.StorageName, info.StorageTag);
                }
            }
            if (msg.SlotInfos.Count != 0) {
                Console.WriteLine(ListToString<SlotInfo>(msg.SlotInfos, "SlotInfos", 1));
            }
        }

        private static void PrintTirCoinInfoMessage(TirCoinInfoMessage msg, object tag) {
            if (msg.TirCoinInfo.ContainsKey(1))
            {
                Console.WriteLine("TirCoinInfoMessage: Quantity={0}", msg.TirCoinInfo[1]);
            }
            else {
                Console.WriteLine(DictToString<byte, int>(msg.TirCoinInfo, "TirCoinInfoMessage", 0));
            }
        }

        private static void PrintRankAlarmInfoMessage(RankAlarmInfoMessage msg, object tag) {
            Console.WriteLine(ListToString<RankAlarmInfo>(msg.RankAlarm,"RankAlarmInfoMessage", 0));
        }

        private static void PrintUpdateBattleInventoryInTownMessage(UpdateBattleInventoryInTownMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintBingoBoardResultMessage(BingoBoardResultMessage msg, object tag) {
            Console.WriteLine("BingoBoardResultMessage:");
            Console.WriteLine("\tResult={0}", ((BingoBoardResultMessage.Bingo_Result)msg.Result).ToString());
            Console.WriteLine("\tBingoBoardNumbers=[{0}]", String.Join(",", msg.BingoBoardNumbers));
        }

        private static void PrintJoinHousingMessage(JoinHousingMessage msg, object tag) {
            Console.WriteLine("JoinHousingMessage: [TargetID={0}]",msg.TargetID);
        }

        private static void PrintAttendanceInfoMessage(AttendanceInfoMessage msg, object tag) {
            //TODO: db connect
            Console.WriteLine("AttendanceInfoMessage:");
            Console.WriteLine("\tEventType={0}", msg.EventType);
            Console.WriteLine("\tCurrentVersion={0}", msg.CurrentVersion);
            Console.WriteLine("\tPeriodText={0}", msg.PeriodText);
            if (msg.AttendanceInfo != null && msg.AttendanceInfo.Count != 0) {
                Console.WriteLine("\tAttendanceInfo:");
                foreach (AttendanceDayInfo info in msg.AttendanceInfo)
                {
                    Console.WriteLine("\t\tday={0} isCompleted={1} completedRewardsIndex={2}", info.day, info.isCompleted, info.completedRewardIndex);
                }
            }
            
            if (msg.BonusRewardInfo != null && msg.BonusRewardInfo.Count != 0) {
                Console.WriteLine("\tBonusRewardInfo:");
                foreach (AttendanceDayInfo info in msg.BonusRewardInfo)
                {
                    Console.WriteLine("\t\tday={0} isCompleted={1} completedRewardsIndex={2}", info.day, info.isCompleted, info.completedRewardIndex);
                }
            }
            
        }
        private static void PrintUpdateTitleMessage(UpdateTitleMessage msg, object tag) {
            Console.WriteLine(ListToString<TitleSlotInfo>(msg.Titles, "UpdateTitleMessage", 0));
        }

        private static void PrintGuildInventoryInfoMessage(GuildInventoryInfoMessage msg, object tag) {
            Console.WriteLine("GuildInventoryInfoMessage:");
            Console.WriteLine("\tIsEnabled={0}",msg.IsEnabled);
            Console.WriteLine("\tStorageCount={0}",msg.StorageCount);
            Console.WriteLine("\tGoldLimit={0}",msg.GoldLimit);
            Console.WriteLine("\tAccessLimtiTag={0}",msg.AccessLimtiTag);
            Console.WriteLine(ListToString<SlotInfo>(msg.SlotInfos,"SlotInfos",1));
        }

        private static void PrintCashshopTirCoinResultMessage(CashshopTirCoinResultMessage msg, object tag) {
            Console.WriteLine("CashshopTirCoinResultMessage:");
            Console.WriteLine("\tIsSuccess={0}",msg.isSuccess);
            Console.WriteLine("\tisBeautyShop={0}",msg.isBeautyShop);
            Console.WriteLine("\tsuccessCount={0}",msg.successCount);
            Console.WriteLine("\tIgnoreItems:");
            foreach (TirCoinIgnoreItemInfo item in msg.IgnoreItems) {
                Console.WriteLine("\t\tItemClass={0} Amount={1} Duration={2} Price={3}",item.ItemClass,item.Amount,item.Duration,item.Price);
            }
        }

        private static void PrintGiveCashShopDiscountCouponMessage(GiveCashShopDiscountCouponMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintNextSectorMessage(NextSectorMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintBurnGauge(BurnGauge msg, object tag) {
            //TODO: add db connect
            Console.WriteLine("BurnGauge:");
            Console.WriteLine("\tGauge={0}",msg.Gauge);
            Console.WriteLine("\tJackpotStartGauge={0}",msg.JackpotStartGauge);
            Console.WriteLine("\tJackpotMaxGauge={0}",msg.JackpotMaxGauge);
        }

        private static void PrintStoryLinesMessage(StoryLinesMessage msg, object tag) {
            Console.WriteLine("StoryLinesMessage:");
            foreach (BriefStoryLineInfo info in msg.StoryStatus) {
                Console.WriteLine("\tStoryLine={0} Phase={1} Status={2} PhaseText={3}",info.StoryLine,info.Phase,info.Status,info.PhaseText);
            }
        }

        private static void PrintQuickSlotInfoMessage(QuickSlotInfoMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintGuildInfoMessage(GuildInfoMessage msg, object tag) {
            //TODO: db connect
            Console.WriteLine(msg.ToString());
        }

        private static void PrintNotifyLook(NotifyLook msg, object tag) {
            //TODO: db connect
            Console.WriteLine("NotifyLook:");
            Console.WriteLine("\tID={0}", msg.ID);
            Console.WriteLine(CharacterSummaryToString(msg.Look, "Look", 1));
        }

        private static void PrintQueryCashShopProductListMessage(QueryCashShopProductListMessage msg, object tag) {
            Console.WriteLine("QueryCashShopProductListMessage: []");
        }

        private static void PrintQueryCashShopBalanceMessage(QueryCashShopBalanceMessage msg, object tag) {
            Console.WriteLine("QueryCashShopBalanceMessage: []");
        }

        private static void PrintSecondPasswordMessage(SecondPasswordMessage msg, object tag) {
            //TODO: hide
            Console.WriteLine(msg.ToString());
        }

        private static CharacterSummary character = null;

        private static void PrintSelectCharacterMessage(SelectCharacterMessage msg, object tag) {
            character = characters[msg.Index];
            Console.WriteLine(msg.ToString());
        }
        private static void PrintRegisterServerMessage(RegisterServerMessage msg, object tag)
        {
            //TODO: proudnet stuff from this message
            Console.WriteLine("RegisterServerMessage:");
            GameInfo g = msg.TheInfo;
            Console.WriteLine("\tName={0}",g.Name);
            Console.WriteLine("\tMap={0}",g.Map);
            Console.WriteLine("\tGameDir={0}",g.GameDir);
            Console.WriteLine("\tDescription={0}",g.Description);
            Console.WriteLine("\tHostID={0}",g.HostID);
            Console.WriteLine("\tDSIP={0}",g.DSIP);
            Console.WriteLine("\tDSPort={0}",g.DSPort);
        }
        private static void PrintQueryQuestProgressMessage(QueryQuestProgressMessage msg, object tag)
        {
            Console.WriteLine("QueryQuestProgressMessage: []");
        }

        private static void PrintQueryInventoryMessage(QueryInventoryMessage msg, object tag)
        {
            Console.WriteLine("QueryInventoryMessage: []");
        }

        private static void PrintReturnToTownMessage(ReturnToTownMessage msg, object tag)
        {
            Console.WriteLine("ReturnToTownMessage: []");
        }

        private static void PrintLeavePartyMessage(LeavePartyMessage msg, object tag)
        {
            Console.WriteLine("LeavePartyMessage: []");
        }

        private static void PrintFindNewbieRecommendGuildMessage(FindNewbieRecommendGuildMessage msg, object tag)
        {
            Console.WriteLine("FindNewbieRecommendGuildMessage: []");
        }

        private static void PrintPropBrokenMessage(PropBrokenMessage msg, object tag)
        {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintSectorPropListMessage(SectorPropListMessage msg, object tag)
        {
            if (msg == null || msg.Props.Count == 0) {
                return;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("SectorPropListMessage: [");
            foreach (KeyValuePair<int, int> entry in msg.Props) {
                sb.Append(entry.Key);
                sb.Append("=");
                sb.Append(entry.Value);
                sb.Append(", ");
            }
            
            sb.Remove(sb.Length - 2, 2);
            sb.Append("]");
            Console.WriteLine(sb.ToString());
        }

        private static void PrintMoveToNextSectorMessage(MoveToNextSectorMessage msg, object tag)
        {
            Console.WriteLine("MoveToNextSectorMessage:");
            Console.WriteLine("\tTriggerName={0}",msg.TriggerName);
            Console.WriteLine("\tTargetGroup={0}",msg.TargetGroup);
            Console.WriteLine("\tHolyProps=[{0}]", String.Join(",", msg.HolyProps));
        }
        private static void PrintStartGameMessage(StartGameMessage msg, object tag)
        {
            Console.WriteLine("StartGameMessage: []");
        }

        private static void PrintAcceptQuestMessage(AcceptQuestMessage msg, object tag)
        {
            Console.WriteLine("AcceptQuestMessage:");
            Console.WriteLine("\tQuestID={0}",msg.QuestID);
            Console.WriteLine("\tTitle={0}",msg.Title);
            Console.WriteLine("\tSwearID={0}",msg.SwearID);
            ShipOptionInfo s = msg.Option;
            Console.WriteLine("\tMaxMemberCount={0}",s.MaxMemberCount);
            Console.WriteLine("\tSwearMemberLimit={0}",s.SwearMemberLimit);
            Console.WriteLine("\tUntilForceStart={0}",s.UntilForceStart);
            Console.WriteLine("\tMinLevel={0}",s.MinLevel);
            Console.WriteLine("\tMaxLevel={0}",s.MaxLevel);
            Console.WriteLine("\tIsPartyOnly={0}",s.IsPartyOnly);
            Console.WriteLine("\tOver18Only={0}",s.Over18Only);
            Console.WriteLine("\tDifficulty={0}",s.Difficulty);
            Console.WriteLine("\tIsSeason2={0}",s.IsSeason2);
            Console.WriteLine("\tSelectedBossQuestIDInfos=[{0}]",String.Join(",",s.SelectedBossQuestIDInfos));
            Console.WriteLine("\tIsPracticeMode={0}",s.IsPracticeMode);
            Console.WriteLine("\tUserDSMode={0}",s.UserDSMode);
        }

        private static void PrintQueryRecommendShipMessage(QueryRecommendShipMessage msg, object tag)
        {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintEquipItemMessage(EquipItemMessage msg, object tag)
        {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintEquipBundleMessage(EquipBundleMessage msg, object tag)
        {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintMoveInventoryItemMessage(MoveInventoryItemMessage msg, object tag)
        {
            Console.WriteLine(msg.ToString());
        }

        private static BeautyShopCustomizeMessage lastBeautyShopMsg = null;

        private static void PrintBeautyShopCustomizeMessage(BeautyShopCustomizeMessage msg, object tag)
        {
            Console.WriteLine("BeautyShopCustomizeMessage:");
            Console.WriteLine("CustomizeItems:");
            foreach (CustomizeItemRequestInfo info in msg.CustomizeItems) {
                String color = String.Format("({0},{1},{2})", IntToRGB(info.Color1), IntToRGB(info.Color2), IntToRGB(info.Color3));
                Console.WriteLine("\tItemClass={0} Category={1} Color={2} Duration={3} Price={4} CouponItemID={5}",info.ItemClass,info.Category,color,info.Duration,info.Price,info.CouponItemID);
            }
            BeautyShopCustomizeMessage l = lastBeautyShopMsg;
            BeautyShopCustomizeMessage c = msg;
            if (l == null || c.PaintingPosX != l.PaintingPosX) {
                Console.WriteLine("\tPaintingPosX={0}", c.PaintingPosX);
            }
            if (l == null || c.PaintingPosX != l.PaintingPosX) {
                Console.WriteLine("\tPaintingPosY={0}", c.PaintingPosY);
            }
            if (l == null || c.PaintingSize != l.PaintingSize) {
                Console.WriteLine("\tPaintingSize={0}", c.PaintingSize);
            }
            if (l == null || c.PaintingRotation != l.PaintingRotation) {
                Console.WriteLine("\tPaintingRotation={0}", c.PaintingRotation);
            }
            if (l == null || c.payment != l.payment) {
                Console.WriteLine("\tPaintingRotation={0}", c.payment);
            }
            if (l == null || c.BodyPaintingPosX != l.BodyPaintingPosX) {
                Console.WriteLine("\tBodyPaintingPosX={0}", c.BodyPaintingPosX);
            }
            if (l == null || c.BodyPaintingPosX != l.BodyPaintingPosX) {
                Console.WriteLine("\tBodyPaintingPosY={0}", c.BodyPaintingPosY);
            }
            if (l == null || c.BodyPaintingSize != l.BodyPaintingSize) {
                Console.WriteLine("\tBodyPaintingSize={0}", c.BodyPaintingSize);
            }
            if (l == null || c.BodyPaintingRotation != l.BodyPaintingRotation) {
                Console.WriteLine("\tBodyPaintingRotation={0}", c.BodyPaintingRotation);
            }
            if (l == null || c.BodyPaintingSide != l.BodyPaintingSide) {
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

        private static void PrintHideCostumeMessage(HideCostumeMessage msg, object tag)
        {
            Console.WriteLine("HideCostumeMessage: HideHead={0} AvatarPart={1} AvatarState={2}",msg.HideHead,msg.AvatarPart,msg.AvatarState);
        }

        private static void PrintUseInventoryItemMessage(UseInventoryItemMessage msg, object tag)
        {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintGetMailItemMessage(GetMailItemMessage msg, object tag)
        {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintQueryMailInfoMessage(QueryMailInfoMessage msg, object tag)
        {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintMonsterKilledMessage(MonsterKilledMessage msg, object tag)
        {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintExecuteNpcServerCommandMessage(ExecuteNpcServerCommandMessage msg, object tag)
        {
            Console.WriteLine("ExecuteNpcServerCommandMessage: []");
        }

        private static void PrintRagdollKickedMessage(RagdollKickedMessage msg, object tag)
        {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintCombatRecordMessage(CombatRecordMessage msg, object tag)
        {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintMicroPlayEventMessage(MicroPlayEventMessage msg, object tag)
        {
            Console.WriteLine(msg.ToString());
        }

        private static void PrintMonsterDamageReportMessage(MonsterDamageReportMessage msg, object tag)
        {
            Console.WriteLine("MonsterDamageReportMessage: Target={0}");
            Console.WriteLine(ListToString(msg.TakeDamageList, "", 0));
        }

        private static string PartyMemberInfoToString(PartyMemberInfo m, int i) {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("\tMember[{0}]:", i++);
            sb.AppendFormat("\t\tNexonSN={0}", m.NexonSN);
            sb.AppendFormat("\t\tCharacter={0}", BaseCharacterToString(m.Character));
            sb.AppendFormat("\t\tCharacterID={0}", m.CharacterID);
            sb.AppendFormat("\t\tSlotNum={0}", m.SlotNum);
            sb.AppendFormat("\t\tLevel={0}", m.Level);
            sb.AppendFormat("\t\tState={0}", m.State);
            sb.AppendFormat("\t\tLatestPing={0}", m.LatestPing);
            sb.AppendFormat("\t\tLatestFrameRate={0}", m.LatestFrameRate);
            sb.AppendFormat("\t\tIsReturn={0}", m.IsReturn);
            sb.AppendFormat("\t\tIsEventJumping={0}", m.IsEventJumping);
            return sb.ToString();
        }

        private static void PrintPartyInfoMessage(PartyInfoMessage msg, object tag) {
            Console.WriteLine("PartyInfoMessage:");
            Console.WriteLine("\tPartyID={0}",msg.PartyID);
            Console.WriteLine("\tPartySize={0}",msg.PartySize);
            Console.WriteLine("\tState={0}",msg.State);
            int i = 0;
            foreach (PartyMemberInfo m in msg.Members) {
                Console.WriteLine("\tMember[{0}]:",i++);
                Console.WriteLine("\t\tNexonSN={0}",m.NexonSN);
                Console.WriteLine("\t\tCharacter={0}",BaseCharacterToString(m.Character));
                Console.WriteLine("\t\tCharacterID={0}",m.CharacterID);
                Console.WriteLine("\t\tSlotNum={0}",m.SlotNum);
                Console.WriteLine("\t\tLevel={0}",m.Level);
                Console.WriteLine("\t\tState={0}",m.State);
                Console.WriteLine("\t\tLatestPing={0}",m.LatestPing);
                Console.WriteLine("\t\tLatestFrameRate={0}",m.LatestFrameRate);
                Console.WriteLine("\t\tIsReturn={0}",m.IsReturn);
                Console.WriteLine("\t\tIsEventJumping={0}",m.IsEventJumping);
            }
        }

        private static T GetPrivateProperty<T>(Object msg, string propName) {
            return (T)(msg.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(msg));
        }



        private static void PrintUpdateShipMessage(UpdateShipMessage msg, object tag) {
            //need to use reflection to access the ShipInfo object
            ShipInfo s = GetPrivateProperty<ShipInfo>(msg, "info");
            Console.WriteLine("UpdateShipMessage:");
            Console.WriteLine("\tPartyID={0}", s.PartyID);
            Console.WriteLine("\tShipName={0}", s.ShipName);
            Console.WriteLine("\tPassword={0}", s.Password);
            Console.WriteLine("\tMinLevelConstraint={0}", s.MinLevelConstraint);
            Console.WriteLine("\tMaxLevelConstraint={0}", s.MaxLevelConstraint);
            Console.WriteLine("\tMemberCount={0}", s.MemberCount);
            Console.WriteLine("\tMaxShipMemberCount={0}", s.MaxShipMemberCount);
            Console.WriteLine("\tstringQuestID={0}", s.QuestID);
            Console.WriteLine("\tIsHuntingQuest={0}", s.IsHuntingQuest);
            Console.WriteLine("\tIsGiantRaid={0}", s.IsGiantRaid);
            Console.WriteLine("\tSwearID={0}", s.SwearID);
            Console.WriteLine("\tRestTime={0}", s.RestTime);
            Console.WriteLine("\tReinforceAllowed={0}", s.ReinforceAllowed);
            Console.WriteLine("\tAdultRule={0}", s.AdultRule);
            Console.WriteLine("\tState={0}", s.State);
            Console.WriteLine("\tPartyBonusCount={0}", s.PartyBonusCount);
            Console.WriteLine("\tPartyBonusRatio={0}", s.PartyBonusRatio);
            Console.WriteLine("\tIsPartyOnly={0}", s.IsPartyOnly);
            Console.WriteLine("\tHostPing={0}", s.HostPing);
            Console.WriteLine("\tHostFrameRate={0}", s.HostFrameRate);
            Console.WriteLine("\tDifficulty={0}", s.Difficulty);
            Console.WriteLine("\tIsReturn={0}", s.IsReturn);
            Console.WriteLine("\tIsSeason={0}", s.IsSeason2);
            Console.WriteLine("\tIsPracticeMode={0}", s.IsPracticeMode);
            Console.WriteLine("\tUserDSMode={0}", s.UserDSMode);
            Console.WriteLine("\tselectedBossQuestIDInfos={0}", String.Join(",", s.selectedBossQuestIDInfos));
            int i = 0;
            foreach (PartyMemberInfo m in s.Members) {
                Console.WriteLine(PartyMemberInfoToString(m,i++));
            }
            //could get an infinite loop if I tried to parse this fully
            Console.WriteLine("\tShipList=[{0}]",String.Join(",",s.ShipListNode.List));
            
        }

        private static void PrintPayCoinCompletedMessage(PayCoinCompletedMessage msg, object tag) {
            Console.WriteLine("PayCoinCompletedMessage:");
            foreach (PaidCoinInfo p in msg.Coininfos) {
                Console.WriteLine("\tSlot={0} SilverCoin=[{1}] PlatinumCoinOwner={2} PlatinumCoinType={3}",p.Slot,String.Join(",",p.SilverCoin),p.PlatinumCoinOwner,p.PlatinumCoinType);
            }
        }

        public static ConnectionRequestMessage lastConnectionRequestMsg = null;

        private static void ConnectionRequestMessage(ConnectionRequestMessage msg, object tag) {
            //TODO: use this info to decrypt relay messages
            Console.WriteLine("ConnectionRequestMessage:");
            Console.WriteLine("\tAddress={0}",msg.Address);
            Console.WriteLine("\tPort={0}",msg.Port);
            Console.WriteLine("\tPosixTime={0}",msg.PosixTime);
            Console.WriteLine("\tKey={0}",msg.Key);
            Console.WriteLine("\tCategory={0}",msg.Category);
            Console.WriteLine("\tPingHostCID={0}",msg.PingHostCID);
            Console.WriteLine("\tGroupID={0}",msg.GroupID);
            lastConnectionRequestMsg = msg;
        }

        private static void PrintLaunchShipGrantedMessage(LaunchShipGrantedMessage msg, object tag) {
            Console.WriteLine("\tLaunchShipGrantedMessage:");
            Console.WriteLine("\tQuestID={0}",msg.QuestID);
            Console.WriteLine("\tAdultRule={0}",msg.AdultRule);
            Console.WriteLine("\tIsPracticeMode={0}",msg.IsPracticeMode);
            Console.WriteLine("\tHostInfo:");
            Console.WriteLine(GameJoinMemberInfoToString(msg.HostInfo,2));
        }
    }
}
