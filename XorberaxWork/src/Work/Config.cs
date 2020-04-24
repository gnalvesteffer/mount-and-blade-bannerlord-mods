using Newtonsoft.Json;

namespace Work
{
    public class Config
    {
        [JsonProperty("hoursInShift")]
        public int HoursInShift { get; set; }

        [JsonProperty("workCooldownInHours")]
        public int WorkCooldownInHours { get; set; }

        [JsonProperty("maxPercentageOfSettlementFundsToEarnFromFullShift")]
        public float MaxPercentageOfSettlementFundsToEarnFromFullShift { get; set; }

        [JsonProperty("relationChangeWithVillageNotablesFromFullShift")]
        public int RelationChangeWithVillageNotablesFromFullShift { get; set; }

        [JsonProperty("athleticsExperienceGainedFromFullShift")]
        public int AthleticsExperienceGainedFromFullShift { get; set; }

        [JsonProperty("bonusPercentageOfPaymentGainedPerWorker")]
        public float BonusPercentageOfPaymentGainedPerWorker { get; set; }

        [JsonProperty("paymentLimit")]
        public int PaymentLimit { get; set; }

        [JsonProperty("probabilityOfReceivingGift")]
        public float ProbabilityOfReceivingGift { get; set; }

        [JsonProperty("maxGiftQuantity")]
        public int MaxGiftQuantity { get; set; }
    }
}
