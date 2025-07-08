// Dtos/GetContractByLoanNoRequestDto.cs
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace pviBase.Dtos
{
    public class GetContractByLoanNoRequestDto
    {
        [Required]
        [JsonProperty("accessKey")] 
        public required string AccessKey { get; set; }

        [Required]
        [JsonProperty("product_Code")]
        public required string ProductCode { get; set; } = "MAFC_SKNVV";

        [Required]
        [JsonProperty("loanNo")]
        public required string LoanNo { get; set; }
    }
}