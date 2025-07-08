// Controllers/HubContractController.cs
using pviBase.Dtos;
using pviBase.Models;
using pviBase.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace pviBase.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
 
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
        /// Creates new insurance contracts.
        /// </summary>
        /// <param name="request">The contract creation request.</param>
        /// <returns>A status indicating success or failure.</returns>
        [HttpPost("MAFC_SKNVV_CreateContract")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 403)] // Forbidden for IP Whitelist
        [ProducesResponseType(typeof(ApiResponse), 429)] // Too Many Requests for Rate Limiting
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<IActionResult> CreateContract([FromBody] CreateContractRequestDto request)
        {
            // The validation is handled by FluentValidation.AspNetCore
            // If validation fails, the ExceptionHandlingMiddleware will catch it and return a 400.

            _logger.LogInformation($"Received request to create contracts. Product Code: {request.ProductCode}, Data Count: {request.Data.Count}");

            // Basic access_key check (for demonstration, use proper auth in production)
            if (request.AccessKey != "2672ECD7-97F3-4ABE-9B6E-3415BCBDA1C2") // Replace with a secure key or actual authentication
            {
                return Unauthorized(new ApiResponse(false, "401", "Invalid access key."));
            }

            // Business logic for contract creation
            var createdContracts = await _insuranceService.CreateInsuranceContracts(request.Data);

            // Response is automatically wrapped by ResponseWrappingMiddleware
            return Ok(new { }); // Return an empty object or relevant data if needed
        }

        /// <summary>
        /// Gets an insurance contract by loan number.
        /// </summary>
        /// <param name = "loanNo" > The loan number of the contract.</param>
        /// <returns>The insurance contract details.</returns>
        //[HttpGet("{loanNo}")]
        //[MapToApiVersion("1.0")]
        //[ProducesResponseType(typeof(ApiResponse<InsuranceContract>), 200)]
        //[ProducesResponseType(typeof(ApiResponse), 404)]
        //[ProducesResponseType(typeof(ApiResponse), 403)]
        //[ProducesResponseType(typeof(ApiResponse), 429)]
        //[ProducesResponseType(typeof(ApiResponse), 500)]
        //public async Task<IActionResult> GetContract(string loanNo)
        //{
        //    _logger.LogInformation($"Attempting to retrieve contract with LoanNo: {loanNo}");
        //    var contract = await _insuranceService.GetContractByLoanNo(loanNo);

        //    if (contract == null)
        //    {
        //        _logger.LogWarning($"Contract with LoanNo {loanNo} not found.");
        //        return NotFound(new ApiResponse(false, "404", $"Contract with LoanNo {loanNo} not found."));
        //    }

        //    _logger.LogInformation($"Successfully retrieved contract with LoanNo: {loanNo}");
        //    // Response will be wrapped by ResponseWrappingMiddleware
        //    return Ok(contract);
        //}


        /// <summary>
        /// Truy vấn thông tin hợp đồng chi tiết theo số hợp đồng tín dụng (LoanNo).
        /// </summary>
        /// <param name="request">Yêu cầu truy vấn hợp đồng.</param>
        /// <returns>Thông tin hợp đồng chi tiết.</returns>
        [HttpPost("MAFC_SKNVV_GetContract_ByLoanNo")] // Endpoint POST mới
        [MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(ApiResponse<GetContractByLoanNoResponseDataDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<IActionResult> GetContractByLoanNo([FromBody] GetContractByLoanNoRequestDto request)
        {
            _logger.LogInformation($"Received request to get detailed contract for LoanNo: {request.LoanNo}");

            // Kiểm tra access_key tương tự như CreateContract
            if (request.AccessKey != "2672ECD7-97F3-4ABE-9B6E-3415BCBDA1C2")
            {
                return Unauthorized(new ApiResponse(false, "401", "Invalid access key."));
            }

            // Lấy thông tin hợp đồng chi tiết từ service
            var detailedContract = await _insuranceService.GetDetailedContractByLoanNo(request.LoanNo);

            if (detailedContract == null)
            {
                _logger.LogWarning($"Detailed contract for LoanNo {request.LoanNo} not found.");
                return NotFound(new ApiResponse(false, "404", $"Detailed contract for LoanNo {request.LoanNo} not found."));
            }

            _logger.LogInformation($"Successfully retrieved detailed contract for LoanNo: {request.LoanNo}");
            return Ok(detailedContract);
        }
    }
}