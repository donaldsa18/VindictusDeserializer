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

namespace PacketCap
{
    class MessagePrinter
    {
        private Dictionary<Guid, int> categoryDict = new Dictionary<Guid, int>();
        private MessageHandlerFactory mf;
        public void registerPrinters(MessageHandlerFactory mf, Dictionary<int, Guid> getGuid) {
            this.mf = mf;
            foreach (KeyValuePair<int, Guid> entry in getGuid) {
                this.categoryDict.Add(entry.Value, entry.Key);
            }
            this.Register<SyncFeatureMatrixMessage>(new Action<SyncFeatureMatrixMessage, object>(PrintSyncFeatureMatrixMessage));
            this.Register<GiveAPMessage>(new Action<GiveAPMessage, object>(PrintGiveAPMessage));
            this.Register<UserIDMessage>(new Action<UserIDMessage, object>(PrintUserIDMessage));
            this.Register<CharacterListMessage>(new Action<CharacterListMessage, object>(PrintCharacterListMessage));
            this.Register<AnswerFinishQuestMessage>(new Action<AnswerFinishQuestMessage, object>(PrintAnswerFinishQuestMessage));
            this.Register<ExchangeMileageResultMessage>(new Action<ExchangeMileageResultMessage, object>(PrintExchangeMileageResultMessage));
            this.Register<SecuredOperationMessage>(new Action<SecuredOperationMessage, object>(PrintSecuredOperationMessage));
            this.Register<UseInventoryItemWithCountMessage>(new Action<UseInventoryItemWithCountMessage, object>(PrintUseInventoryItemWithCountMessage));
            this.Register<AllUserJoinCompleteMessage>(new Action<AllUserJoinCompleteMessage, object>(PrintAllUserJoinCompleteMessage));
            this.Register<RequestMarbleProcessNodeMessage>(new Action<RequestMarbleProcessNodeMessage, object>(PrintRequestMarbleProcessNodeMessage));
            this.Register<QuerySharedInventoryMessage>(new Action<QuerySharedInventoryMessage, object>(PrintQuerySharedInventoryMessage));
            this.Register<UpdateHistoryBookMessage>(new Action<UpdateHistoryBookMessage, object>(PrintUpdateHistoryBookMessage));
            this.Register<RequestItemCombinationMessage>(new Action<RequestItemCombinationMessage, object>(PrintRequestItemCombinationMessage));
            this.Register<GiveCashShopDiscountCouponResultMessage>(new Action<GiveCashShopDiscountCouponResultMessage, object>(PrintGiveCashShopDiscountCouponResultMessage));
            this.Register<UpdateHousingPropsMessage>(new Action<UpdateHousingPropsMessage, object>(PrintUpdateHousingPropsMessage));
            this.Register<HousingMemberInfoMessage>(new Action<HousingMemberInfoMessage, object>(PrintHousingMemberInfoMessage));
            this.Register<HousingKickMessage>(new Action<HousingKickMessage, object>(PrintHousingKickMessage));
            this.Register<HousingInvitedMessage>(new Action<HousingInvitedMessage, object>(PrintHousingInvitedMessage));
            this.Register<HousingHostRestartingMessage>(new Action<HousingHostRestartingMessage, object>(PrintHousingHostRestartingMessage));
            this.Register<EnterHousingMessage>(new Action<EnterHousingMessage, object>(PrintEnterHousingMessage));
            this.Register<EndPendingDialogMessage>(new Action<EndPendingDialogMessage, object>(PrintEndPendingDialogMessage));
            this.Register<CreateHousingMessage>(new Action<CreateHousingMessage, object>(PrintCreateHousingMessage));
            this.Register<HotSpringRequestPotionEffectMessage>(new Action<HotSpringRequestPotionEffectMessage, object>(PrintHotSpringRequestPotionEffectMessage));
            this.Register<HotSpringAddPotionMessage>(new Action<HotSpringAddPotionMessage, object>(PrintHotSpringAddPotionMessage));
            this.Register<BurnItemsMessage>(new Action<BurnItemsMessage, object>(PrintBurnItemsMessage));
            this.Register<FreeTitleNameCheckMessage>(new Action<FreeTitleNameCheckMessage, object>(PrintFreeTitleNameCheckMessage));
            this.Register<BurnRewardItemsMessage>(new Action<BurnRewardItemsMessage, object>(PrintBurnRewardItemsMessage));
            this.Register<AllUserGoalEventModifyMessage>(new Action<AllUserGoalEventModifyMessage, object>(PrintAllUserGoalEventModifyMessage));
            this.Register<AvatarSynthesisItemMessage>(new Action<AvatarSynthesisItemMessage, object>(PrintAvatarSynthesisItemMessage));
            this.Register<GetFriendshipPointMessage>(new Action<GetFriendshipPointMessage, object>(PrintGetFriendshipPointMessage));
            this.Register<ExchangeMileageMessage>(new Action<ExchangeMileageMessage, object>(PrintExchangeMileageMessage));
            this.Register<CaptchaResponseMessage>(new Action<CaptchaResponseMessage, object>(PrintCaptchaResponseMessage));
            this.Register<GuildChatMessage>(new Action<GuildChatMessage, object>(PrintGuildChatMessage));
            this.Register<ChangeMasterMessage>(new Action<ChangeMasterMessage, object>(PrintChangeMasterMessage));
            this.Register<GuildGainGPMessage>(new Action<GuildGainGPMessage, object>(PrintGuildGainGPMessage));
            this.Register<GuildLevelUpMessage>(new Action<GuildLevelUpMessage, object>(PrintGuildLevelUpMessage));
            this.Register<HousingRoomListMessage>(new Action<HousingRoomListMessage, object>(PrintHousingRoomListMessage));
            this.Register<HotSpringRequestInfoResultMessage>(new Action<HotSpringRequestInfoResultMessage, object>(PrintHotSpringRequestInfoResultMessage));
            this.Register<BurnJackpotMessage>(new Action<BurnJackpotMessage, object>(PrintBurnJackpotMessage));
            this.Register<RequestMarbleCastDiceMessage>(new Action<RequestMarbleCastDiceMessage, object>(PrintRequestMarbleCastDiceMessage));
            this.Register<HousingPartyInfoMessage>(new Action<HousingPartyInfoMessage, object>(PrintHousingPartyInfoMessage));
            this.Register<AddFriendShipResultMessage>(new Action<AddFriendShipResultMessage, object>(PrintAddFriendShipResultMessage));
            this.Register<HousingInvitationRejectMessage>(new Action<HousingInvitationRejectMessage, object>(PrintHousingInvitationRejectMessage));
            this.Register<EnhanceSuccessRatioDebugMessage>(new Action<EnhanceSuccessRatioDebugMessage, object>(PrintEnhanceSuccessRatioDebugMessage));
            this.Register<HasSecondPasswordMessage>(new Action<HasSecondPasswordMessage, object>(PrintHasSecondPasswordMessage));
            this.Register<GetUserIDMessage>(new Action<GetUserIDMessage, object>(PrintGetUserIDMessage));
            this.Register<MakeNamedRingMessage>(new Action<MakeNamedRingMessage, object>(PrintMakeNamedRingMessage));
            this.Register<MarbleInfoResultMessage>(new Action<MarbleInfoResultMessage, object>(PrintMarbleInfoResultMessage));
            this.Register<RequestPartChangingMessage>(new Action<RequestPartChangingMessage, object>(PrintRequestPartChangingMessage));
            this.Register<CaptchaResponseResultMessage>(new Action<CaptchaResponseResultMessage, object>(PrintCaptchaResponseResultMessage));
            this.Register<JoinGuildChatRoomMessage>(new Action<JoinGuildChatRoomMessage, object>(PrintJoinGuildChatRoomMessage));
            this.Register<HousingListMessage>(new Action<HousingListMessage, object>(PrintHousingListMessage));
            this.Register<AvatarSynthesisMaterialRecipesMessage>(new Action<AvatarSynthesisMaterialRecipesMessage, object>(PrintAvatarSynthesisMaterialRecipesMessage));
            this.Register<AvatarSynthesisRequestMessage>(new Action<AvatarSynthesisRequestMessage, object>(PrintAvatarSynthesisRequestMessage));
            this.Register<GameResourceRespondMessage>(new Action<GameResourceRespondMessage, object>(PrintGameResourceRespondMessage));
            this.Register<AllUserGoalEventMessage>(new Action<AllUserGoalEventMessage, object>(PrintAllUserGoalEventMessage));
            this.Register<LeaveHousingMessage>(new Action<LeaveHousingMessage, object>(PrintLeaveHousingMessage));
            this.Register<DecomposeItemResultMessage>(new Action<DecomposeItemResultMessage, object>(PrintDecomposeItemResultMessage));
            this.Register<QueryAvatarSynthesisMaterialRecipesMessage>(new Action<QueryAvatarSynthesisMaterialRecipesMessage, object>(PrintQueryAvatarSynthesisMaterialRecipesMessage));
            this.Register<SearchHousingRoomMessage>(new Action<SearchHousingRoomMessage, object>(PrintSearchHousingRoomMessage));
            this.Register<MarbleSetTimerMessage>(new Action<MarbleSetTimerMessage, object>(PrintMarbleSetTimerMessage));
            this.Register<FreeTitleNameCheckResultMessage>(new Action<FreeTitleNameCheckResultMessage, object>(PrintFreeTitleNameCheckResultMessage));
            this.Register<InsertBlessStoneCompleteMessage>(new Action<InsertBlessStoneCompleteMessage, object>(PrintInsertBlessStoneCompleteMessage));
            this.Register<EnchantLimitlessMessage>(new Action<EnchantLimitlessMessage, object>(PrintEnchantLimitlessMessage));
            this.Register<RequestBraceletCombinationMessage>(new Action<RequestBraceletCombinationMessage, object>(PrintRequestBraceletCombinationMessage));
            this.Register<SwapHousingItemMessage>(new Action<SwapHousingItemMessage, object>(PrintSwapHousingItemMessage));
            this.Register<MarbleProcessNodeResultMessage>(new Action<MarbleProcessNodeResultMessage, object>(PrintMarbleProcessNodeResultMessage));
            this.Register<BurnGaugeRequestMessage>(new Action<BurnGaugeRequestMessage, object>(PrintBurnGaugeRequestMessage));
            this.Register<SetQuoteMessage>(new Action<SetQuoteMessage, object>(PrintSetQuoteMessage));
            this.Register<RequestAttendanceRewardMessage>(new Action<RequestAttendanceRewardMessage, object>(PrintRequestAttendanceRewardMessage));
            this.Register<HousingGameHostedMessage>(new Action<HousingGameHostedMessage, object>(PrintHousingGameHostedMessage));
            this.Register<HousingKickedMessage>(new Action<HousingKickedMessage, object>(PrintHousingKickedMessage));
            this.Register<RequestAddPeerMessage>(new Action<RequestAddPeerMessage, object>(PrintRequestAddPeerMessage));
            this.Register<MaxDurabilityRepairItemMessage>(new Action<MaxDurabilityRepairItemMessage, object>(PrintMaxDurabilityRepairItemMessage));
            this.Register<SecondPasswordResultMessage>(new Action<SecondPasswordResultMessage, object>(PrintSecondPasswordResultMessage));
            this.Register<CharacterCommonInfoMessage>(new Action<CharacterCommonInfoMessage, object>(PrintCharacterCommonInfoMessage));
            this.Register<ChannelServerAddress>(new Action<ChannelServerAddress, object>(PrintChannelServerAddress));
            this.Register<SystemMessage>(new Action<SystemMessage, object>(PrintSystemMessage));
            this.Register<NpcTalkMessage>(new Action<NpcTalkMessage, object>(PrintNpcTalkMessage));
            this.Register<HousingStartGrantedMessage>(new Action<HousingStartGrantedMessage, object>(PrintHousingStartGrantedMessage));
            this.Register<UpdateStoryGuideMessage>(new Action<UpdateStoryGuideMessage, object>(PrintUpdateStoryGuideMessage));
            this.Register<AddFriendshipInfoMessage>(new Action<AddFriendshipInfoMessage, object>(PrintAddFriendshipInfoMessage));
            this.Register<SkillListMessage>(new Action<SkillListMessage, object>(PrintSkillListMessage));
            this.Register<LoginOkMessage>(new Action<LoginOkMessage, object>(PrintLoginOkMessage));
            this.Register<MailListMessage>(new Action<MailListMessage, object>(PrintMailListMessage));
            this.Register<TodayMissionInitializeMessage>(new Action<TodayMissionInitializeMessage, object>(PrintTodayMissionInitializeMessage));
            this.Register<APMessage>(new Action<APMessage, object>(PrintAPMessage));
            this.Register<GuildResultMessage>(new Action<GuildResultMessage, object>(PrintGuildResultMessage));
            this.Register<CostumeUpdateMessage>(new Action<CostumeUpdateMessage, object>(PrintCostumeUpdateMessage));
            this.Register<EquipmentInfoMessage>(new Action<EquipmentInfoMessage, object>(PrintEquipmentInfoMessage));
            this.Register<UpdateStatMessage>(new Action<UpdateStatMessage, object>(PrintUpdateStatMessage));
            this.Register<UpdateInventoryInfoMessage>(new Action<UpdateInventoryInfoMessage, object>(PrintUpdateInventoryInfoMessage));
            this.Register<StatusEffectUpdated>(new Action<StatusEffectUpdated, object>(PrintStatusEffectUpdated));
            this.Register<QuestProgressMessage>(new Action<QuestProgressMessage, object>(PrintQuestProgressMessage));
            this.Register<FriendshipInfoListMessage>(new Action<FriendshipInfoListMessage, object>(PrintFriendshipInfoListMessage));
            this.Register<NpcListMessage>(new Action<NpcListMessage, object>(PrintNpcListMessage));
            this.Register<TradeSearchResult>(new Action<TradeSearchResult, object>(PrintTradeSearchResult));
            this.Register<InventoryInfoMessage>(new Action<InventoryInfoMessage, object>(PrintInventoryInfoMessage));
            this.Register<AskSecondPasswordMessage>(new Action<AskSecondPasswordMessage, object>(PrintAskSecondPasswordMessage));
            this.Register<NoticeGameEnvironmentMessage>(new Action<NoticeGameEnvironmentMessage, object>(PrintNoticeGameEnvironmentMessage));
            this.Register<SpSkillMessage>(new Action<SpSkillMessage, object>(PrintSpSkillMessage));
            this.Register<VocationSkillListMessage>(new Action<VocationSkillListMessage, object>(PrintVocationSkillListMessage));
            this.Register<WhisperFilterListMessage>(new Action<WhisperFilterListMessage, object>(PrintWhisperFilterListMessage));
            this.Register<GetCharacterMissionStatusMessage>(new Action<GetCharacterMissionStatusMessage, object>(PrintGetCharacterMissionStatusMessage));
            this.Register<SelectPatternMessage>(new Action<SelectPatternMessage, object>(PrintSelectPatternMessage));
            this.Register<QueryHousingItemsMessage>(new Action<QueryHousingItemsMessage, object>(PrintQueryHousingItemsMessage));
            this.Register<TitleListMessage>(new Action<TitleListMessage, object>(PrintTitleListMessage));
            this.Register<FishingResultMessage>(new Action<FishingResultMessage, object>(PrintFishingResultMessage));
            this.Register<PetListMessage>(new Action<PetListMessage, object>(PrintPetListMessage));
            this.Register<PetFeedListMessage>(new Action<PetFeedListMessage, object>(PrintPetFeedListMessage));
            this.Register<SharedInventoryInfoMessage>(new Action<SharedInventoryInfoMessage, object>(PrintSharedInventoryInfoMessage));
            this.Register<TirCoinInfoMessage>(new Action<TirCoinInfoMessage, object>(PrintTirCoinInfoMessage));
            this.Register<RankAlarmInfoMessage>(new Action<RankAlarmInfoMessage, object>(PrintRankAlarmInfoMessage));
            this.Register<UpdateBattleInventoryInTownMessage>(new Action<UpdateBattleInventoryInTownMessage, object>(PrintUpdateBattleInventoryInTownMessage));
            this.Register<BingoBoardResultMessage>(new Action<BingoBoardResultMessage, object>(PrintBingoBoardResultMessage));
            this.Register<RandomRankInfoMessage>(new Action<RandomRankInfoMessage, object>(PrintRandomRankInfoMessage));
            this.Register<JoinHousingMessage>(new Action<JoinHousingMessage, object>(PrintJoinHousingMessage));
            this.Register<AttendanceInfoMessage>(new Action<AttendanceInfoMessage, object>(PrintAttendanceInfoMessage));
            this.Register<UpdateTitleMessage>(new Action<UpdateTitleMessage, object>(PrintUpdateTitleMessage));
            this.Register<GuildInventoryInfoMessage>(new Action<GuildInventoryInfoMessage, object>(PrintGuildInventoryInfoMessage));
            this.Register<CashshopTirCoinResultMessage>(new Action<CashshopTirCoinResultMessage, object>(PrintCashshopTirCoinResultMessage));
            this.Register<GiveCashShopDiscountCouponMessage>(new Action<GiveCashShopDiscountCouponMessage, object>(PrintGiveCashShopDiscountCouponMessage));
            this.Register<NextSectorMessage>(new Action<NextSectorMessage, object>(PrintNextSectorMessage));
            this.Register<BurnGauge>(new Action<BurnGauge, object>(PrintBurnGauge));
            this.Register<StoryLinesMessage>(new Action<StoryLinesMessage, object>(PrintStoryLinesMessage));
            this.Register<QuickSlotInfoMessage>(new Action<QuickSlotInfoMessage, object>(PrintQuickSlotInfoMessage));
            this.Register<ManufactureInfoMessage>(new Action<ManufactureInfoMessage, object>(PrintManufactureInfoMessage));
            this.Register<GuildInfoMessage>(new Action<GuildInfoMessage, object>(PrintGuildInfoMessage));
            //this.Register<>(new Action<, object>(Print));
            //this.Register<>(new Action<, object>(Print));
            //this.Register<>(new Action<, object>(Print));

        }
        private void Register<T>(Action<T,object> printer) {
            if (categoryDict.ContainsKey(typeof(T).GUID)) {
                mf.Register<T>(printer,categoryDict[typeof(T).GUID]);
            }
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

        private static void PrintCharacterListMessage(CharacterListMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
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
            switch (msg.Operation) {
                case SecuredOperationType.Public:
                    sb.Append("Public ");
                    break;
                case SecuredOperationType.OpenCashShopDialog:
                    sb.Append("OpenCashShopDialog ");
                    break;
                case SecuredOperationType.OpenTradeDialog:
                    sb.Append("OpenTradeDialog ");
                    break;
            }
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

        private static string UpdateTypeToString(UpdateHistoryBookMessage.UpdateType t) {
            switch (t)
            {
                case UpdateHistoryBookMessage.UpdateType.Unknown:
                    return "Unknown";
                case UpdateHistoryBookMessage.UpdateType.Remove:
                    return "Remove";
                case UpdateHistoryBookMessage.UpdateType.Add:
                    return "Add";
                case UpdateHistoryBookMessage.UpdateType.Full:
                    return "Full";
                default:
                    return "";
            }
        }

        private static void PrintUpdateHistoryBookMessage(UpdateHistoryBookMessage msg, object tag) {
            Console.WriteLine("UpdateHistoryBookMessage:");
            Console.WriteLine("\tType={0}",UpdateTypeToString(msg.Type));

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
            String t = new string('\t', tabs);
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}Name={1}", t, m.Name);
            sb.AppendFormat("{0}Level={1}", t, m.Level);
            String c = BaseCharacterToString((BaseCharacter)m.BaseClass);

            sb.AppendFormat("{0}Class={1}", t, c);
            sb.AppendFormat("{0}TitleID={1}", t, m.TitleID);//translate using heroes.db3 and translation xml
            sb.AppendFormat("{0}TitleCount={1}", t, m.TitleCount);


            sb.AppendFormat(DictToString<string, int>(m.Stats, "Stats", 1+tabs));
            sb.AppendFormat(CostumeInfoToString(m.CostumeInfo, tabs));
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

        private static string EnterHousingTypeToString(EnterHousingType t) {
            switch (t)
            {
                case EnterHousingType.Default:
                    return "Default";
                case EnterHousingType.OpenPublic:
                    return "OpenPublic";
                case EnterHousingType.OpenPrivate:
                    return "OpenPrivate";
                case EnterHousingType.EnterAny:
                    return "EnterAny";
                case EnterHousingType.EnterSpecified:
                    return "EnterSpecified";
                default:
                    return "";
            }
        }

        private static void PrintEnterHousingMessage(EnterHousingMessage msg, object tag) {
            String housingType = EnterHousingTypeToString(msg.EnterType);
            Console.WriteLine("EnterHousingMessage: CharacterName={0} HousingIndex={1} EnterType={2} HousingPlayID={3}", msg.CharacterName, msg.HousingIndex, housingType, msg.HousingPlayID);
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
            sb.Append(t);
            sb.Append(name);
            sb.Append(":\n");
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

        private static string AddFriendShipResultMessageToString(AddFriendShipResultMessage.AddFriendShipResult r) {
            switch (r)
            {
                case AddFriendShipResultMessage.AddFriendShipResult.Result_Already_Added:
                    return "Result_Already_Added";
                case AddFriendShipResultMessage.AddFriendShipResult.Result_Character_NotFounded:
                    return "Result_Character_NotFounded";
                case AddFriendShipResultMessage.AddFriendShipResult.Result_Exception:
                    return "Result_Exception";
                case AddFriendShipResultMessage.AddFriendShipResult.Result_FriendCount_Max:
                    return "Result_FriendCount_Max";
                case AddFriendShipResultMessage.AddFriendShipResult.Result_Ok:
                    return "Result_Ok";
                case AddFriendShipResultMessage.AddFriendShipResult.Result_SameID:
                    return "Result_SameID";
                default:
                    return "";
            }
        }

        private static void PrintAddFriendShipResultMessage(AddFriendShipResultMessage msg, object tag) {
            String result = AddFriendShipResultMessageToString((AddFriendShipResultMessage.AddFriendShipResult)msg.Result);
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

        private static string DecomposeItemResultEXPToString(DecomposeItemResultEXP d) {
            switch (d)
            {
                case DecomposeItemResultEXP.fail:
                    return "fail";
                case DecomposeItemResultEXP.increase:
                    return "increase";
                case DecomposeItemResultEXP.not_increase:
                    return "not_increase";
                case DecomposeItemResultEXP.not_increase_already_max:
                    return "not_increase_already_max";
                case DecomposeItemResultEXP.part_extract:
                    return "part_extract";
                default:
                    return "";
            }
        }

        private static void PrintDecomposeItemResultMessage(DecomposeItemResultMessage msg, object tag) {

            String item = DecomposeItemResultEXPToString(msg.ResultEXP);
            Console.WriteLine("DecomposeItemResultMessage: ResultEXP={0}",item);
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

        private static string BlessStoneToString(BlessStoneType t) {
            switch (t)
            {
                case BlessStoneType.NONE:
                    return "NONE";
                case BlessStoneType.ALL:
                    return "ALL";
                case BlessStoneType.EXP:
                    return "EXP";
                case BlessStoneType.LUCK:
                    return "LUCK";
                case BlessStoneType.AP:
                    return "AP";
                case BlessStoneType.StoneTypeCount:
                    return "StoneTypeCount";
                default:
                    return "";
            }
        }

        private static void PrintInsertBlessStoneCompleteMessage(InsertBlessStoneCompleteMessage msg, object tag) {
            Console.WriteLine("InsertBlessStoneCompleteMessage:");
            Console.WriteLine("\tSlot={0}",msg.Slot);
            Console.WriteLine("\tOwnerList=[{0}]",String.Join(",",msg.OwnerList));
            StringBuilder sb = new StringBuilder();
            foreach (BlessStoneType t in msg.TypeList) {
                sb.Append(BlessStoneToString(t));
                sb.Append(",");
            }
            Console.WriteLine("\tTypeList=[{0}]",sb.ToString());
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

        private static String VocationToString(VocationEnum v) {
            switch (v) {
                case VocationEnum.Invalid:
                    return "Invalid";
                case VocationEnum.None:
                    return "None";
                case VocationEnum.Paladin:
                    return "Paladin";
                case VocationEnum.DarkKnight:
                    return "DarkKnight";
                default:
                    return "";
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

            return sb.ToString();
        }

        private static String ColorDictToString(IDictionary<int,int> dict, String name, int numTabs) {
            HashSet<int> keys = new HashSet<int>();
            StringBuilder sb = new StringBuilder();
            String t = new string('\t', numTabs);
            sb.Append(t);
            sb.Append(name);
            sb.Append(":\n");
            if (dict == null) {
                return sb.ToString();
            }
            t = new string('\t', numTabs+1);
            foreach (KeyValuePair<int, int> entry in dict) {
                keys.Add(entry.Key / 3);
            }
            foreach (int key in keys) {
                int val = -1;
                sb.Append(t);
                sb.Append(key);
                sb.Append("=(");
                dict.TryGetValue(key, out val);
                sb.Append(val);
                sb.Append(",");
                val = -1;
                dict.TryGetValue(key+1, out val);
                sb.Append(val);
                sb.Append(",");
                val = -1;
                dict.TryGetValue(key+2, out val);
                sb.Append(val);
                sb.Append(")\n");
            }
            return sb.ToString();
        }

        private static String CostumeInfoToString(CostumeInfo c, int numTabs) {
            //TODO: send to database
            if (c == null) {
                return "";
            }
            String t = new string('\t', numTabs);
            StringBuilder sb = new StringBuilder();
            sb.Append(t);
            sb.Append("CostumeInfo:\n");
            t = new string('\t', numTabs+1);
            sb.Append(t);
            sb.Append("Shineness=");
            sb.Append(c.Shineness);
            sb.Append("\n");
            sb.Append(t);
            sb.Append("Height=");
            sb.Append(c.Height);
            sb.Append("\n");
            sb.Append(t);
            sb.Append("Bust=");
            sb.Append(c.Bust);
            sb.Append("\n");
            sb.Append(t);
            sb.Append("PaintingPosX=");
            sb.Append(c.PaintingPosX);
            sb.Append("\n");
            sb.Append(t);
            sb.Append("PaintingPosY=");
            sb.Append(c.PaintingPosY);
            sb.Append("\n");
            sb.Append(t);
            sb.Append("PaintingRotation=");
            sb.Append(c.PaintingRotation);
            sb.Append("\n");
            sb.Append(t);
            sb.Append("PaintingSize=");
            sb.Append(c.PaintingSize);
            sb.Append("\n");
            sb.Append(t);
            sb.Append("HideHeadCostume=");
            sb.Append(c.HideHeadCostume);
            sb.Append("\n");
            sb.Append(t);
            sb.Append("CafeType=");
            sb.Append(c.CafeType);
            sb.Append("\n");
            sb.Append(t);
            sb.Append("IsReturn=");
            sb.Append(c.IsReturn);
            sb.Append("\n");
            sb.Append(t);
            sb.Append("VIPCode=");
            sb.Append(c.VIPCode);
            sb.Append("\n");

            sb.Append(DictToString<int,int>(c.CostumeTypeInfo, "CostumeTypeInfo", numTabs));
            sb.Append(ColorDictToString(c.ColorInfo, "ColorInfo", numTabs));
            sb.Append(DictToString<int, bool>(c.AvatarInfo, "AvatarInfo", numTabs));
            sb.Append(DictToString<int, int>(c.AvatarHideInfo, "AvatarHideInfo", numTabs));
            sb.Append(DictToString<int, byte>(c.PollutionInfo, "PollutionInfo", numTabs));
            sb.Append(DictToString<int, int>(c.EffectInfo, "EffectInfo", numTabs));
            sb.Append(DictToString<int, int>(c.DecorationInfo, "DecorationInfo", numTabs));
            sb.Append(ColorDictToString(c.DecorationColorInfo, "DecorationColorInfo", numTabs));
            sb.Append(DictToString<int, float>(c.BodyShapeInfo, "BodyShapeInfo", numTabs));
            //sb.Append(DictToString<int, int>(c, "", numTabs));
            
            //c.CostumeTypeInfo == Costume.CostumeSN.Value
            return sb.ToString();
        }

        private static void PrintCharacterCommonInfoMessage(CharacterCommonInfoMessage msg, object tag) {
            CharacterSummary c = msg.Info;
            Console.WriteLine("CharacterCommonInfoMessage:");
            Console.WriteLine("\tCharacterID={0}",c.CharacterID);
            String basechar = BaseCharacterToString(c.BaseCharacter);
            Console.WriteLine("\tBaseCharacter={0}",basechar);
            Console.WriteLine("\tLevel={0}",c.Level);
            Console.WriteLine("\tTitle={0}",c.Title);
            Console.WriteLine("\tTitleCount={0}",c.TitleCount);
            Console.WriteLine(CostumeInfoToString(c.Costume,1));
            

            Console.WriteLine("\tQuote={0}",c.Quote);
            Console.WriteLine("\tGuildName={0}",c.GuildName);
            String v = VocationToString(c.VocationClass);
            Console.WriteLine("\tVocation={0}",v);
            if (c.Pet != null)
            {
                Console.WriteLine("\tPet: Name={1}, Type={2}", c.Pet.PetName, c.Pet.PetType);
            }
            Console.WriteLine("\tFreeTitleName={0}",c.FreeTitleName);
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

        private static void PrintMailListMessage(MailListMessage msg, object tag) {
            Console.WriteLine("MailListMessage:");
            Console.WriteLine(ListToString<BriefMailInfo>(msg.ReceivedMailList, "ReceivedMailList", 1));
            Console.WriteLine(ListToString<BriefMailInfo>(msg.SentMailList,"SentMailList",1));
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
            Console.WriteLine("CostumeUpdateMessage:");
            Console.WriteLine(CostumeInfoToString(msg.CostumeInfo, 1));
        }

        private static void PrintEquipmentInfoMessage(EquipmentInfoMessage msg, object tag) {
            Console.WriteLine(DictToString<int, long>(msg.EquipInfos, "EquipmentInfoMessage", 0));
        }

        private static void PrintUpdateStatMessage(UpdateStatMessage msg, object tag) {
            Console.WriteLine(DictToString<string, int>(msg.Stat, "UpdateStatMessage",0));
        }

        private static void PrintUpdateInventoryInfoMessage(UpdateInventoryInfoMessage msg, object tag) {
            Console.WriteLine("UpdateInventoryInfoMessage: [?]");
        }

        private static void PrintStatusEffectUpdated(StatusEffectUpdated msg, object tag) {
            Console.WriteLine("StatusEffectUpdated:");
            Console.WriteLine("\tCharacterName={0}",msg.CharacterName);
            Console.WriteLine("\tStatusEffects:");
            if (msg.StatusEffects == null) {
                return;
            }
            foreach (StatusEffectElement e in msg.StatusEffects) {
                Console.WriteLine("\t\tType={0} Level={1} RemainTime={2} CombatCount={3}",e.Type,e.Level,e.RemainTime,e.CombatCount);
            }
        }

        private static void PrintQuestProgressMessage(QuestProgressMessage msg, object tag) {
            Console.WriteLine("QuestProgressMessage:");
            Console.WriteLine(ListToString<QuestProgressInfo>(msg.QuestProgress, "QuestProgress", 1));
            Console.WriteLine(ListToString<AchieveGoalInfo>(msg.AchievedGoals, "AchievedGoals", 1));
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

        private static void PrintInventoryInfoMessage(InventoryInfoMessage msg, object tag) {
            //TODO: db connect to share inventory
            Console.WriteLine("InventoryInfoMessage:");
            Console.WriteLine("\tStorageInfos:");
            foreach (StorageInfo info in msg.StorageInfos)
            {
                Console.WriteLine("\t\tstorageID={0} isAvailable={1} storageName={2} storageTag={3}", info.StorageID, info.IsAvailable, info.StorageName, info.StorageTag);
            }
            Console.WriteLine(ListToString<SlotInfo>(msg.SlotInfos, "SlotInfos", 1));
            Console.WriteLine(DictToString<int, long>(msg.EquipmentInfo, "EquipmentInfo", 1));
            Console.WriteLine(DictToString<int, string>(msg.QuickSlotInfo.SlotItemClasses, "QuickSlotInfo", 1));
            Console.WriteLine("\tUnequippableParts=[{0}]",String.Join(",",msg.UnequippableParts));
            
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

        private static void PrintTitleListMessage(TitleListMessage msg, object tag) {
            Console.WriteLine("TitleListMessage:");
            Console.WriteLine(ListToString<TitleSlotInfo>(msg.AccountTitles, "AccountTitles", 1));
            Console.WriteLine(ListToString<TitleSlotInfo>(msg.Titles, "Titles", 1));
        }

        private static void PrintFishingResultMessage(FishingResultMessage msg, object tag) {
            Console.WriteLine(ListToString<FishingResultInfo>(msg.FishingResult, "FishingResultMessage", 0));
        }

        private static void PrintPetListMessage(PetListMessage msg, object tag) {
            Console.WriteLine("PetListMessage:");
            Console.WriteLine("\tIsTotalPetList={0}", msg.IsTotalPetList);
            Console.WriteLine(ListToString<PetStatusInfo>(msg.PetList, "PetList", 1));
        }

        private static void PrintPetFeedListMessage(PetFeedListMessage msg, object tag) {
            Console.WriteLine("PetFeedListMessage:");
            Console.WriteLine("\tIsTotalPetList={0}", msg.IsTotalPetList);
            Console.WriteLine(ListToString<PetFeedElement>(msg.PetFeedList, "PetFeedList", 1));
        }

        private static void PrintSharedInventoryInfoMessage(SharedInventoryInfoMessage msg, object tag) {
            Console.WriteLine("SharedInventoryInfoMessage:");
            Console.WriteLine("\tStorageInfos:");
            foreach (StorageInfo info in msg.StorageInfos) {
                Console.WriteLine("\t\tstorageID={0} isAvailable={1} storageName={2} storageTag={3}",info.StorageID,info.IsAvailable,info.StorageName,info.StorageTag);
            }
            Console.WriteLine(ListToString<SlotInfo>(msg.SlotInfos, "SlotInfos", 1));
        }

        private static void PrintTirCoinInfoMessage(TirCoinInfoMessage msg, object tag) {
            Console.WriteLine(DictToString<byte, int>(msg.TirCoinInfo, "TirCoinInfoMessage", 0));
        }

        private static void PrintRankAlarmInfoMessage(RankAlarmInfoMessage msg, object tag) {
            Console.WriteLine(ListToString<RankAlarmInfo>(msg.RankAlarm,"RankAlarmInfoMessage", 0));
        }

        private static void PrintUpdateBattleInventoryInTownMessage(UpdateBattleInventoryInTownMessage msg, object tag) {
            Console.WriteLine(msg.ToString());
        }

        private static string BingoResultToString(BingoBoardResultMessage.Bingo_Result r) {
            switch (r) {
                case BingoBoardResultMessage.Bingo_Result.Result_Bingo_InitError:
                    return "Result_Bingo_InitError";
                case BingoBoardResultMessage.Bingo_Result.Result_Bingo_Expired:
                    return "Result_Bingo_Expired";
                case BingoBoardResultMessage.Bingo_Result.Result_Bingo_FeatureOff:
                    return "Result_Bingo_FeatureOff";
                case BingoBoardResultMessage.Bingo_Result.Result_Bingo_Ok:
                    return "Result_Bingo_Ok";
                case BingoBoardResultMessage.Bingo_Result.Result_Bingo_Completed:
                    return "Result_Bingo_Completed";
                case BingoBoardResultMessage.Bingo_Result.Result_Bingo_RewardProcess:
                    return "Result_Bingo_RewardProcess";
                case BingoBoardResultMessage.Bingo_Result.Result_Bingo_Loading:
                    return "Result_Bingo_Loading";
            }
            return "";
        }

        private static void PrintBingoBoardResultMessage(BingoBoardResultMessage msg, object tag) {
            Console.WriteLine("BingoBoardResultMessage:");
            Console.WriteLine("\tResult={0}", BingoResultToString((BingoBoardResultMessage.Bingo_Result)msg.Result));
            Console.WriteLine("\tBingoBoardNumbers=[{0}]", String.Join(",", msg.BingoBoardNumbers));
        }

        private static void PrintRandomRankInfoMessage(RandomRankInfoMessage msg, object tag) {
            Console.WriteLine("RandomRankInfoMessage:");
            foreach (RandomRankResultInfo info in msg.RandomRankResult) {
                Console.WriteLine("\tEventID={0}",info.EventID);
                Console.WriteLine("\tPeriodType={0}", info.PeriodType);
                Console.WriteLine(ListToString<RankResultInfo>(info.RandomRankResult, "RandomRankResult", 1));
            }
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
            Console.WriteLine("\tAttendanceInfo:");
            foreach (AttendanceDayInfo info in msg.AttendanceInfo) {
                Console.WriteLine("\t\tday={0} isCompleted={1} completedRewardsIndex={2}",info.day,info.isCompleted,info.completedRewardIndex);
            }
            Console.WriteLine("\tBonusRewardInfo:");
            foreach (AttendanceDayInfo info in msg.BonusRewardInfo)
            {
                Console.WriteLine("\t\tday={0} isCompleted={1} completedRewardsIndex={2}", info.day, info.isCompleted, info.completedRewardIndex);
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

        private static void PrintManufactureInfoMessage(ManufactureInfoMessage msg, object tag) {
            Console.WriteLine("ManufactureInfoMessage:");
            Console.WriteLine(DictToString<string, int>(msg.ExpDictionary, "ExpDictionary", 1));
            Console.WriteLine(DictToString<string, int>(msg.GradeDictionary, "GradeDictionary", 1));
            Console.WriteLine(ListToString<string>(msg.Recipes, "Recipes", 1));
        }

        private static void PrintGuildInfoMessage(GuildInfoMessage msg, object tag) {
            //TODO: db connect
            Console.WriteLine(msg.ToString());
        }
    }
}
