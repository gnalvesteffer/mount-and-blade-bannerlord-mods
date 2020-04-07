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

        private bool _shouldWoundDuringTraining = true;
        private float _woundProbability = 0.02f;
        private int _maximumNumberOfHoursToTrain = 24;
        private int _experiencePerHour = 10;
        private int _trainingCooldownHours = 72;
        private int _trainingCooldownHoursRemaining;
        private bool _isTraining;
        private int _trainingHoursRemaining;
        private int _totalUnitsWoundedInTraining;

        public bool CanTrain => _trainingCooldownHoursRemaining == 0;
        public float TrainingProgress => 1.0f - (float)_trainingHoursRemaining / _maximumNumberOfHoursToTrain;
        public int MaximumNumberOfHoursToTrain => _maximumNumberOfHoursToTrain;
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
                var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigFilePath));
                _experiencePerHour = config.ExperiencePerHour;
                _maximumNumberOfHoursToTrain = config.MaximumHoursToTrain;
                _trainingCooldownHours = config.TrainingCooldownHours;
                _shouldWoundDuringTraining = config.ShouldWoundDuringTraining;
                _woundProbability = config.WoundProbability;
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
            _trainingHoursRemaining = _maximumNumberOfHoursToTrain;
            _trainingCooldownHoursRemaining = _trainingCooldownHours;
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
                var experienceGained = member.Xp + _experiencePerHour;
                party.MemberRoster.AddXpToTroop(experienceGained, member.Troop);
                var wasTroopWoundedInTraining = _shouldWoundDuringTraining && MBRandom.RandomFloat <= _woundProbability;
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
