// Controllers/HubContractController.cs
using pviBase.Dtos;
using pviBase.Models;
using pviBase.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.RateLimiting;
using pviBase.Helpers; // Cần cho ErrorCodes

namespace pviBase.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    // [EnableRateLimiting("fixed")]
    public class HubContractController : ControllerBase
    {
        private readonly IInsuranceService _insuranceService;
        private readonly ILogger<HubContractController> _logger;

        public HubContractController(IInsuranceService insuranceService, ILogger<HubContractController> logger)
        {
            _insuranceService = insuranceService;
            _logger = logger;
        }

        /// <summary>
        /// Creates new insurance contracts asynchronously.
        /// </summary>
        /// <param name="request">The contract creation request.</param>
        /// <returns>A status indicating that the request has been accepted for processing, along with a RequestId.</returns>
        [HttpPost("MAFC_SKNVV_CreateContract")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(ApiResponse<string>), 202)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 429)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<IActionResult> CreateContract([FromBody] CreateContractRequestDto request)
        {
            _logger.LogInformation($"Received request to create contracts. Product Code: {request.ProductCode}, Data Count: {request.Data.Count}");

            if (request.AccessKey != "2672ECD7-97F3-4ABE-9B6E-3415BCBDA1C2")
            {
                // Sử dụng mã lỗi và thông báo mới cho Access Key
                return Unauthorized(new ApiResponse(false, ErrorCodes.AccessKeyNotFoundCode, ErrorCodes.AccessKeyNotFoundMessage));
            }

            string requestId = await _insuranceService.EnqueueCreateInsuranceContractsJob(request);

            // Sử dụng mã thành công và thông báo mới
            return Accepted(new ApiResponse<string>(true, ErrorCodes.SuccessCode, ErrorCodes.SuccessMessage, requestId));
        }

        /// <summary>
        /// Gets an insurance contract by loan number.
        /// </summary>
        /// <param name="loanNo">The loan number of the contract.</param>
        /// <returns>The insurance contract details.</returns>
        [HttpGet("{loanNo}")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(ApiResponse<InsuranceContract>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        [ProducesResponseType(typeof(ApiResponse), 429)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<IActionResult> GetContract(string loanNo)
        {
            _logger.LogInformation($"Attempting to retrieve contract with LoanNo: {loanNo}");
            var contract = await _insuranceService.GetContractByLoanNo(loanNo);

            if (contract == null)
            {
                // Sử dụng mã lỗi và thông báo mới cho Contract Not Found
                _logger.LogWarning($"Contract with LoanNo {loanNo} not found.");
                return NotFound(new ApiResponse(false, ErrorCodes.ContractNotFoundCode, ErrorCodes.ContractNotFoundMessage));
            }

            _logger.LogInformation($"Successfully retrieved contract with LoanNo: {loanNo}");
            // Phản hồi thành công sẽ được ResponseWrappingMiddleware bọc với mã "000"
            return Ok(contract);
        }

        /// <summary>
        /// Truy vấn thông tin hợp đồng chi tiết theo số hợp đồng tín dụng (LoanNo).
        /// </summary>
        /// <param name="request">Yêu cầu truy vấn hợp đồng.</param>
        /// <returns>Thông tin hợp đồng chi tiết.</returns>
        [HttpPost("MAFC_SKNVV_GetContract_ByLoanNo")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(ApiResponse<GetContractByLoanNoResponseDataDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<IActionResult> GetContractByLoanNo([FromBody] GetContractByLoanNoRequestDto request)
        {
            _logger.LogInformation($"Received request to get detailed contract for LoanNo: {request.LoanNo}");

            if (request.AccessKey != "2672ECD7-97F3-4ABE-9B6E-3415BCBDA1C2")
            {
                // Sử dụng mã lỗi và thông báo mới cho Access Key
                return Unauthorized(new ApiResponse(false, ErrorCodes.AccessKeyNotFoundCode, ErrorCodes.AccessKeyNotFoundMessage));
            }

            var detailedContract = await _insuranceService.GetDetailedContractByLoanNo(request.LoanNo);

            if (detailedContract == null)
            {
                // Sử dụng mã lỗi và thông báo mới cho Contract Not Found
                _logger.LogWarning($"Detailed contract for LoanNo {request.LoanNo} not found.");
                return NotFound(new ApiResponse(false, ErrorCodes.ContractNotFoundCode, ErrorCodes.ContractNotFoundMessage));
            }

            _logger.LogInformation($"Successfully retrieved detailed contract for LoanNo: {request.LoanNo}");
            // Phản hồi thành công sẽ được ResponseWrappingMiddleware bọc với mã "000"
            return Ok(detailedContract);
        }

        /// <summary>
        /// Truy vấn trạng thái của một yêu cầu xử lý hợp đồng.
        /// </summary>
        /// <param name="requestId">ID duy nhất của yêu cầu.</param>
        /// <returns>Trạng thái chi tiết của yêu cầu.</returns>
        [HttpGet("status/{requestId}")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(ApiResponse<RequestLog>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<IActionResult> GetRequestStatus(string requestId)
        {
            _logger.LogInformation($"Attempting to retrieve status for RequestId: {requestId}");
            var requestLog = await _insuranceService.GetRequestLogById(requestId);

            if (requestLog == null)
            {
                _logger.LogWarning($"Request with ID {requestId} not found.");
                return NotFound(new ApiResponse(false, ErrorCodes.ContractNotFoundCode, ErrorCodes.ContractNotFoundMessage)); // Có thể dùng mã lỗi 404 chung
            }

            _logger.LogInformation($"Successfully retrieved status for RequestId: {requestId}. Status: {requestLog.Status}");

            // Nếu trạng thái là Pending, có thể trả về mã ERR_004
            if (requestLog.Status == RequestStatus.Pending)
            {
                return Ok(new ApiResponse<RequestLog>(true, ErrorCodes.ContractPendingCode, ErrorCodes.ContractPendingMessage, requestLog));
            }
            // Các trạng thái khác (Processing, Completed, Failed) sẽ trả về mã SuccessCode "000"
            // hoặc bạn có thể định nghĩa mã riêng cho Processing/Failed nếu muốn.
            // Hiện tại, chúng ta trả về RequestLog và để client tự phân tích Status.
            return Ok(new ApiResponse<RequestLog>(true, ErrorCodes.SuccessCode, ErrorCodes.SuccessMessage, requestLog));
        }
    }
}
