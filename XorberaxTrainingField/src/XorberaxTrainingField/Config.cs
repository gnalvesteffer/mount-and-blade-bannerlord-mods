using Newtonsoft.Json;

namespace TrainingField
{
    public class Config
    {
        [JsonProperty("experiencePerHour")]
        public int ExperiencePerHour { get; set; }

        [JsonProperty("maximumHoursToTrain")]
        public int MaximumHoursToTrain { get; set; }

        [JsonProperty("trainingCooldownHours")]
        public int TrainingCooldownHours { get; set; }

        [JsonProperty("enableWoundingDuringTraining")]
        public bool ShouldWoundDuringTraining { get; set; }

        [JsonProperty("woundProbability")]
        public float WoundProbability { get; set; }
    }
}
