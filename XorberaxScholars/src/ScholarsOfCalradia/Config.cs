using Newtonsoft.Json;

namespace ScholarsOfCalradia
{
    internal class Config
    {
        [JsonProperty("noviceScholarExperienceGain")]
        public int NoviceScholarExperienceGain { get; set; }

        [JsonProperty("noviceScholarCostPerAttendee")]
        public int NoviceScholarCostPerAttendee { get; set; }

        [JsonProperty("intermediateScholarExperienceGain")]
        public int IntermediateScholarExperienceGain { get; set; }

        [JsonProperty("intermediateScholarCostPerAttendee")]
        public int IntermediateScholarCostPerAttendee { get; set; }

        [JsonProperty("advancedScholarExperienceGain")]
        public int AdvancedScholarExperienceGain { get; set; }

        [JsonProperty("advancedScholarCostPerAttendee")]
        public int AdvancedScholarCostPerAttendee { get; set; }

        [JsonProperty("expertScholarExperienceGain")]
        public int ExpertScholarExperienceGain { get; set; }

        [JsonProperty("expertScholarCostPerAttendee")]
        public int ExpertScholarCostPerAttendee { get; set; }

        [JsonProperty("scholarAppearanceProbability")]
        public float ScholarAppearanceProbability { get; set; }
    }
}
