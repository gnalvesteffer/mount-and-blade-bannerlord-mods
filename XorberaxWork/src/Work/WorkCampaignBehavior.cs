using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.TwoDimension;

namespace Work
{
    public class WorkCampaignBehavior : CampaignBehaviorBase
    {
        private int _hoursRemainingUntilShiftFinished;
        private int _hoursRemainingUntilAbleToWork;

        private int TotalHoursWorked => SubModule.Config.HoursInShift - _hoursRemainingUntilShiftFinished;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                dataStore.SyncData("_hoursRemainingUntilAbleToWork", ref _hoursRemainingUntilAbleToWork);
            }
            catch
            {
            }
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            AddMenus(campaignGameStarter);
        }

        private void OnHourlyTick()
        {
            _hoursRemainingUntilAbleToWork = (int)Mathf.Max(_hoursRemainingUntilAbleToWork - 1, 0);
            var currentHoursRemainingUntilShiftFinished = _hoursRemainingUntilShiftFinished;
            _hoursRemainingUntilShiftFinished = (int)Mathf.Max(_hoursRemainingUntilShiftFinished - 1, 0);
            if (currentHoursRemainingUntilShiftFinished == 1) // work will end this hour
            {
                OnWorkEnded();
            }
        }

        private void AddMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption(
                "village",
                "village_work",
                "{=village_work}Work",
                menuCallbackArgs =>
                {
                    var canWork = _hoursRemainingUntilAbleToWork == 0;
                    menuCallbackArgs.IsEnabled = canWork;
                    menuCallbackArgs.Tooltip = canWork ? new TextObject() : new TextObject($"You will be able to work again in {_hoursRemainingUntilAbleToWork} {(_hoursRemainingUntilAbleToWork == 1 ? "hour" : "hours")}.");
                    menuCallbackArgs.optionLeaveType = GameMenuOption.LeaveType.Wait;
                    return true;
                },
                menuCallbackArgs => BeginWork(),
                false,
                3
            );
            campaignGameStarter.AddWaitGameMenu(
                "village_work_wait",
                "{=village_work_wait_description}You're helping the locals gather resources and work the land.",
                null,
                args =>
                {
                    args.MenuContext.GameMenu.SetTargetedWaitingTimeAndInitialProgress(SubModule.Config.HoursInShift, 0);
                    args.MenuContext.GameMenu.AllowWaitingAutomatically();
                    return true;
                },
                null,
                (args, dt) =>
                {
                    var progress = (float)TotalHoursWorked / SubModule.Config.HoursInShift;
                    args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(progress);
                },
                GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption
            );
            campaignGameStarter.AddGameMenuOption(
                "village_work_wait",
                "village_work_wait_stop_working",
                "{=village_work_wait_stop_working}Stop working.",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args => StopWorking()
            );
        }

        private void BeginWork()
        {
            _hoursRemainingUntilShiftFinished = SubModule.Config.HoursInShift;
            GameMenu.SwitchToMenu("village_work_wait");
        }

        private void StopWorking()
        {
            OnWorkEnded();
        }

        private void OnWorkEnded()
        {
            _hoursRemainingUntilAbleToWork = SubModule.Config.WorkCooldownInHours;
            GameMenu.SwitchToMenu("village");

            if (TotalHoursWorked == 0)
            {
                return;
            }

            var settlementFunds = Settlement.CurrentSettlement.GetComponent<SettlementComponent>().Gold;
            var maxSettlementFundsToEarn = settlementFunds * MBRandom.RandomFloatRanged(SubModule.Config.MaxPercentageOfSettlementFundsToEarnFromFullShift);
            var amountPaid = (int)Mathf.Min(maxSettlementFundsToEarn * TotalHoursWorked, SubModule.Config.PaymentLimit);
            if (amountPaid == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("The village can't afford to pay you for your work."));
                return;
            }
            var gift = Settlement.CurrentSettlement.ItemRoster.GetRandomElement();
            var giftQuantity = MBRandom.RandomFloat <= SubModule.Config.ProbabilityOfReceivingGift ? Mathf.Clamp(MBRandom.RandomInt(gift.Amount), 0, SubModule.Config.MaxGiftQuantity) : 0;
            GiveGoldAction.ApplyForSettlementToCharacter(Settlement.CurrentSettlement, Hero.MainHero, amountPaid, true);
            if (giftQuantity > 0)
            {
                GiveItemAction.ApplyForParties(Settlement.CurrentSettlement.Party, PartyBase.MainParty, gift, giftQuantity);
            }
            InformationManager.DisplayMessage(new InformationMessage($"You were paid {amountPaid}<img src=\"Icons\\Coin@2x\"> for {TotalHoursWorked:0} {(TotalHoursWorked == 1 ? "hour" : "hours")} of work{(giftQuantity > 0 ? $", and received a gift of {giftQuantity} {gift.EquipmentElement.Item.Name}" : String.Empty)}.", "event:/ui/notification/coins_positive"));
        }
    }
}
