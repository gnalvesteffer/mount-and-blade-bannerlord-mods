using Newtonsoft.Json;

namespace Banks
{
    public class Config
    {
        [JsonProperty("availableLoanAmountPerRenown")]
        public int AvailableLoanAmountPerRenown { get; set; }

        [JsonProperty("availableLoanAmountDivisor")]
        public int AvailableLoanAmountDivisor { get; set; }

        [JsonProperty("renownCostPerLoanAmountDivisor")]
        public int RenownCostPerLoanAmountDivisor { get; set; }

        [JsonProperty("daysToRepayLoan")]
        public int DaysToRepayLoan { get; set; }

        [JsonProperty("renownLossForUnpaidLoan")]
        public int RenownLossForUnpaidLoan { get; set; }

        [JsonProperty("crimeRatingIncreaseForUnpaidLoan")]
        public int CrimeRatingIncreaseForUnpaidLoan { get; set; }

        [JsonProperty("interestRatePerSettlementProsperityFactor")]
        public float InterestRatePerSettlementProsperityFactor { get; set; }

        [JsonProperty("bankAccountOpeningCostBase")]
        public int BankAccountOpeningCostBase { get; set; }

        [JsonProperty("bankAccountOpeningCostSettlementProsperityDivisor")]
        public int BankAccountOpeningCostSettlementProsperityDivisor { get; set; }
    }
}
