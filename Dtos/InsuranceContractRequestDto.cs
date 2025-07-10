using Newtonsoft.Json;
using System;

namespace pviBase.Dtos
{
    public class InsuranceContractRequestDto
    {
        [JsonProperty("loanNo")]
        public string? LoanNo { get; set; }

        [JsonProperty("loanType")]
        public string? LoanType { get; set; }

        [JsonProperty("loanDate")]
        public DateTime LoanDate { get; set; }

        [JsonProperty("custName")]
        public string? CustName { get; set; }

        [JsonProperty("custBirthday")]
        public DateTime CustBirthday { get; set; }

        [JsonProperty("custGender")]
        public string? CustGender { get; set; }

        [JsonProperty("custIdNo")]
        public string? CustIdNo { get; set; }

        [JsonProperty("custAddress")]
        public string? CustAddress { get; set; }

        [JsonProperty("custPhone")]
        public string? CustPhone { get; set; }

        [JsonProperty("custEmail")]
        public string? CustEmail { get; set; }

        [JsonProperty("loanAmount")]
        public decimal LoanAmount { get; set; }

        [JsonProperty("loanTerm")]
        public int LoanTerm { get; set; }

        [JsonProperty("insRate")]
        public double InsRate { get; set; }

        [JsonProperty("disbursementDate")]
        public DateTime DisbursementDate { get; set; }

        // ✅ File name dùng để map file trong danh sách allAttachments
        [JsonProperty("attachmentFileName")]
        public string? AttachmentFileName { get; set; }

        // Thêm để lưu dữ liệu file upload
        public byte[]? AttachmentData { get; set; }
        public string? AttachmentContentType { get; set; }
    }
}
