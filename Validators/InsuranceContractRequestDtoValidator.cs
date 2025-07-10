using FluentValidation;
using Microsoft.EntityFrameworkCore;
using pviBase.Data;
using pviBase.Dtos;
using System;
using System.Linq;

namespace pviBase.Validators
{
    public class InsuranceContractRequestDtoValidator : AbstractValidator<InsuranceContractRequestDto>
    {
        private readonly ApplicationDbContext _dbContext;

        public InsuranceContractRequestDtoValidator(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;

            RuleFor(x => x.LoanNo)
                .NotEmpty().WithMessage("Số hợp đồng tín dụng là bắt buộc.")
                .MustAsync(async (loanNo, cancellation) =>
                {
                    return !await _dbContext.InsuranceContracts
                            .AnyAsync(ic => ic.LoanNo == loanNo, cancellation);
                }).WithMessage("Số hợp đồng tín dụng đã tồn tại.");
            RuleFor(x => x.LoanNo).NotEmpty().WithMessage("Số hợp đồng tín dụng là bắt buộc.");
            RuleFor(x => x.LoanType).NotEmpty().WithMessage("Loại hình vay là bắt buộc.");
            RuleFor(x => x.LoanDate).NotEmpty().WithMessage("Ngày ký hợp đồng tín dụng là bắt buộc.")
                                    .Must(BeAValidDate).WithMessage("Ngày ký hợp đồng tín dụng không hợp lệ (dd/MM/yyyy).");
            RuleFor(x => x.CustName).NotEmpty().WithMessage("Họ tên khách hàng là bắt buộc.");
            RuleFor(x => x.CustBirthday).NotEmpty().WithMessage("Ngày sinh khách hàng là bắt buộc.")
                                         .Must(BeAValidDate).WithMessage("Ngày sinh khách hàng không hợp lệ (dd/MM/yyyy).")
                                         .Must(BeWithinAgeRange).WithMessage("Tuổi khách hàng phải nằm trong khoảng 18 -> 70.");
            RuleFor(x => x.CustGender).NotEmpty().WithMessage("Giới tính khách hàng là bắt buộc.")
                                      .Must(x => x == "M" || x == "F").WithMessage("Giới tính khách hàng chỉ có thể là 'M' hoặc 'F'.");
            RuleFor(x => x.CustIdNo).NotEmpty().WithMessage("Số CMND/CCNN/Passport khách hàng là bắt buộc.");
            RuleFor(x => x.CustPhone).NotEmpty().WithMessage("Số điện thoại khách hàng là bắt buộc.")
                                     .Length(10).WithMessage("Số điện thoại khách hàng phải có 10 chữ số.");
            RuleFor(x => x.LoanAmount).NotNull().WithMessage("Số tiền vay là bắt buộc.")
                                      .GreaterThanOrEqualTo(1).WithMessage("Số tiền vay phải lớn hơn hoặc bằng 1.")
                                      .LessThanOrEqualTo(100000000).WithMessage("Số tiền vay phải nhỏ hơn hoặc bằng 100,000,000.");
            RuleFor(x => x.LoanTerm).NotNull().WithMessage("Thời hạn vay là bắt buộc.")
                                    .GreaterThanOrEqualTo(1).WithMessage("Thời hạn vay phải lớn hơn hoặc bằng 1.")
                                    .LessThanOrEqualTo(48).WithMessage("Thời hạn vay phải nhỏ hơn hoặc bằng 48.");
            RuleFor(x => x.InsRate).NotNull().WithMessage("Tỷ lệ phí bảo hiểm là bắt buộc.")
                                   .Must(BeAValidInsRate).WithMessage("Tỷ lệ phí bảo hiểm không hợp lệ.");
            RuleFor(x => x.DisbursementDate).NotEmpty().WithMessage("Ngày giải ngân là bắt buộc.")
                                            .Must(BeAValidDate).WithMessage("Ngày giải ngân không hợp lệ (dd/MM/yyyy).");
        }

        private bool BeAValidDate(string dateString)
        {
            return DateTime.TryParseExact(dateString, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _);
        }

        private bool BeWithinAgeRange(string custBirthdayString)
        {
            if (!DateTime.TryParseExact(custBirthdayString, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime custBirthday))
            {
                return false;
            }

            var today = DateTime.Today;
            var age = today.Year - custBirthday.Year;
            if (custBirthday.Date > today.AddYears(-age)) age--;

            return age >= 18 && age <= 70;
        }

        private bool BeAValidInsRate(double insRate)
        {
            double[] validRates = { 3.0, 3.3, 4.5, 5.0, 5.5, 6.0, 6.6, 7.0, 7.7 };
            return validRates.Contains(insRate);
        }
    }

    public class CreateContractRequestDtoValidator : AbstractValidator<CreateContractRequestDto>
    {
        public CreateContractRequestDtoValidator(IValidator<InsuranceContractRequestDto> insuranceContractValidator)
        {
            RuleFor(x => x.AccessKey).NotEmpty().WithMessage("Access key là bắt buộc.");
            RuleFor(x => x.ProductCode).NotEmpty().WithMessage("Mã sản phẩm là bắt buộc.")
                                       .Must(x => x == "MAFC_SKNVV").WithMessage("Mã sản phẩm không hợp lệ. Giá trị mặc định là MAFC_SKNVV.");
            RuleFor(x => x.Data).NotEmpty().WithMessage("Danh sách chi tiết người tham gia bảo hiểm là bắt buộc.")
                                 .Must(data => data != null && data.Any()).WithMessage("Danh sách chi tiết người tham gia bảo hiểm không được rỗng.");
            RuleForEach(x => x.Data).SetValidator(insuranceContractValidator);
        }
    }


}