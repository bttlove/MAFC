// Validators/GetContractByLoanNoRequestDtoValidator.cs
using FluentValidation;
using pviBase.Dtos;

namespace pviBase.Validators
{
    public class GetContractByLoanNoRequestDtoValidator : AbstractValidator<GetContractByLoanNoRequestDto>
    {
        public GetContractByLoanNoRequestDtoValidator()
        {
            RuleFor(x => x.AccessKey).NotEmpty().WithMessage("Access key là bắt buộc.");
            RuleFor(x => x.ProductCode).NotEmpty().WithMessage("Mã sản phẩm là bắt buộc.")
                                       .Must(x => x == "MAFC_SKNVV").WithMessage("Mã sản phẩm không hợp lệ. Giá trị mặc định là MAFC_SKNVV.");
            RuleFor(x => x.LoanNo).NotEmpty().WithMessage("Số hợp đồng tín dụng là bắt buộc.");
        }
    }
}