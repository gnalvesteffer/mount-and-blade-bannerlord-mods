using Newtonsoft.Json;

namespace VoiceOvers
{
    internal class Config
    {
        [JsonProperty("isDevMode")]
        public bool IsDevMode { get; set; }
    }
}
