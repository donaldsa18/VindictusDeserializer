using Devcat.Core.Net.Message;
using ServiceCore.CharacterServiceOperations;
using ServiceCore.EndPointNetwork;
using ServiceCore.EndPointNetwork.Captcha;
using ServiceCore.EndPointNetwork.GuildService;
using ServiceCore.EndPointNetwork.Housing;
using ServiceCore.EndPointNetwork.Item;
using ServiceCore.EndPointNetwork.MicroPlay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            //this.Register<>(new Action<, object>(Print));
            //this.Register<>(new Action<, object>(Print));
            //this.Register<>(new Action<, object>(Print));
            //this.Register<>(new Action<, object>(Print));
            //this.Register<>(new Action<, object>(Print));
            //this.Register<>(new Action<, object>(Print));
            //this.Register<>(new Action<, object>(Print));
            //this.Register<>(new Action<, object>(Print));

        }
        private void Register<T>(Action<T,object> printer) {
            if (categoryDict.ContainsKey(typeof(T).GUID)) {
                mf.Register<T>(printer,categoryDict[typeof(T).GUID]);
            }
            else {
                Console.WriteLine("Couldn't find {0} in dictionary", typeof(T).FullName);
            }
        }
        private static void PrintSyncFeatureMatrixMessage(SyncFeatureMatrixMessage msg, object tag) {
            Console.WriteLine("SyncFeatureMatrixMessage:");
            foreach (KeyValuePair<String, String> entry in msg.FeatureDic) {
                Console.WriteLine("\t{0}: {1}",entry.Key,entry.Value);
            }
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

        private static void PrintUpdateHistoryBookMessage(UpdateHistoryBookMessage msg, object tag) {
            Console.WriteLine("UpdateHistoryBookMessage:");
            StringBuilder sb = new StringBuilder();
            sb.Append("\tType=");
            switch (msg.Type) {
                case UpdateHistoryBookMessage.UpdateType.Unknown:
                    sb.Append("Unknown");
                    break;
                case UpdateHistoryBookMessage.UpdateType.Remove:
                    sb.Append("Remove");
                    break;
                case UpdateHistoryBookMessage.UpdateType.Add:
                    sb.Append("Add");
                    break;
                case UpdateHistoryBookMessage.UpdateType.Full:
                    sb.Append("Full");
                    break;
            }
            Console.WriteLine(sb.ToString());
            sb = new StringBuilder();
            sb.Append("\tHistoryBooks=[");
            foreach (String book in msg.HistoryBooks) {
                sb.Append(book);
                sb.Append(",");
            }
            sb.Append("]");
            Console.WriteLine(sb.ToString());
        }

        private static void PrintRequestItemCombinationMessage(RequestItemCombinationMessage msg, object tag) {
            Console.WriteLine("RequestItemCombinationMessage:");
            Console.WriteLine("\tcombinedEquipItemClass={0}", msg.combinedEquipItemClass);
            StringBuilder sb = new StringBuilder();
            sb.Append("\t partsIDList=[");
            foreach (long part in msg.partsIDList) {
                sb.Append(part);
                sb.Append(",");
            }
            sb.Append("]");
            Console.WriteLine(sb.ToString());
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

        private static void PrintGameJoinMemberInfo(GameJoinMemberInfo m, int tabs) {
            String t = new string('\t', tabs);

            Console.WriteLine("{0}Name={1}", t, m.Name);
            Console.WriteLine("{0}Level={1}", t, m.Level);
            String c = BaseCharacterToString((BaseCharacter)m.BaseClass);

            Console.WriteLine("{0}Class={1}", t, c);
            Console.WriteLine("{0}TitleID={1}", t, m.TitleID);//translate using heroes.db3 and translation xml
            Console.WriteLine("{0}TitleCount={1}", t, m.TitleCount);

            Console.WriteLine("{0}Stats:", t);
            if (m.Stats != null) {
                foreach (KeyValuePair<string, int> entry in m.Stats)
                {
                    Console.WriteLine("{0}\t{0}={1}", t, entry.Key, entry.Value);
                }
            }
            if (m.CostumeInfo != null) {
                Console.WriteLine("{0}Height={1}", t, m.CostumeInfo.Height);
                Console.WriteLine("{0}Bust={1}", t, m.CostumeInfo.Bust);
            }

            Console.WriteLine("{0}EquippedItems:");
            if (m.EquippedItems != null) {
                foreach (KeyValuePair<int, string> entry in m.EquippedItems)
                {
                    Console.WriteLine("{0}{1}\t={1}", t, entry.Key, entry.Value);
                }
            }

            if (m.Pet != null) {
                Console.WriteLine("{0}Pet: Name={1}, Type={2}", t, m.Pet.PetName, m.Pet.PetType);
            }
            
            Console.WriteLine("{0}...", t);//much more info if needed
        }

        private static void PrintHousingMemberInfoMessage(HousingMemberInfoMessage msg, object tag) {
            //TODO: connect to database to collect avatar info
            Console.WriteLine("HousingMemberInfoMessage:");
            PrintGameJoinMemberInfo(msg.MemberInfo,1);
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
            String housingType = "";
            switch (msg.EnterType) {
                case EnterHousingType.Default:
                    housingType = "Default";
                    break;
                case EnterHousingType.OpenPublic:
                    housingType = "OpenPublic";
                    break;
                case EnterHousingType.OpenPrivate:
                    housingType = "OpenPrivate";
                    break;
                case EnterHousingType.EnterAny:
                    housingType = "EnterAny";
                    break;
                case EnterHousingType.EnterSpecified:
                    housingType = "EnterSpecified";
                    break;
            }
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
            Console.WriteLine("\tRewardItems:");
            foreach (KeyValuePair<string, int> entry in msg.RewardItems) {
                Console.WriteLine("\t\t{0}={1}",entry.Key,entry.Value);
            }
            Console.WriteLine("\tRewardMailItems:");
            foreach (KeyValuePair<string, int> entry in msg.RewardMailItems)
            {
                Console.WriteLine("\t\t{0}={1}", entry.Key, entry.Value);
            }
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

        private static void PrintHousingRoomListMessage(HousingRoomListMessage msg, object tag) {
            Console.WriteLine("HousingRoomListMessage:");
            if (msg.HousingRoomList == null) {
                return;
            }
            foreach (HousingRoomInfo info in msg.HousingRoomList)
            {
                if (info != null)
                {
                    Console.WriteLine("\t{0}", info.ToString());
                }
            }
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
            String result = "";
            switch ((AddFriendShipResultMessage.AddFriendShipResult)msg.Result) {
                case AddFriendShipResultMessage.AddFriendShipResult.Result_Already_Added:
                    result = "Result_Already_Added";
                    break;
                case AddFriendShipResultMessage.AddFriendShipResult.Result_Character_NotFounded:
                    result = "Result_Character_NotFounded";
                    break;
                case AddFriendShipResultMessage.AddFriendShipResult.Result_Exception:
                    result = "Result_Exception";
                    break;
                case AddFriendShipResultMessage.AddFriendShipResult.Result_FriendCount_Max:
                    result = "Result_FriendCount_Max";
                    break;
                case AddFriendShipResultMessage.AddFriendShipResult.Result_Ok:
                    result = "Result_Ok";
                    break;
                case AddFriendShipResultMessage.AddFriendShipResult.Result_SameID:
                    result = "Result_SameID";
                    break;
            }
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
            Console.WriteLine("AllUserGoalEventMessage: AllUserGoalInfo:");
            foreach (KeyValuePair<int, int> goal in msg.AllUserGoalInfo) {
                Console.WriteLine("\t{0}={1}",goal.Key,goal.Value);
            }
        }

        private static void PrintLeaveHousingMessage(LeaveHousingMessage msg, object tag) {
            Console.WriteLine("LeaveHousingMessage []");
        }

        private static void PrintDecomposeItemResultMessage(DecomposeItemResultMessage msg, object tag) {
            
            String item = "";
            switch (msg.ResultEXP) {
                case DecomposeItemResultEXP.fail:
                    item = "fail";
                    break;
                case DecomposeItemResultEXP.increase:
                    item = "increase";
                    break;
                case DecomposeItemResultEXP.not_increase:
                    item = "not_increase";
                    break;
                case DecomposeItemResultEXP.not_increase_already_max:
                    item = "not_increase_already_max";
                    break;
                case DecomposeItemResultEXP.part_extract:
                    item = "part_extract";
                    break;
            }
            Console.WriteLine("DecomposeItemResultMessage: ResultEXP={0}",item);
            Console.WriteLine("\tGiveItem:");
            foreach (string itemclass in msg.GiveItemClassList) {
                Console.WriteLine("\t\t{0}",itemclass);
            }
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
            Console.WriteLine("\tOwnerList=[{0}]",String.Join(",",msg.OwnerList));
            StringBuilder sb = new StringBuilder();
            foreach (BlessStoneType t in msg.TypeList) {
                switch (t) {
                    case BlessStoneType.NONE:
                        sb.Append("NONE");
                        break;
                    case BlessStoneType.ALL:
                        sb.Append("ALL");
                        break;
                    case BlessStoneType.EXP:
                        sb.Append("EXP");
                        break;
                    case BlessStoneType.LUCK:
                        sb.Append("LUCK");
                        break;
                    case BlessStoneType.AP:
                        sb.Append("AP");
                        break;
                    case BlessStoneType.StoneTypeCount:
                        sb.Append("StoneTypeCount");
                        break;
                }
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
            Console.WriteLine("\tHousingProps:");
            foreach (HousingPropInfo info in msg.HousingProps) {
                Console.WriteLine("\t\t{0},info.ToString()");
            }
            Console.WriteLine("\tHostInfo:");
            PrintGameJoinMemberInfo(msg.HostInfo, 2);
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
            String s = "";
            switch (v) {
                case VocationEnum.Invalid:
                    s = "Invalid";
                    break;
                case VocationEnum.None:
                    s = "None";
                    break;
                case VocationEnum.Paladin:
                    s = "Paladin";
                    break;
                case VocationEnum.DarkKnight:
                    s = "DarkKnight";
                    break;
            }
            return s;
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
            if (c.Costume != null)
            {
                Console.WriteLine("\tHeight={1}", c.Costume.Height);
                Console.WriteLine("\tBust={1}", c.Costume.Bust);
            }

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
    }
}
