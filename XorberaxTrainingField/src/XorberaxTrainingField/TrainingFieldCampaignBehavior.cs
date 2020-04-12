using System;
using StoryMode;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TrainingField
{
    public class TrainingFieldCampaignBehavior : CampaignBehaviorBase
    {
        internal static TrainingFieldCampaignBehavior Current;

        private int _trainingCooldownHoursRemaining;
        private bool _isTraining;
        private int _trainingHoursRemaining;
        private int _totalUnitsWoundedInTraining;

        public bool CanTrain => _trainingCooldownHoursRemaining == 0;
        public float TrainingProgress => 1.0f - (float)_trainingHoursRemaining / SubModule.Config.MaximumHoursToTrain;
        public int MaximumNumberOfHoursToTrain => SubModule.Config.MaximumHoursToTrain;
        public int TotalUnitsWoundedInTraining => _totalUnitsWoundedInTraining;
        public bool HasTroopsToTrain => MobileParty.MainParty.MemberRoster.TotalRegulars > 0;

        public TrainingFieldCampaignBehavior()
        {
            Current = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourAdvanced);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            AddArenaTrainingMenus(campaignGameStarter);
        }

        private void AddArenaTrainingMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption(
                "town_arena",
                "town_arena_train_troops",
                "{=town_arena_train_troops}Train your troops ({COST}{GOLD_ICON})",
                menuCallbackArgs =>
                {
                    MBTextManager.SetTextVariable("COST", CalculateCostToTrainTroops());
                    menuCallbackArgs.optionLeaveType = GameMenuOption.LeaveType.Wait;
                    return HasTroopsToTrain && !Settlement.CurrentSettlement.HasTournament;
                },
                menuCallbackArgs => BeginTraining(),
                false,
                2
            );
            campaignGameStarter.AddWaitGameMenu(
                "town_arena_train_troops_wait",
                "{=town_arena_train_troops_wait}Your troops train tirelessly throughout the day, increasing their combat effectiveness.",
                null,
                args =>
                {
                    args.MenuContext.GameMenu.SetTargetedWaitingTimeAndInitialProgress(MaximumNumberOfHoursToTrain, 0);
                    args.MenuContext.GameMenu.AllowWaitingAutomatically();
                    return true;
                },
                null,
                (args, dt) => { args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(TrainingProgress); },
                GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption
            );
            campaignGameStarter.AddGameMenuOption(
                "town_arena_train_troops_wait",
                "town_arena_train_troops_wait_end_training",
                "{=town_arena_train_troops_wait_end_training}End training early.",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args => CancelTraining()
            );
        }

        private static int CalculateCostToTrainTroops()
        {
            var settlement = Settlement.CurrentSettlement;
            if (settlement.IsTrainingField())
            {
                return 0;
            }
            var totalUnits = MobileParty.MainParty.MemberRoster.TotalRegulars;
            var basePrice = totalUnits * SubModule.Config.PerUnitTrainingCostAtArenas;
            var additionalFee = SubModule.Config.AdditionalFeeForSettlementFactor * (settlement.Prosperity + settlement.Militia);
            return (int)(basePrice + additionalFee);
        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                dataStore.SyncData(nameof(_trainingCooldownHoursRemaining), ref _trainingCooldownHoursRemaining);
            }
            catch
            {
            }
        }

        internal void BeginTraining()
        {
            if (!CanTrain)
            {
                DisplayMessage($"You have already conducted training recently. Your troops need {_trainingCooldownHoursRemaining} {(_trainingCooldownHoursRemaining > 1 ? "hours" : "hour")} to recover.");
                return;
            }

            var trainingCost = CalculateCostToTrainTroops();
            if (MobileParty.MainParty.LeaderHero.Gold < trainingCost)
            {
                DisplayMessage("You cannot afford to train your troops here.");
                return;
            }
            ApplyTrainingCosts(trainingCost);
            SwitchToActiveTrainingMenu();
            _isTraining = true;
            _totalUnitsWoundedInTraining = 0;
            _trainingHoursRemaining = SubModule.Config.MaximumHoursToTrain;
            _trainingCooldownHoursRemaining = SubModule.Config.TrainingCooldownHours;
        }

        private void ApplyTrainingCosts(int cost)
        {
            GiveGoldAction.ApplyForCharacterToSettlement(
                PartyBase.MainParty.LeaderHero,
                Settlement.CurrentSettlement,
                cost
            );
        }

        private void OnHourAdvanced()
        {
            if (!_isTraining)
            {
                _trainingCooldownHoursRemaining = Math.Max(_trainingCooldownHoursRemaining - 1, 0);
                return;
            }

            DistributeTrainingExperienceToParty();

            _trainingHoursRemaining = Math.Max(_trainingHoursRemaining - 1, 0);
            if (_trainingHoursRemaining == 0)
            {
                OnTrainingFinished();
            }
        }

        private void OnTrainingFinished()
        {
            OnTrainingEnded();
            DisplayMessage("You have finished training for today.");
        }

        internal void CancelTraining()
        {
            OnTrainingEnded();
            DisplayMessage("You end the training early.");
        }

        private void OnTrainingEnded()
        {
            _isTraining = false;
            ReturnToPreviousMenu();
        }

        private static void DisplayMessage(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage(message));
        }

        private static void DisplayWarningMessage(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage(message, Colors.Red));
        }

        private void DistributeTrainingExperienceToParty()
        {
            var party = MobileParty.MainParty;
            var totalUnitsWoundedInPartyThisHour = 0;
            var totalExperienceGainedThisHour = 0;
            var totalTroopsTrainedThisHour = 0;
            var baseHourlyExperience = GetBaseHourlyExperience();
            var flattenedMemberRoster = party.MemberRoster.ToFlattenedRoster();
            foreach (var member in flattenedMemberRoster)
            {
                if (member.Troop.IsHero || member.IsWounded || member.IsKilled)
                {
                    continue;
                }
                ++totalTroopsTrainedThisHour;
                var experienceGainMultiplier = SubModule.Config.ShouldCombatSkillsAffectExperienceGain ? CalculateExperienceGainMultiplierForTroop(member.Troop, party) : 1.0f;
                var experienceGained = (int)(baseHourlyExperience * experienceGainMultiplier);
                totalExperienceGainedThisHour += experienceGained;
                party.MemberRoster.AddXpToTroop(experienceGained, member.Troop);
                var wasTroopWoundedInTraining = SubModule.Config.ShouldWoundDuringTraining && MBRandom.RandomFloat <= GetWoundProbability();
                if (wasTroopWoundedInTraining)
                {
                    party.MemberRoster.WoundTroop(member.Troop);
                    ++totalUnitsWoundedInPartyThisHour;
                }
                party.MemberRoster.AddToCounts(member.Troop, 0, false, wasTroopWoundedInTraining ? 1 : 0, experienceGained);
            }
            if (totalUnitsWoundedInPartyThisHour > 0)
            {
                DisplayWarningMessage($"{totalUnitsWoundedInPartyThisHour} {(totalUnitsWoundedInPartyThisHour > 1 ? "units were" : "unit was")} wounded during training.");
            }
            if (totalExperienceGainedThisHour > 0)
            {
                DisplayMessage($"{totalTroopsTrainedThisHour} {(totalTroopsTrainedThisHour == 1 ? "troop" : "troops")} gained {totalExperienceGainedThisHour} experience.");
            }
        }

        private static float GetWoundProbability()
        {
            if (Settlement.CurrentSettlement.IsTrainingField())
            {
                return SubModule.Config.WoundProbabilityAtTrainingField;
            }
            return SubModule.Config.WoundProbabilityAtArena;
        }

        private static int GetBaseHourlyExperience()
        {
            var settlement = Settlement.CurrentSettlement;
            if (settlement.IsTrainingField())
            {
                return SubModule.Config.BaseExperiencePerHour;
            }
            var settlementMilitiaExperienceBoost = settlement.Militia * SubModule.Config.AdditionalExperiencePerHourPerSettlementMilitia;
            return (int)(SubModule.Config.BaseExperiencePerHourAtArenas + settlementMilitiaExperienceBoost);
        }

        private float CalculateExperienceGainMultiplierForTroop(CharacterObject memberTroop, MobileParty party)
        {
            var bowSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Bow) / SubModule.Config.MaxSkillValue;
            var crossbowSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Crossbow) / SubModule.Config.MaxSkillValue;
            var polearmSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Polearm) / SubModule.Config.MaxSkillValue;
            var ridingSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Riding) / SubModule.Config.MaxSkillValue;
            var throwingSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Throwing) / SubModule.Config.MaxSkillValue;
            var oneHandedSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.OneHanded) / SubModule.Config.MaxSkillValue;
            var twoHandedSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.TwoHanded) / SubModule.Config.MaxSkillValue;
            var leadershipSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Leadership) / SubModule.Config.MaxSkillValue;
            var tacticsSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Tactics) / SubModule.Config.MaxSkillValue;
            var athleticsSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Athletics) / SubModule.Config.MaxSkillValue;

            var experienceGainMultiplier = 1.0f;
            if (memberTroop.IsArcher)
            {
                experienceGainMultiplier += bowSkillValue + crossbowSkillValue;
            }
            if (memberTroop.IsInfantry)
            {
                experienceGainMultiplier += polearmSkillValue + oneHandedSkillValue + twoHandedSkillValue;
            }
            if (memberTroop.IsMounted)
            {
                experienceGainMultiplier += ridingSkillValue;
            }
            if (memberTroop.HasThrowingWeapon())
            {
                experienceGainMultiplier += throwingSkillValue;
            }
            return experienceGainMultiplier + (leadershipSkillValue + tacticsSkillValue + athleticsSkillValue) * MBRandom.RandomFloat;
        }

        private static void ReturnToPreviousMenu()
        {
            GameMenu.SwitchToMenu(GetPreviousMenuId());
        }

        private static string GetPreviousMenuId()
        {
            return Settlement.CurrentSettlement.IsTrainingField()
                ? "training_field_menu"
                : "town_arena";
        }

        private static void SwitchToActiveTrainingMenu()
        {
            var trainingWaitMenuId = GetTrainingWaitMenuId();
            GameMenu.ActivateGameMenu(trainingWaitMenuId);
            GameMenu.SwitchToMenu(trainingWaitMenuId);
        }

        private static string GetTrainingWaitMenuId()
        {
            return Settlement.CurrentSettlement.IsTrainingField()
                ? "training_field_train_troops_wait"
                : "town_arena_train_troops_wait";
        }
    }
}
