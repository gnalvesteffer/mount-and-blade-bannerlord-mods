using Newtonsoft.Json;

namespace CutThroughEveryone
{
    public class Config
    {
        [JsonProperty("onlyCutThroughWhenUnitIsKilled")]
        public bool ShouldOnlyCutThroughWhenUnitIsKilled { get; set; }

        [JsonProperty("damageRetainedPerCut")]
        public float DamageRetainedPerCut { get; set; }
    }
}
