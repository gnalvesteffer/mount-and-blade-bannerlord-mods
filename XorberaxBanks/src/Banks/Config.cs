using Newtonsoft.Json;

namespace Banks
{
    public class Config
    {
        [JsonProperty("availableLoanAmountPerRenown")]
        public int AvailableLoanAmountPerRenown { get; set; }

        [JsonProperty("availableLoanAmountDivisor")]
        public int availableLoanAmountDivisor { get; set; }

        [JsonProperty("daysToRepayLoan")]
        public int DaysToRepayLoan { get; set; }

        [JsonProperty("interestRatePerSettlementProsperityFactor")]
        public float InterestRatePerSettlementProsperityFactor { get; set; }

        [JsonProperty("bankAccountOpeningCostBase")]
        public int BankAccountOpeningCostBase { get; set; }

        [JsonProperty("bankAccountOpeningCostSettlementProsperityDivisor")]
        public int BankAccountOpeningCostSettlementProsperityDivisor { get; set; }
    }
}
