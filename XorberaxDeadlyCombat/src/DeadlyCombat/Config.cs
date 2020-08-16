using Newtonsoft.Json;

namespace DeadlyCombat
{
    internal class Config
    {
        [JsonProperty("percentageOfDamageRequiredToKillUnit")]
        public float PercentageOfDamageRequiredToKillUnit { get; set; }

        [JsonProperty("unitHealthPercentageToCauseBleedout")]
        public float UnitHealthPercentageToCauseBleedout { get; set; }

        [JsonProperty("unitSpeedReductionRateDuringBleedout")]
        public float UnitSpeedReductionRateDuringBleedout { get; set; }

        [JsonProperty("shouldAutoReloadConfig")]
        public bool ShouldAutoReloadConfig { get; set; }
    }
}
