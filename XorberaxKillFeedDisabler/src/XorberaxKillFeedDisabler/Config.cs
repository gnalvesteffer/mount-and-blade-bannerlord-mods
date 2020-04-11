using Newtonsoft.Json;

namespace KillFeedDisabler
{
    public class Config
    {
        [JsonProperty("enableGeneralKillFeed")]
        public bool EnableGeneralKillFeed { get; set; }

        [JsonProperty("enablePersonalKillFeed")]
        public bool EnablePersonalKillFeed { get; set; }
    }
}
