using Newtonsoft.Json;

namespace TrainingField
{
    public class Config
    {
        [JsonProperty("experiencePerHour")]
        public int BaseExperiencePerHour { get; set; } = 10;

        [JsonProperty("experiencePerHourAtArenas")]
        public int BaseExperiencePerHourAtArenas { get; set; } = 50;

        [JsonProperty("perUnitTrainingCostAtArenas")]
        public int PerUnitTrainingCostAtArenas { get; set; }

        [JsonProperty("additionalFeeForSettlementFactor")]
        public float AdditionalFeeForSettlementFactor { get; set; }

        [JsonProperty("additionalExperiencePerHourPerSettlementMilitia")]
        public float AdditionalExperiencePerHourPerSettlementMilitia { get; set; }

        [JsonProperty("maximumHoursToTrain")]
        public int MaximumHoursToTrain { get; set; } = 24;

        [JsonProperty("trainingCooldownHours")]
        public int TrainingCooldownHours { get; set; } = 72;

        [JsonProperty("enableWoundingDuringTraining")]
        public bool ShouldWoundDuringTraining { get; set; } = true;

        [JsonProperty("woundProbabilityAtTrainingField")]
        public float WoundProbabilityAtTrainingField { get; set; } = 0.1f;

        [JsonProperty("woundProbabilityAtArena")]
        public float WoundProbabilityAtArena { get; set; } = 0.02f;

        [JsonProperty("maxSkillValue")]
        public int MaxSkillValue { get; set; } = 275;

        [JsonProperty("combatSkillsAffectExperienceGain")]
        public bool ShouldCombatSkillsAffectExperienceGain { get; set; } = true;
    }
}
