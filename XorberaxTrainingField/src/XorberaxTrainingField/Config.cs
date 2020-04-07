using Newtonsoft.Json;

namespace TrainingField
{
    public class Config
    {
        [JsonProperty("experiencePerHour")]
        public int BaseExperiencePerHour { get; set; } = 10;

        [JsonProperty("maximumHoursToTrain")]
        public int MaximumHoursToTrain { get; set; } = 24;

        [JsonProperty("trainingCooldownHours")]
        public int TrainingCooldownHours { get; set; } = 72;

        [JsonProperty("enableWoundingDuringTraining")]
        public bool ShouldWoundDuringTraining { get; set; } = true;

        [JsonProperty("woundProbability")]
        public float WoundProbability { get; set; } = 0.02f;

        [JsonProperty("maxSkillValue")]
        public int MaxSkillValue { get; set; } = 275;

        [JsonProperty("combatSkillsAffectExperienceGain")]
        public bool ShouldCombatSkillsAffectExperienceGain { get; set; } = true;
    }
}
