using Microsoft.AspNetCore.Mvc;

namespace pviBase.Dtos
{
    public class CreateContractFormDto
    {
        [FromForm(Name = "product_Code")]
        public string? ProductCode { get; set; }

        [FromForm(Name = "accessKey")]
        public string? AccessKey { get; set; }
        [FromForm(Name = "ma_chuongtrinh")]
        public string? MaChuongTrinh { get; set; }

        [FromForm(Name = "loanNo")]
        public string? LoanNo { get; set; }

        [FromForm(Name = "loanType")]
        public string? LoanType { get; set; }

        [FromForm(Name = "loanDate")]
        public string? LoanDate { get; set; }

        [FromForm(Name = "custName")]
        public string? CustName { get; set; }

        [FromForm(Name = "custBirthday")]
        public string? CustBirthday { get; set; }

        [FromForm(Name = "custGender")]
        public string? CustGender { get; set; }

        [FromForm(Name = "custIdNo")]
        public string? CustIdNo { get; set; }

        [FromForm(Name = "custAddress")]
        public string? CustAddress { get; set; }

        [FromForm(Name = "custPhone")]
        public string? CustPhone { get; set; }

        [FromForm(Name = "custEmail")]
        public string? CustEmail { get; set; }

        [FromForm(Name = "loanAmount")]
        public long LoanAmount { get; set; }

        [FromForm(Name = "loanTerm")]
        public int LoanTerm { get; set; }

        [FromForm(Name = "insRate")]
        public double InsRate { get; set; }

        [FromForm(Name = "disbursementDate")]
        public string? DisbursementDate { get; set; }

        [FromForm(Name = "allAttachments")]
        public List<IFormFile>? AllAttachments { get; set; }
    }

}
