// Dtos/GetContractByLoanNoResponseDataDto.cs
// cho phần data trong respone
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace pviBase.Dtos
{
    public class GetContractByLoanNoResponseDataDto
    {
        [JsonProperty("importBatchId")]
        public Guid ImportBatchId { get; set; } // Sử dụng Guid cho ID

        [JsonProperty("product_Code")]
        public string ProductCode { get; set; }

        [JsonProperty("contract_PackageCode")]
        public string ContractPackageCode { get; set; }

        [JsonProperty("contract_StartDate")]
        public DateTime ContractStartDate { get; set; }

        [JsonProperty("contract_EndDate")]
        public DateTime ContractEndDate { get; set; }

        [JsonProperty("contract_Term")]
        public int ContractTerm { get; set; }

        [JsonProperty("loan_Type")]
        public string LoanType { get; set; }

        [JsonProperty("loan_No")]
        public string LoanNo { get; set; }

        [JsonProperty("insured_IdNo")]
        public string InsuredIdNo { get; set; }

        [JsonProperty("insured_Name")]
        public string InsuredName { get; set; }

        [JsonProperty("insured_Birth")]
        public DateTime InsuredBirth { get; set; }

        [JsonProperty("inured_Gender")] // Lưu ý: có thể là lỗi chính tả trong mẫu của bạn, nên là "insured_Gender"
        public string InuredGender { get; set; }

        [JsonProperty("hub_Contract_Spilits")]
        public List<HubContractSplitDto> HubContractSplits { get; set; }
    }
}