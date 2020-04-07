using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TrainingField
{
    public class TrainingFieldCampaignBehavior : CampaignBehaviorBase
    {
        private static readonly string ConfigFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");

        internal static TrainingFieldCampaignBehavior Current;

        private Config _config = new Config();
        private int _trainingCooldownHoursRemaining;
        private bool _isTraining;
        private int _trainingHoursRemaining;
        private int _totalUnitsWoundedInTraining;

        public bool CanTrain => _trainingCooldownHoursRemaining == 0;
        public float TrainingProgress => 1.0f - (float)_trainingHoursRemaining / _config.MaximumHoursToTrain;
        public int MaximumNumberOfHoursToTrain => _config.MaximumHoursToTrain;
        public int TotalUnitsWoundedInTraining => _totalUnitsWoundedInTraining;
        public bool HasTroopsToTrain => MobileParty.MainParty.MemberRoster.TotalRegulars > 0;

        public TrainingFieldCampaignBehavior()
        {
            Current = this;
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (!File.Exists(ConfigFilePath))
            {
                return;
            }
            try
            {
                _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigFilePath));
            }
            catch
            {
            }
        }

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourAdvanced);
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
                DisplayMessage($"You have already conducted training recently. Come back in {_trainingCooldownHoursRemaining} {(_trainingCooldownHoursRemaining > 1 ? "hours" : "hour")}.");
                return;
            }

            SwitchToActiveTrainingMenu();
            _isTraining = true;
            _totalUnitsWoundedInTraining = 0;
            _trainingHoursRemaining = _config.MaximumHoursToTrain;
            _trainingCooldownHoursRemaining = _config.TrainingCooldownHours;
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
            ReturnToTrainingFieldMainMenu();
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
            var flattenedMemberRoster = party.MemberRoster.ToFlattenedRoster();
            foreach (var member in flattenedMemberRoster)
            {
                if (member.Troop.IsHero || member.IsWounded || member.IsKilled)
                {
                    continue;
                }
                var experienceGainMultiplier = CalculateExperienceGainMultiplierForTroop(member.Troop, party);
                var experienceGained = (int)(_config.BaseExperiencePerHour * experienceGainMultiplier);
                party.MemberRoster.AddXpToTroop(experienceGained, member.Troop);
                var wasTroopWoundedInTraining = _config.ShouldWoundDuringTraining && MBRandom.RandomFloat <= _config.WoundProbability;
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
        }

        private float CalculateExperienceGainMultiplierForTroop(CharacterObject memberTroop, MobileParty party)
        {
            var bowSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Bow) / _config.MaxSkillValue;
            var crossbowSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Crossbow) / _config.MaxSkillValue;
            var polearmSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Polearm) / _config.MaxSkillValue;
            var ridingSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Riding) / _config.MaxSkillValue;
            var throwingSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Throwing) / _config.MaxSkillValue;
            var oneHandedSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.OneHanded) / _config.MaxSkillValue;
            var twoHandedSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.TwoHanded) / _config.MaxSkillValue;
            var leadershipSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Leadership) / _config.MaxSkillValue;
            var tacticsSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Tactics) / _config.MaxSkillValue;
            var athleticsSkillValue = (float)party.GetHighestSkillValueInParty(DefaultSkills.Athletics) / _config.MaxSkillValue;

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
            return experienceGainMultiplier + leadershipSkillValue + tacticsSkillValue + athleticsSkillValue;
        }

        private static void ReturnToTrainingFieldMainMenu()
        {
            GameMenu.SwitchToMenu("training_field_menu");
        }

        private static void SwitchToActiveTrainingMenu()
        {
            GameMenu.ActivateGameMenu("training_field_train_troops_wait");
            GameMenu.SwitchToMenu("training_field_train_troops_wait");
        }
    }
}
