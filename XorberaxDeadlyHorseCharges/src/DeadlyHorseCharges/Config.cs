using Newtonsoft.Json;

namespace DeadlyHorseCharges
{
    public class Config
    {
        [JsonProperty("chargeDamageMultiplier")]
        public float ChargeDamageMultiplier { get; set; }
    }
}
