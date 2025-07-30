using pviBase.Dtos;
using pviBase.Models;
using pviBase.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.RateLimiting;
using pviBase.Helpers;
using FluentValidation;
using System.Linq;
using Newtonsoft.Json;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using pviBase.Data;

namespace pviBase.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class HubContractController : ControllerBase
    {
        private readonly IInsuranceService _insuranceService;
        private readonly ILogger<HubContractController> _logger;
        private readonly IValidator<CreateContractRequestDto> _createValidator;
        private readonly IValidator<GetContractByLoanNoRequestDto> _getByLoanNoValidator;
        private readonly PviApiForwardService _pviApiForwardService;
        private readonly ApplicationDbContext _dbContext; // ✅ THÊM ApplicationDbContext
        private readonly RequestLogService _requestLogService;

        public HubContractController(
            IInsuranceService insuranceService,
            ILogger<HubContractController> logger,
            IValidator<CreateContractRequestDto> createValidator,
            IValidator<GetContractByLoanNoRequestDto> getByLoanNoValidator,
            PviApiForwardService pviApiForwardService,
            ApplicationDbContext dbContext // ✅ Inject thêm vào constructor
        )
        {
            _insuranceService = insuranceService;
            _logger = logger;
            _createValidator = createValidator;
            _getByLoanNoValidator = getByLoanNoValidator;
            _pviApiForwardService = pviApiForwardService;
            _dbContext = dbContext; // ✅ Gán lại vào field
        }
        /// <summary>
        /// Forward request và file base64 sang API PVI thật (test demo)
        /// </summary>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("forward-pvi")]
        public async Task<IActionResult> ForwardToPviApi([FromForm] string json, [FromForm] IFormFile file)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogError("[ForwardToPviApi] JSON is null or empty");
                    return BadRequest("JSON is required");
                }
                _logger.LogInformation($"[ForwardToPviApi] Forward trực tiếp request lên API PVI thật. json null? {json == null}, file null? {file == null}");

                byte[]? fileBytes = null;
                string? fileName = null;
                string? contentTypeFile = null;
                if (file != null)
                {
                    using var ms = new System.IO.MemoryStream();
                    await file.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                    fileName = file.FileName;
                    contentTypeFile = file.ContentType;
                }

                var response = await _pviApiForwardService.ForwardRawRequestToPviApi(
                    json,
                    fileBytes ?? Array.Empty<byte>(),
                    fileName ?? "",
                    contentTypeFile ?? ""
                );
                _logger.LogInformation($"[ForwardToPviApi] Nhận response từ API thật: {response}");
                return Content(response, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ForwardToPviApi] Exception khi xử lý request");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test upload file API - trả về số lượng file và tên file nhận được.
        /// </summary>
        [HttpPost("test-upload")]
        public IActionResult TestUpload([FromForm] List<IFormFile> allAttachments)
        {
            // Log headers
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var contentType = Request.ContentType;
            var modelStateValid = ModelState.IsValid;
            var modelStateErrors = ModelState
                .Where(x => x.Value?.Errors != null && x.Value.Errors.Count > 0)
                .Select(x => new
                {
                    x.Key,
                    Errors = x.Value?.Errors?.Select(e => e.ErrorMessage) ?? new List<string>()
                });

            _logger.LogInformation($"[TestUpload] Content-Type: {contentType}");
            _logger.LogInformation($"[TestUpload] Headers: {JsonConvert.SerializeObject(headers)}");
            _logger.LogInformation($"[TestUpload] ModelState.IsValid: {modelStateValid}");
            if (!modelStateValid)
            {
                _logger.LogWarning($"[TestUpload] ModelState errors: {JsonConvert.SerializeObject(modelStateErrors)}");
            }

            return Ok(new
            {
                count = allAttachments?.Count ?? 0,
                names = allAttachments?.Select(f => f.FileName),
                contentType,
                modelStateValid,
                modelStateErrors,
                headers
            });
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
        [HttpPost("CreateContract")]
        public async Task<IActionResult> CreateContract([FromForm] CreateContractFormDto form)
        {

            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var contentType = Request.ContentType;
            var modelStateValid = ModelState.IsValid;
            _logger.LogInformation($"[CreateContract] Content-Type: {contentType}");
            _logger.LogInformation($"[CreateContract] Headers: {JsonConvert.SerializeObject(headers)}");
            _logger.LogInformation($"[CreateContract] ModelState.IsValid: {modelStateValid}");

            if (!modelStateValid)
            {
                var modelStateErrors = ModelState
                    .Where(x => x.Value?.Errors != null && x.Value.Errors.Count > 0)
                    .Select(x => new
                    {
                        x.Key,
                        Errors = x.Value?.Errors?.Select(e => e.ErrorMessage) ?? new List<string>()
                    });

                _logger.LogWarning($"[CreateContract] ModelState errors: {JsonConvert.SerializeObject(modelStateErrors)}");
                return BadRequest(new ApiResponse(false, "INVALID_MODELSTATE", "Dữ liệu không hợp lệ", modelStateErrors));
            }

            // --- Kiểm tra rule hợp lệ cho các trường: ma_chuongtrinh, phi_tyle_phi, thoihan_bh, sotien_bh ---

            // Bảng rule chương trình bảo hiểm (9 chương trình, nhiều thời hạn, phần trăm số tiền bảo hiểm)
            var programRules = new Dictionary<string, (double feeRate, Dictionary<int, double> insuredPercents)>
            {
                { "CT01", (0.03, new Dictionary<int, double> { {12, 2.5}, {24, 1.5}, {36, 1.2}, {48, 1.2} }) },
                { "CT02", (0.033, new Dictionary<int, double> { {12, 2.75}, {24, 1.65}, {36, 1.3}, {48, 1.3} }) },
                { "CT03", (0.045, new Dictionary<int, double> { {12, 3.75}, {24, 2.25}, {36, 1.8}, {48, 1.8} }) },
                { "CT04", (0.05,  new Dictionary<int, double> { {12, 4.15}, {24, 2.5}, {36, 2.0}, {48, 2.0} }) },
                { "CT05", (0.055, new Dictionary<int, double> { {12, 4.6}, {24, 2.75}, {36, 2.2}, {48, 2.2} }) },
                { "CT06", (0.06,  new Dictionary<int, double> { {12, 5.0}, {24, 3.0}, {36, 2.4}, {48, 2.4} }) },
                { "CT07", (0.066, new Dictionary<int, double> { {12, 5.5}, {24, 3.3}, {36, 2.64}, {48, 2.64} }) },
                { "CT08", (0.07,  new Dictionary<int, double> { {12, 5.8}, {24, 3.5}, {36, 2.8}, {48, 2.8} }) },
                { "CT09", (0.077, new Dictionary<int, double> { {12, 6.0}, {24, 3.8}, {36, 3.0}, {48, 3.0} }) }
            };

            string ma_chuongtrinh = string.IsNullOrWhiteSpace(form.MaChuongTrinh) ? "" : form.MaChuongTrinh.Trim().ToUpper();
            int months = form.LoanTerm;
            // Làm tròn thời hạn bảo hiểm lên mốc gần nhất: 12, 24, 36, 48
            if (months < 12)
                months = 12;
            else if (months > 12 && months <= 24)
                months = 24;
            else if (months > 24 && months <= 36)
                months = 36;
            else if (months > 36)
                months = 48;

            long loanAmount = form.LoanAmount;
            var ruleErrors = new List<string>();
            // Kiểm tra CustGender chỉ nhận 'M' hoặc 'FM'
            if (!string.IsNullOrEmpty(form.CustGender) && form.CustGender != "M" && form.CustGender != "FM")
            {
                ruleErrors.Add($"CustGender chỉ nhận giá trị 'M' hoặc 'FM'. Nhận: {form.CustGender}");
            }
            if (loanAmount < 1 || loanAmount > 100_000_000)
            {
                ruleErrors.Add($"Số tiền vay (loanAmount) phải từ 1 đến 100,000,000. Nhận: {loanAmount}");
            }
            if (!programRules.ContainsKey(ma_chuongtrinh))
            {
                ruleErrors.Add($"Mã chương trình không hợp lệ: {ma_chuongtrinh}");
            }
            else if (!programRules[ma_chuongtrinh].insuredPercents.ContainsKey(months))
            {
                var validTerms = string.Join(", ", programRules[ma_chuongtrinh].insuredPercents.Keys.Select(k => k + " tháng"));
                ruleErrors.Add($"Thời hạn bảo hiểm không hợp lệ cho chương trình {ma_chuongtrinh}. Đúng: {validTerms}, nhận: {months} tháng");
            }

            // Kiểm tra insRate client truyền lên
            if (Math.Abs(form.InsRate - programRules[ma_chuongtrinh].feeRate) > 0.00001)
            {
                ruleErrors.Add($"InsRate truyền lên không đúng rule. Đúng: {programRules[ma_chuongtrinh].feeRate}, nhận: {form.InsRate}");
            }

            if (ruleErrors.Count > 0)
            {
                _logger.LogWarning($"[CreateContract] Rule validation errors: {JsonConvert.SerializeObject(ruleErrors)}");
                return BadRequest(new ApiResponse(false, "INVALID_RULE", "Dữ liệu không hợp lệ theo rule chương trình bảo hiểm", ruleErrors));
            }

            // Tính toán đúng rule
            var rule = programRules[ma_chuongtrinh];
            double phi_tyle_phi = rule.feeRate;
            var percent = rule.insuredPercents[months];
            long sotien_bh = (long)(loanAmount * percent);
            double tong_phi_bh = loanAmount * phi_tyle_phi;

            // Kiểm tra trùng mã hợp đồng trước khi lưu DB
            bool existed = _dbContext.InsuranceContracts.Any(x => x.LoanNo == form.LoanNo);
            if (existed)
            {
                _logger.LogWarning($"[CreateContract] Mã hợp đồng đã tồn tại: {form.LoanNo}");
                return BadRequest(new ApiResponse(false, "DUPLICATE_LOANNO", $"Mã hợp đồng đã tồn tại: {form.LoanNo}"));
            }

            // Lấy dữ liệu trực tiếp từ form
            string CpId = "15e1c1cf1c2a4ae8a5f0e5c8c10bf3a6";
            string StartTime = "00:00";
            string EndTime = "23:59";
            string ngay_batdau = form.DisbursementDate ?? "01/01/2026";
            string ma_gdich_doitac = form.LoanNo ?? "";

            // Tính hạn bảo hiểm
            string thoihan_bh = "";
            if (!string.IsNullOrEmpty(ngay_batdau))
            {
                DateTime dtStart;
                if (DateTime.TryParseExact(ngay_batdau, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out dtStart))
                {
                    var dtEnd = dtStart.AddMonths(months);
                    thoihan_bh = dtEnd.ToString("dd/MM/yyyy");
                }
            }

            // File đính kèm
            var FileAttach = new List<dynamic>();
            byte[]? fileBytes = null;
            string? fileName = null;
            string? contentTypeFile = null;

            if (form.AllAttachments != null && form.AllAttachments.Count > 0)
            {
                var file = form.AllAttachments[0];
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                    fileName = file.FileName;
                    contentTypeFile = file.ContentType;

                    var fileAttach = pviBase.Services.PviApiForwardService.BuildFileAttach(file.FileName, fileBytes, Path.GetExtension(file.FileName), "GYC");
                    FileAttach.Add(fileAttach);
                }
            }

            // Tính Sign
            string keycp = "1ab8972c95fe4e3e8bec7fe83a4cdaab";
            var signFields = new List<(string name, object value)>
    {
        ("ngay_batdau", ngay_batdau),
        ("thoihan_bh", thoihan_bh),
        ("ma_gdich_doitac", ma_gdich_doitac),
        ("sotien_bh", sotien_bh),
        ("tong_phi_bh", tong_phi_bh),
        ("StartTime", StartTime),
        ("EndTime", EndTime)
    };

            string signString = keycp;
            foreach (var f in signFields)
            {
                if (f.value == null) continue;

                switch (f.value)
                {
                    case string sVal:
                        if (!string.IsNullOrEmpty(sVal)) signString += sVal;
                        break;

                    case long lVal:
                        if (lVal != 0) signString += lVal.ToString();
                        break;

                    case int iVal:
                        if (iVal != 0) signString += iVal.ToString();
                        break;

                    case double dVal:
                        if (dVal != 0)
                        {
                            if (dVal % 1 == 0)
                                signString += ((long)dVal).ToString();
                            else
                                signString += dVal.ToString("0.################", CultureInfo.InvariantCulture);
                        }
                        break;

                    default:
                        var str = f.value.ToString();
                        if (!string.IsNullOrEmpty(str) && str != "0")
                            signString += str;
                        break;
                }
            }

            string Sign = pviBase.Helpers.Md5Helper.TinhMD5(signString).ToLower();

            // Build body gửi PVI
            var pviBody = new
            {
                CpId,
                StartTime,
                EndTime,
                product_Code = form.ProductCode ?? "",
                nguoi_thuhuong = form.CustName ?? "",
                dia_chi_th = form.CustAddress ?? "",
                quyen_loibh = "Tai nạn cá nhân",
                dtbh_tg_cho_01 = true,
                dtbh_tg_cho_02 = false,
                dtbh_tg_cho_03 = true,
                dien_thoai = form.CustPhone ?? "",
                khach_hang = form.CustName ?? "",
                sotien_bh = sotien_bh != 0 ? (object)sotien_bh : 0,
                thoihan_bh,
                phi_tyle_phi,
                tong_phi_bh,
                Email = form.CustEmail ?? "",
                ngay_batdau,
                dia_chi = form.CustAddress ?? "",
                ma_gdich_doitac,
                ma_sp = "010402",
                ma_chuongtrinh = form.MaChuongTrinh ?? "",
                NguoiDinhKem = new[]
                {
                    new {
                        ho_ten = form.CustName ?? "",
                        gioi_tinh = form.CustGender ?? "",
                        ngay_sinh = form.CustBirthday ?? "",
                        dia_chi = form.CustAddress ?? "",
                        dien_thoai = form.CustPhone ?? "",
                        cmt_hc = form.CustIdNo ?? ""
                    }
                },
                sohopdong_tindung = form.LoanNo ?? "",
                ngayhopdong_tindung = form.LoanDate ?? "01/01/2025",
                laisuat_chovay = phi_tyle_phi,
                FileAttach,
                Sign
            };

            string jsonToSend = JsonConvert.SerializeObject(pviBody);
            _logger.LogInformation($"[CreateContract] JSON gửi đi: {jsonToSend}");
            _logger.LogInformation($"[CreateContract] Sign trong JSON gửi đi: {Sign}");

            // --- Gửi về PVI ---
            var response = await _pviApiForwardService.ForwardRawRequestToPviApi(
                jsonToSend,
                fileBytes ?? Array.Empty<byte>(),
                fileName ?? "",
                contentTypeFile ?? ""
            );
            _logger.LogInformation($"[CreateContract] Forwarded to PVI, response: {response}");

            // --- Lưu xuống DB ---
            var contract = new InsuranceContract
            {
                LoanNo = form.LoanNo ?? "",
                LoanType = form.LoanType ?? "DEFAULT",
                LoanDate = ParseDate(form.LoanDate, "01/01/2025"),
                CustName = form.CustName ?? "",
                CustBirthday = ParseDate(form.CustBirthday, "01/01/2000"),
                CustGender = form.CustGender ?? "",
                CustIdNo = form.CustIdNo ?? "",
                CustAddress = form.CustAddress ?? "",
                CustPhone = form.CustPhone ?? "",
                CustEmail = form.CustEmail ?? "",
                LoanAmount = sotien_bh,
                LoanTerm = form.LoanTerm,
                InsRate = phi_tyle_phi,
                DisbursementDate = ParseDate(form.DisbursementDate, "01/01/2026"),
                AttachmentData = fileBytes,
                AttachmentFileName = fileName,
                AttachmentContentType = contentTypeFile,
                CreatedAt = DateTime.UtcNow.AddHours(7) // UTC+7 cho Việt Nam
            };

            _dbContext.InsuranceContracts.Add(contract);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"[CreateContract] Đã lưu DB với ID: {contract.Id}, LoanNo: {contract.LoanNo}");

            return Content(response, "application/json");
        }

        private DateTime ParseDate(dynamic? dateStr, string fallback)
        {
            if (dateStr == null) dateStr = fallback;
            DateTime dt;
            if (DateTime.TryParseExact((string)dateStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out dt))
            {
                return dt;
            }
            return DateTime.ParseExact(fallback, "dd/MM/yyyy", null);
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
        public async Task<IActionResult> GetContractByLoanNo([FromForm] GetContractByLoanNoRequestDto request)
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
        [HttpGet("download-attachment/{loanNo}")]
        public async Task<IActionResult> DownloadAttachment(string loanNo)
        {
            var contract = await _insuranceService.GetContractByLoanNo(loanNo);
            if (contract == null || contract.AttachmentData == null)
                return NotFound("Không tìm thấy file đính kèm!");

            return File(contract.AttachmentData, contract.AttachmentContentType ?? "application/octet-stream", contract.AttachmentFileName ?? "attachment");
        }
    }
}
