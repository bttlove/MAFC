using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace pviBase.Dtos
{
    public class CreateContractRequestDto
    {
        [Required]
        [JsonProperty("accessKey")]
        public required string AccessKey { get; set; }

        [Required]
        [JsonProperty("product_Code")]
        public required string ProductCode { get; set; } = "MAFC_SKNVV";

        [Required]
        [JsonProperty("data")]
        public required List<InsuranceContractRequestDto> Data { get; set; } = new();

        // ✅ Không bind từ JSON — lấy từ multipart/form-data
        [JsonIgnore]
        public List<IFormFile>? AllAttachments { get; set; }
    }
}
