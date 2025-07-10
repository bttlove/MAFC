// Configurations/MappingProfile.cs
using AutoMapper;
using pviBase.Dtos;
using pviBase.Models;
using System;
using System.Globalization;
using System.Collections.Generic; // Cần cho List

namespace pviBase.Configurations
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapping từ Request DTO sang Model (đã có)
            CreateMap<InsuranceContractRequestDto, InsuranceContract>()
       .ForMember(dest => dest.LoanDate, opt => opt.MapFrom(src => src.LoanDate))
       .ForMember(dest => dest.CustBirthday, opt => opt.MapFrom(src => src.CustBirthday))
       .ForMember(dest => dest.DisbursementDate, opt => opt.MapFrom(src => src.DisbursementDate));


            // Mapping từ Model sang Response Data DTO mới
            CreateMap<InsuranceContract, GetContractByLoanNoResponseDataDto>()
                .ForMember(dest => dest.ImportBatchId, opt => opt.MapFrom(src => Guid.NewGuid())) // Tạo mới Guid cho demo
                .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => "MAFC_SKNVV")) // Giá trị cố định theo mẫu
                .ForMember(dest => dest.ContractPackageCode, opt => opt.MapFrom(src => MapInsRateToPackageCode(src.InsRate))) // Ánh xạ InsRate sang PackageCode
                .ForMember(dest => dest.ContractStartDate, opt => opt.MapFrom(src => src.DisbursementDate)) // Lấy từ DisbursementDate
                .ForMember(dest => dest.ContractEndDate, opt => opt.MapFrom(src => src.DisbursementDate.AddMonths(src.LoanTerm).AddDays(-1))) // Tính toán ngày kết thúc
                .ForMember(dest => dest.ContractTerm, opt => opt.MapFrom(src => src.LoanTerm))
                .ForMember(dest => dest.LoanType, opt => opt.MapFrom(src => src.LoanType))
                .ForMember(dest => dest.LoanNo, opt => opt.MapFrom(src => src.LoanNo))
                .ForMember(dest => dest.InsuredIdNo, opt => opt.MapFrom(src => src.CustIdNo))
                .ForMember(dest => dest.InsuredName, opt => opt.MapFrom(src => src.CustName))
                .ForMember(dest => dest.InsuredBirth, opt => opt.MapFrom(src => src.CustBirthday))
                .ForMember(dest => dest.InuredGender, opt => opt.MapFrom(src => src.CustGender))
                .ForMember(dest => dest.HubContractSplits, opt => opt.MapFrom(src => GenerateContractSplits(src.DisbursementDate, src.LoanTerm))); // Tạo danh sách splits
        }

        // Helper method để ánh xạ InsRate sang Contract_PackageCode
        private string MapInsRateToPackageCode(double insRate)
        {
            return insRate switch
            {
                3.0 => "CT_01",
                3.3 => "CT_02",
                4.5 => "CT_03",
                5.0 => "CT_04",
                5.5 => "CT_05",
                6.0 => "CT_06",
                6.6 => "CT_07",
                7.0 => "CT_08",
                7.7 => "CT_09",
                _ => "UNKNOWN_PACKAGE" // Trường hợp không khớp
            };
        }

        // Helper method để tạo danh sách HubContractSplits
        private List<HubContractSplitDto> GenerateContractSplits(DateTime startDate, int loanTerm)
        {
            var splits = new List<HubContractSplitDto>();
            // Giả định mỗi split là 12 tháng (1 năm)
            // Bạn có thể điều chỉnh logic này tùy theo yêu cầu thực tế của "splits"
            for (int i = 0; i < Math.Ceiling((double)loanTerm / 12); i++)
            {
                var splitStartDate = startDate.AddMonths(i * 12);
                var splitEndDate = splitStartDate.AddMonths(12).AddDays(-1); // Kết thúc vào ngày trước ngày bắt đầu của năm tiếp theo

                // Đảm bảo EndDate không vượt quá ContractEndDate tổng thể
                if (splitEndDate > startDate.AddMonths(loanTerm).AddDays(-1))
                {
                    splitEndDate = startDate.AddMonths(loanTerm).AddDays(-1);
                }

                splits.Add(new HubContractSplitDto
                {
                    CtsContractIndex = i + 1,
                    CtsContractStartDate = splitStartDate,
                    CtsContractEndDate = splitEndDate,
                    CtsPviRefId = null,
                    CtsPviRefPolNo = null,
                    CtsPviRefLink = null
                });
            }
            return splits;
        }
    }
}