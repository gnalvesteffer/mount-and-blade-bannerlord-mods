using Newtonsoft.Json;

namespace Banks
{
    public class Config
    {
        [JsonProperty("availableLoanAmountPerRenown")]
        public int AvailableLoanAmountPerRenown { get; set; }

        [JsonProperty("interestRatePerSettlementProsperityFactor")]
        public float InterestRatePerSettlementProsperityFactor { get; set; }

        [JsonProperty("bankAccountOpeningCostBase")]
        public int BankAccountOpeningCostBase { get; set; }

        [JsonProperty("bankAccountOpeningCostSettlementProsperityDivisor")]
        public int BankAccountOpeningCostSettlementProsperityDivisor { get; set; }
    }
}
