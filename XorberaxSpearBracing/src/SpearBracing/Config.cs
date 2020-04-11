using Newtonsoft.Json;

namespace SpearBracing
{
    public class Config
    {
        [JsonProperty("boneCollisionRadius")]
        public float BoneCollisionRadius { get; set; }

        [JsonProperty("isDebugMode")]
        public bool IsDebugMode { get; set; }
    }
}
