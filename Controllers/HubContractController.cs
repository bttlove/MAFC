// Controllers/HubContractController.cs
using pviBase.Dtos;
using pviBase.Models;
using pviBase.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.RateLimiting;
using pviBase.Helpers;
using FluentValidation; // Thêm using này để dùng IValidator
using System.Linq;

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
        private readonly IValidator<CreateContractRequestDto> _createValidator;
        private readonly IValidator<GetContractByLoanNoRequestDto> _getByLoanNoValidator;

        public HubContractController(
            IInsuranceService insuranceService,
            ILogger<HubContractController> logger,
            IValidator<CreateContractRequestDto> createValidator,
            IValidator<GetContractByLoanNoRequestDto> getByLoanNoValidator)
        {
            _insuranceService = insuranceService;
            _logger = logger;
            _createValidator = createValidator;
            _getByLoanNoValidator = getByLoanNoValidator;
        }

        /// <summary>
        /// Creates new insurance contracts asynchronously.
        /// </summary>
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
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
                return BadRequest(new ApiResponse(false, "VALIDATION_ERROR", "Dữ liệu không hợp lệ", errors));
            }

            _logger.LogInformation($"Received request to create contracts. Product Code: {request.ProductCode}, Data Count: {request.Data.Count}");

            if (request.AccessKey != "2672ECD7-97F3-4ABE-9B6E-3415BCBDA1C2")
            {
                return Unauthorized(new ApiResponse(false, ErrorCodes.AccessKeyNotFoundCode, ErrorCodes.AccessKeyNotFoundMessage));
            }

            string requestId = await _insuranceService.EnqueueCreateInsuranceContractsJob(request);
            return Accepted(new ApiResponse<string>(true, ErrorCodes.SuccessCode, ErrorCodes.SuccessMessage, requestId));
        }

        /// <summary>
        /// Gets an insurance contract by loan number.
        /// </summary>
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
                _logger.LogWarning($"Contract with LoanNo {loanNo} not found.");
                return NotFound(new ApiResponse(false, ErrorCodes.ContractNotFoundCode, ErrorCodes.ContractNotFoundMessage));
            }

            _logger.LogInformation($"Successfully retrieved contract with LoanNo: {loanNo}");
            return Ok(contract);
        }

        /// <summary>
        /// Truy vấn thông tin hợp đồng chi tiết theo số hợp đồng tín dụng (LoanNo).
        /// </summary>
        [HttpPost("MAFC_SKNVV_GetContract_ByLoanNo")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(ApiResponse<GetContractByLoanNoResponseDataDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<IActionResult> GetContractByLoanNo([FromBody] GetContractByLoanNoRequestDto request)
        {
            var validationResult = await _getByLoanNoValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
                return BadRequest(new ApiResponse(false, "VALIDATION_ERROR", "Dữ liệu không hợp lệ", errors));
            }

            _logger.LogInformation($"Received request to get detailed contract for LoanNo: {request.LoanNo}");

            if (request.AccessKey != "2672ECD7-97F3-4ABE-9B6E-3415BCBDA1C2")
            {
                return Unauthorized(new ApiResponse(false, ErrorCodes.AccessKeyNotFoundCode, ErrorCodes.AccessKeyNotFoundMessage));
            }

            var detailedContract = await _insuranceService.GetDetailedContractByLoanNo(request.LoanNo);

            if (detailedContract == null)
            {
                _logger.LogWarning($"Detailed contract for LoanNo {request.LoanNo} not found.");
                return NotFound(new ApiResponse(false, ErrorCodes.ContractNotFoundCode, ErrorCodes.ContractNotFoundMessage));
            }

            _logger.LogInformation($"Successfully retrieved detailed contract for LoanNo: {request.LoanNo}");
            return Ok(detailedContract);
        }

        /// <summary>
        /// Truy vấn trạng thái của một yêu cầu xử lý hợp đồng.
        /// </summary>
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
                return NotFound(new ApiResponse(false, ErrorCodes.ContractNotFoundCode, ErrorCodes.ContractNotFoundMessage));
            }

            _logger.LogInformation($"Successfully retrieved status for RequestId: {requestId}. Status: {requestLog.Status}");

            if (requestLog.Status == RequestStatus.Pending)
            {
                return Ok(new ApiResponse<RequestLog>(true, ErrorCodes.ContractPendingCode, ErrorCodes.ContractPendingMessage, requestLog));
            }

            return Ok(new ApiResponse<RequestLog>(true, ErrorCodes.SuccessCode, ErrorCodes.SuccessMessage, requestLog));
        }
    }
}
