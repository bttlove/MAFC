using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace pviBase.Dtos
{
    public class CreateContractRequestDto
    {
        [Required]
        [JsonProperty("accessKey")]
        public required string AccessKey { get; set; } // Đã sửa

        [Required]
        [JsonProperty("product_Code")]
        public required string ProductCode { get; set; } = "MAFC_SKNVV"; // Đã sửa

        [Required]
        [JsonProperty("data")]
        public required List<InsuranceContractRequestDto> Data { get; set; } // Đã sửa
    }
}