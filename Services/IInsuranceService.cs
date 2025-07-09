// Services/IInsuranceService.cs
using pviBase.Dtos;
using pviBase.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pviBase.Services
{
    public interface IInsuranceService
    {
        // Phương thức này sẽ được gọi bởi Controller để khởi tạo job
        Task<string> EnqueueCreateInsuranceContractsJob(CreateContractRequestDto request);

        // Phương thức này sẽ được gọi bởi Hangfire Background Job
        Task ProcessCreateInsuranceContractsJob(string requestId, CreateContractRequestDto request);

        Task<InsuranceContract?> GetContractByLoanNo(string loanNo);
        Task<GetContractByLoanNoResponseDataDto?> GetDetailedContractByLoanNo(string loanNo);

        // THÊM DÒNG NÀY: Phương thức để lấy trạng thái của yêu cầu
        Task<RequestLog?> GetRequestLogById(string requestId);
    }
}