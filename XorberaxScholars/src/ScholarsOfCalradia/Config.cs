using Newtonsoft.Json;

namespace ScholarsOfCalradia
{
    internal class Config
    {
        [JsonProperty("noviceScholarExperienceGain")]
        public int NoviceScholarExperienceGain { get; set; }

        [JsonProperty("noviceScholarCostPerAttendee")]
        public int NoviceScholarCostPerAttendee { get; set; }

        [JsonProperty("noviceLectureDurationInHours")]
        public int NoviceLectureDurationInHours { get; set; }

        [JsonProperty("intermediateScholarExperienceGain")]
        public int IntermediateScholarExperienceGain { get; set; }

        [JsonProperty("intermediateScholarCostPerAttendee")]
        public int IntermediateScholarCostPerAttendee { get; set; }

        [JsonProperty("intermediateLectureDurationInHours")]
        public int IntermediateLectureDurationInHours { get; set; }

        [JsonProperty("advancedScholarExperienceGain")]
        public int AdvancedScholarExperienceGain { get; set; }

        [JsonProperty("advancedScholarCostPerAttendee")]
        public int AdvancedScholarCostPerAttendee { get; set; }

        [JsonProperty("advancedLectureDurationInHours")]
        public int AdvancedLectureDurationInHours { get; set; }

        [JsonProperty("expertScholarExperienceGain")]
        public int ExpertScholarExperienceGain { get; set; }

        [JsonProperty("expertScholarCostPerAttendee")]
        public int ExpertScholarCostPerAttendee { get; set; }

        [JsonProperty("expertLectureDurationInHours")]
        public int ExpertLectureDurationInHours { get; set; }

        [JsonProperty("scholarAppearanceProbability")]
        public float ScholarAppearanceProbability { get; set; }
    }
}
