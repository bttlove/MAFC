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
using Newtonsoft.Json;
using System.Globalization;

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
        private readonly PviApiForwardService _pviApiForwardService;

        public HubContractController(
            IInsuranceService insuranceService,
            ILogger<HubContractController> logger,
            IValidator<CreateContractRequestDto> createValidator,
            IValidator<GetContractByLoanNoRequestDto> getByLoanNoValidator,
            PviApiForwardService pviApiForwardService)
        {
            _insuranceService = insuranceService;
            _logger = logger;
            _createValidator = createValidator;
            _getByLoanNoValidator = getByLoanNoValidator;
            _pviApiForwardService = pviApiForwardService;
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
                var response = await _pviApiForwardService.ForwardRawRequestToPviApi(json, file);
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
            // --- LOGIC Y CHANG POSTMAN, TÊN BIẾN, FIELD, SIGNATURE ---
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

            _logger.LogInformation($"[CreateContract] Content-Type: {contentType}");
            _logger.LogInformation($"[CreateContract] Headers: {JsonConvert.SerializeObject(headers)}");
            _logger.LogInformation($"[CreateContract] ModelState.IsValid: {modelStateValid}");
            if (!modelStateValid)
            {
                _logger.LogWarning($"[CreateContract] ModelState errors: {JsonConvert.SerializeObject(modelStateErrors)}");
            }

            // Parse JSON từ trường request
            var requestJson = form.RequestJson;
            dynamic? req = JsonConvert.DeserializeObject(requestJson);
            if (req == null)
            {
                return BadRequest(new ApiResponse(false, "INVALID_JSON", "Không thể phân tích request JSON"));
            }


            // Map fields y chang Postman, đúng tên biến, đúng thứ tự, tính toán động
            var d = req.data?[0];
            string CpId = "15e1c1cf1c2a4ae8a5f0e5c8c10bf3a6";
            string StartTime = "00:00";
            string EndTime = "23:59";
            string nguoi_thuhuong = (string)(d?.custName ?? "");
            string dia_chi_th = (string)(d?.custAddress ?? "");
            string quyen_loibh = (string)(d?.quyen_loibh ?? "Tai nạn cá nhân");
            bool dtbh_tg_cho_01 = d?.dtbh_tg_cho_01 != null ? (bool)d.dtbh_tg_cho_01 : true;
            bool dtbh_tg_cho_02 = d?.dtbh_tg_cho_02 != null ? (bool)d.dtbh_tg_cho_02 : false;
            bool dtbh_tg_cho_03 = true; // luôn true theo yêu cầu
            string dien_thoai = (string)(d?.custPhone ?? "");
            string khach_hang = (string)(d?.custName ?? "");
            long sotien_bh = d?.loanAmount != null ? (long)d.loanAmount : 0;
            double phi_tyle_phi = d?.insRate != null ? (double)d.insRate : 0;
            string Email = (string)(d?.custEmail ?? "");
            string ngay_batdau = (string)(d?.disbursementDate ?? "01/01/2026");
            string dia_chi = (string)(d?.custAddress ?? "");
            string ma_gdich_doitac = (string)(d?.loanNo ?? "");
            string ma_sp = "010402";
            string ma_chuongtrinh = "CT01";
            string sohopdong_tindung = (string)(d?.loanNo ?? "");
            string ngayhopdong_tindung = (string)(d?.loanDate ?? "01/01/2025");
            double laisuat_chovay = d?.insRate != null ? (double)d.insRate : 0;

            // Tính thoihan_bh: nếu có d.thoihan_bh thì lấy, nếu không thì lấy ngày_batdau + số tháng (loanTerm)
            string thoihan_bh = "";
            if (!string.IsNullOrEmpty((string)(d?.thoihan_bh ?? "")))
            {
                thoihan_bh = (string)d.thoihan_bh;
            }
            else if (d?.loanTerm != null && !string.IsNullOrEmpty(ngay_batdau))
            {
                // loanTerm là số tháng, ngày_batdau dạng dd/MM/yyyy
                int months = 0;
                int.TryParse(d.loanTerm.ToString(), out months);
                DateTime dtStart;
                if (DateTime.TryParseExact(ngay_batdau, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out dtStart))
                {
                    var dtEnd = dtStart.AddMonths(months);
                    thoihan_bh = dtEnd.ToString("dd/MM/yyyy");
                }
            }
            else
            {
                thoihan_bh = "";
            }

            // Tính tong_phi_bh = sotien_bh * phi_tyle_phi, luôn là kiểu số (double hoặc long)
            double tong_phi_bh = 0;
            if (d?.loanAmount != null && d?.insRate != null)
            {
                double rate = 0;
                if (d.insRate is double)
                {
                    rate = (double)d.insRate;
                }
                else if (d.insRate is string s && double.TryParse(s, out var parsed))
                {
                    rate = parsed;
                }
                else
                {
                    double.TryParse(d.insRate.ToString(), out rate);
                }
                tong_phi_bh = sotien_bh * rate;
            }

            // Danh sách đính kèm
            var NguoiDinhKem = new List<dynamic>
            {
                new {
                    ho_ten = (string)(d?.custName ?? ""),
                    gioi_tinh = (string)(d?.custGender ?? ""),
                    ngay_sinh = (string)(d?.custBirthday ?? ""),
                    dia_chi = (string)(d?.custAddress ?? ""),
                    dien_thoai = (string)(d?.custPhone ?? ""),
                    cmt_hc = (string)(d?.custIdNo ?? "")
                }
            };

            // FileAttach
            var FileAttach = new List<dynamic>();
            if (form.AllAttachments != null && form.AllAttachments.Count > 0)
            {
                var file = form.AllAttachments[0];
                using (var ms = new System.IO.MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();
                    var fileAttach = pviBase.Services.PviApiForwardService.BuildFileAttach(file.FileName, fileBytes, System.IO.Path.GetExtension(file.FileName), "GYC");
                    FileAttach.Add(fileAttach);
                }
            }

            // Tính toán Sign: loại bỏ hoàn toàn field nào rỗng, null, hoặc 0 khỏi chuỗi hash (y chang Postman)
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

            // Bắt đầu build chuỗi
            string signString = keycp;

            // Log từng field để kiểm tra
            _logger.LogInformation($"[CreateContract] keycp: '{keycp}' (length: {keycp.Length})");
            _logger.LogInformation($"[CreateContract] ngay_batdau: '{ngay_batdau}' (length: {ngay_batdau?.Length ?? 0})");
            _logger.LogInformation($"[CreateContract] thoihan_bh: '{thoihan_bh}' (length: {thoihan_bh?.Length ?? 0})");
            _logger.LogInformation($"[CreateContract] ma_gdich_doitac: '{ma_gdich_doitac}' (length: {ma_gdich_doitac?.Length ?? 0})");
            _logger.LogInformation($"[CreateContract] sotien_bh: '{sotien_bh}' (length: {sotien_bh.ToString().Length})");
            _logger.LogInformation($"[CreateContract] tong_phi_bh: '{tong_phi_bh}' (length: {tong_phi_bh.ToString().Length})");
            _logger.LogInformation($"[CreateContract] StartTime: '{StartTime}' (length: {StartTime?.Length ?? 0})");
            _logger.LogInformation($"[CreateContract] EndTime: '{EndTime}' (length: {EndTime?.Length ?? 0})");

            // Duyệt field để build signString
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
                            // Nếu là số nguyên (như 550000000.0), thì ép về long để loại bỏ phần ".0"
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

            // Final log
            _logger.LogInformation($"[CreateContract] Final signString for MD5: '{signString}'");
            _logger.LogInformation($"[CreateContract] Length of signString: {signString.Length}");

            // Tính MD5
            string Sign = pviBase.Helpers.Md5Helper.TinhMD5(signString).ToLower();
            _logger.LogInformation($"[CreateContract] Final MD5 Sign: {Sign}");

            // Build object đúng tên biến, đúng thứ tự
            var pviBody = new
            {
                CpId,
                StartTime,
                EndTime,
                nguoi_thuhuong,
                dia_chi_th,
                quyen_loibh,
                dtbh_tg_cho_01,
                dtbh_tg_cho_02,
                dtbh_tg_cho_03,
                dien_thoai,
                khach_hang,
                sotien_bh = sotien_bh != 0 ? (object)sotien_bh : 0,
                thoihan_bh,
                phi_tyle_phi,
                tong_phi_bh,
                Email,
                ngay_batdau,
                dia_chi,
                ma_gdich_doitac,
                ma_sp,
                ma_chuongtrinh,
                NguoiDinhKem,
                sohopdong_tindung,
                ngayhopdong_tindung,
                laisuat_chovay,
                FileAttach,
                Sign
            };

            var jsonToSend = JsonConvert.SerializeObject(pviBody);
            _logger.LogInformation($"[CreateContract] JSON gửi đi: {jsonToSend}");
            // Log riêng trường Sign trong JSON gửi đi
            try
            {
                var signInJson = Newtonsoft.Json.Linq.JObject.Parse(jsonToSend)["Sign"]?.ToString();
                _logger.LogInformation($"[CreateContract] Sign trong JSON gửi đi: {signInJson}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[CreateContract] Không thể log Sign trong JSON gửi đi: {ex.Message}");
            }
            var response = await _pviApiForwardService.ForwardRawRequestToPviApi(jsonToSend, form.AllAttachments != null && form.AllAttachments.Count > 0 ? form.AllAttachments[0] : null);
            _logger.LogInformation($"[CreateContract] Forwarded to PVI, response: {response}");
            return Content(response, "application/json");
            
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