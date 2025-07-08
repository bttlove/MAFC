using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace pviBase.Dtos
{
    public class InsuranceContractRequestDto
    {
        [Required]
        [JsonProperty("loanNo")]
        public required string LoanNo { get; set; } // Đã sửa

        [Required]
        [JsonProperty("loanType")]
        public required string LoanType { get; set; } // Đã sửa

        [Required]
        [JsonProperty("loanDate")]
        public required string LoanDate { get; set; } // Đã sửa

        [Required]
        [JsonProperty("custName")]
        public required string CustName { get; set; } // Đã sửa

        [Required]
        [JsonProperty("custBirthday")]
        public required string CustBirthday { get; set; } // Đã sửa

        [Required]
        [JsonProperty("custGender")]
        public required string CustGender { get; set; } // Đã sửa

        [Required]
        [JsonProperty("custIdNo")]
        public required string CustIdNo { get; set; } // Đã sửa

        [JsonProperty("custAddress")]
        public string? CustAddress { get; set; } // Giữ nguyên nullable

        [Required]
        [JsonProperty("custPhone")]
        public required string CustPhone { get; set; } // Đã sửa

        [JsonProperty("custEmail")]
        public string? CustEmail { get; set; } // Giữ nguyên nullable

        [Required]
        [JsonProperty("loanAmount")]
        public required long LoanAmount { get; set; } // Đã sửa

        [Required]
        [JsonProperty("loanTerm")]
        public required int LoanTerm { get; set; } // Đã sửa

        [Required]
        [JsonProperty("insRate")]
        public required double InsRate { get; set; } // Đã sửa

        [Required]
        [JsonProperty("disbursementDate")]
        public required string DisbursementDate { get; set; } // Đã sửa
    }
}