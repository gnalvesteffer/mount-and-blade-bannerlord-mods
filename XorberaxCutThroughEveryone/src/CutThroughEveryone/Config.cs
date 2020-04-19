using Newtonsoft.Json;

namespace CutThroughEveryone
{
    internal class Config
    {
        [JsonProperty("onlyCutThroughWhenUnitIsKilled")]
        public bool ShouldOnlyCutThroughKilledUnits { get; set; }

        [JsonProperty("damageRetainedPerCut")]
        public float DamageRetainedPerCut { get; set; }

        [JsonProperty("percentageOfInflictedDamageRequiredToCutThroughArmor")]
        public float PercentageOfInflictedDamageRequiredToCutThroughArmor { get; set; }

        [JsonProperty("doFriendlyUnitsBlockCutThroughs")]
        public bool DoFriendlyUnitsBlockCutThroughs { get; set; }

        [JsonProperty("onlyPlayerCanCutThrough")]
        public bool OnlyPlayerCanCutThrough { get; set; }
    }
}
