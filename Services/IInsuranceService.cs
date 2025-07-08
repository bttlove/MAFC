// Services/IInsuranceService.cs
using pviBase.Dtos;
using pviBase.Models;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace pviBase.Services
{
    public interface IInsuranceService
    {
        Task<List<InsuranceContract>> CreateInsuranceContracts(List<InsuranceContractRequestDto> contractDtos);
        Task<InsuranceContract?> GetContractByLoanNo(string loanNo);
        Task<GetContractByLoanNoResponseDataDto?> GetDetailedContractByLoanNo(string loanNo);
    }
}