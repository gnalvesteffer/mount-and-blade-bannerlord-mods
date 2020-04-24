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
        private bool _isWorking;
        private int _hoursRemainingUntilShiftFinished;
        private int _hoursRemainingUntilAbleToWork;
        private Settlement _lastSettlementWorkedAt;

        private int TotalHoursWorked => SubModule.Config.HoursInShift - _hoursRemainingUntilShiftFinished;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageBeingRaided);
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
            _hoursRemainingUntilShiftFinished = (int)Mathf.Max(_hoursRemainingUntilShiftFinished - 1, 0);
            if (_isWorking && TotalHoursWorked == SubModule.Config.HoursInShift || _isWorking && Settlement.CurrentSettlement == null)
            {
                OnWorkEnded();
            }
        }

        private void OnVillageBeingRaided(Village villageBeingRaided)
        {
            if (_isWorking && (villageBeingRaided == Settlement.CurrentSettlement?.Village || Settlement.CurrentSettlement != _lastSettlementWorkedAt))
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
                "{XORBERAX_WORK_WAIT_DESCRIPTION}",
                null,
                args =>
                {
                    var totalWorkers = GetTotalWorkersInParty(PartyBase.MainParty);
                    MBTextManager.SetTextVariable("XORBERAX_WORK_WAIT_DESCRIPTION", new TextObject($"{(totalWorkers == 0 ? "You" : $"You and {totalWorkers} of your workers")} are helping the locals around their village by gathering resources and working the land.\n \nYou've worked for {{XORBERAX_WORK_TOTAL_HOURS_WORKED_TEXT}}."));
                    args.MenuContext.GameMenu.SetTargetedWaitingTimeAndInitialProgress(SubModule.Config.HoursInShift, 0);
                    args.MenuContext.GameMenu.AllowWaitingAutomatically();
                    return true;
                },
                null,
                (args, dt) =>
                {
                    MBTextManager.SetTextVariable("XORBERAX_WORK_TOTAL_HOURS_WORKED_TEXT", $"{TotalHoursWorked} {(TotalHoursWorked == 1 ? "hour" : "hours")}");
                    var progress = GetNormalizedHoursWorkedInShift(TotalHoursWorked);
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
            _isWorking = true;
            _lastSettlementWorkedAt = Settlement.CurrentSettlement;
            _hoursRemainingUntilShiftFinished = SubModule.Config.HoursInShift;
            GameMenu.SwitchToMenu("village_work_wait");
        }

        private void StopWorking()
        {
            OnWorkEnded();
        }

        private void OnWorkEnded()
        {
            _isWorking = false;
            _hoursRemainingUntilAbleToWork = SubModule.Config.WorkCooldownInHours;
            IncreaseRelationsWithSettlement(_lastSettlementWorkedAt, TotalHoursWorked);
            IncreaseSkills(TotalHoursWorked);
            GameMenu.SwitchToMenu("village");

            if (TotalHoursWorked == 0)
            {
                return;
            }

            var settlementFunds = _lastSettlementWorkedAt.GetComponent<SettlementComponent>().Gold;
            var baseSettlementFundsToEarn = (int)(settlementFunds * MBRandom.RandomFloatRanged(SubModule.Config.MaxPercentageOfSettlementFundsToEarnFromFullShift));
            var (gift, giftQuantity) = GetSettlementGift();
            if (baseSettlementFundsToEarn == 0)
            {
                if (giftQuantity > 0)
                {
                    PartyBase.MainParty.ItemRoster.AddToCounts(gift, giftQuantity);
                    InformationManager.DisplayMessage(new InformationMessage($"The locals gave you {giftQuantity} {gift.EquipmentElement.Item.Name} as payment for your work."));
                    return;
                }
                InformationManager.DisplayMessage(new InformationMessage("The locals can't afford to pay you for your work."));
                return;
            }

            var totalWorkersInParty = GetTotalWorkersInParty(PartyBase.MainParty);
            var bonusPaymentForWorkers = (int)Mathf.Clamp(baseSettlementFundsToEarn * (totalWorkersInParty * SubModule.Config.BonusPercentageOfPaymentGainedPerWorker), 0, settlementFunds);
            var amountPaid = (int)Mathf.Min((baseSettlementFundsToEarn + bonusPaymentForWorkers) * TotalHoursWorked, Mathf.Min(settlementFunds, SubModule.Config.PaymentLimit));
            if (amountPaid == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("The locals refuse to pay you for your work."));
                return;
            }

            GiveGoldAction.ApplyForSettlementToCharacter(_lastSettlementWorkedAt, Hero.MainHero, amountPaid, true);
            if (giftQuantity > 0)
            {
                GiveItemAction.ApplyForParties(_lastSettlementWorkedAt.Party, PartyBase.MainParty, gift, giftQuantity);
            }
            InformationManager.DisplayMessage(new InformationMessage($"You were paid {amountPaid}<img src=\"Icons\\Coin@2x\"> for {TotalHoursWorked:0} {(TotalHoursWorked == 1 ? "hour" : "hours")} of work{(giftQuantity > 0 ? $", and received a gift of {giftQuantity} {gift.EquipmentElement.Item.Name}" : String.Empty)}.", "event:/ui/notification/coins_positive"));
        }

        private (ItemRosterElement Item, int quantity) GetSettlementGift()
        {
            var gift = _lastSettlementWorkedAt.ItemRoster.GetRandomElement();
            var quantity = MBRandom.RandomFloat <= SubModule.Config.ProbabilityOfReceivingGift ? Mathf.Clamp(MBRandom.RandomInt(gift.Amount), 0, SubModule.Config.MaxGiftQuantity) : 0;
            return (gift, quantity);
        }

        private void IncreaseRelationsWithSettlement(Settlement settlement, int hoursWorked)
        {
            if (settlement == null)
            {
                return;
            }

            var relationChange = (int)(SubModule.Config.RelationChangeWithVillageNotablesFromFullShift * GetNormalizedHoursWorkedInShift(hoursWorked));
            if (relationChange == 0)
            {
                return;
            }

            foreach (var notable in settlement.Notables)
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, notable, relationChange);
            }
        }

        private void IncreaseSkills(float hoursWorked)
        {
            Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, SubModule.Config.AthleticsExperienceGainedFromFullShift * GetNormalizedHoursWorkedInShift(hoursWorked));
        }

        private int GetTotalWorkersInParty(PartyBase party)
        {
            return party?.MemberRoster?.Sum(member => member.Character.Occupation == Occupation.Villager && member.Character.HeroObject != Hero.MainHero ? member.Number : 0) ?? 0;
        }

        private float GetNormalizedHoursWorkedInShift(float hoursWorked)
        {
            return hoursWorked / SubModule.Config.HoursInShift;
        }
    }
}
