using Devcat.Core.Net.Message;
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
            //this.Register<>(new Action<, object>(Print));
            //this.Register<>(new Action<, object>(Print));
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

        private static void PrintHousingMemberInfoMessage(HousingMemberInfoMessage msg, object tag) {
            //TODO: connect to database to collect avatar info
            Console.WriteLine("HousingMemberInfoMessage:");
            GameJoinMemberInfo m = msg.MemberInfo;
            Console.WriteLine("\tName={0}", m.Name);
            Console.WriteLine("\tLevel={0}", m.Level);
            Console.WriteLine("\tClass={0}", m.BaseClass);//not sure how to translate to string, find out manually
            Console.WriteLine("\tTitleID={0}", m.TitleID);//translate using heroes.db3 and translation xml
            Console.WriteLine("\tTitleCount={0}", m.TitleCount);

            Console.WriteLine("\tStats:");
            foreach (KeyValuePair<string, int> entry in m.Stats) {
                Console.WriteLine("\t\t{0}={1}",entry.Key,entry.Value);
            }
            Console.WriteLine("\tHeight={0}", m.CostumeInfo.Height);
            Console.WriteLine("\tBust={0}", m.CostumeInfo.Bust);
            
            Console.WriteLine("\tEquippedItems:");
            foreach (KeyValuePair<int,string> entry in m.EquippedItems)
            {
                Console.WriteLine("\t\t{0}={1}", entry.Key, entry.Value);
            }
            Console.WriteLine("\tPet: Name={0}, Type={1}",m.Pet.PetName,m.Pet.PetType);
            Console.WriteLine("\t...");//much more info if needed
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
    }
}
