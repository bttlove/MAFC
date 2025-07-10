using Microsoft.AspNetCore.Mvc;

namespace pviBase.Dtos
{
    public class CreateContractFormDto
    {
        [FromForm(Name = "request")]
        public string RequestJson { get; set; } = null!;

        [FromForm(Name = "allAttachments")]
        public List<IFormFile>? AllAttachments { get; set; }
    }

}
