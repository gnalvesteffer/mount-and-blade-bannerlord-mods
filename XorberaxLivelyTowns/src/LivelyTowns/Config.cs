using Newtonsoft.Json;

namespace LivelyTowns
{
    public class Config
    {
        [JsonProperty("guardMinSpawnDistance")]
        public float GuardMinSpawnDistance { get; set; }

        [JsonProperty("guardMaxSpawnDistance")]
        public float GuardMaxSpawnDistance { get; set; }

        [JsonProperty("guardAlertDistance")]
        public float GuardAlertDistance { get; set; }
    }
}
