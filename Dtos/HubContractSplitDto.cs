// Dtos/HubContractSplitDto.cs
using Newtonsoft.Json;
using System;

namespace pviBase.Dtos
{
    public class HubContractSplitDto
    {
        [JsonProperty("ctS_Contract_Index")]
        public int CtsContractIndex { get; set; }

        [JsonProperty("ctS_Contract_StartDate")]
        public DateTime CtsContractStartDate { get; set; }

        [JsonProperty("ctS_Contract_EndDate")]
        public DateTime CtsContractEndDate { get; set; }

        [JsonProperty("ctS_PVI_Ref_Id")]
        public string? CtsPviRefId { get; set; }

        [JsonProperty("ctS_PVI_Ref_PolNo")]
        public string? CtsPviRefPolNo { get; set; }

        [JsonProperty("ctS_PVI_Ref_Link")]
        public string? CtsPviRefLink { get; set; }
    }
}