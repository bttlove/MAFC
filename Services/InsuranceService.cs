// Services/InsuranceService.cs
using AutoMapper;
using pviBase.Data;
using pviBase.Dtos;
using pviBase.Helpers;
using pviBase.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using System.Linq;
using Hangfire; // Cần cho BackgroundJob.Enqueue

namespace pviBase.Services
{
    public class InsuranceService : IInsuranceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<InsuranceService> _logger;
        private readonly IValidator<InsuranceContractRequestDto> _contractValidator; // Đổi tên để rõ ràng hơn
        private readonly IValidator<CreateContractRequestDto> _createContractValidator; // Validator cho request tổng thể
        private readonly IValidator<GetContractByLoanNoRequestDto> _getContractValidator;
        public InsuranceService(
            ApplicationDbContext context,
            IMapper mapper,
            IDistributedCache cache,
            ILogger<InsuranceService> logger,
            IValidator<InsuranceContractRequestDto> contractValidator, // Tiêm validator cụ thể
            IValidator<CreateContractRequestDto> createContractValidator, // Tiêm validator tổng thể
            IValidator<GetContractByLoanNoRequestDto> getContractValidator)
        {
            _context = context;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
            _contractValidator = contractValidator;
            _createContractValidator = createContractValidator;
            _getContractValidator = getContractValidator;
        }

        // Phương thức được Controller gọi để khởi tạo và đưa job vào hàng đợi
        public async Task<string> EnqueueCreateInsuranceContractsJob(CreateContractRequestDto request)
        {
            // Validate request tổng thể trước khi đưa vào hàng đợi
            var validationResult = await _createContractValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = new Dictionary<string, string[]>();
                foreach (var errorGroup in validationResult.Errors.GroupBy(e => e.PropertyName))
                {
                    errors[errorGroup.Key] = errorGroup.Select(e => e.ErrorMessage).ToArray();
                }
                throw new pviBase.Helpers.ValidationException(errors);
            }

            // Tạo một RequestId duy nhất cho job này
            string requestId = Guid.NewGuid().ToString();

            // Ghi log yêu cầu ban đầu vào bảng RequestLogs với trạng thái Pending
            var requestLog = new RequestLog
            {
                RequestId = requestId,
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                RequestData = JsonSerializer.Serialize(request) // Lưu trữ dữ liệu request gốc
            };
            _context.RequestLogs.Add(requestLog);
            await _context.SaveChangesAsync();

            // Đẩy job vào Hangfire để xử lý bất đồng bộ
            BackgroundJob.Enqueue(() => ProcessCreateInsuranceContractsJob(requestId, request));

            _logger.LogInformation($"Enqueued job for RequestId: {requestId}");
            return requestId; // Trả về RequestId cho client để theo dõi
        }

        // Phương thức này được Hangfire gọi để xử lý job thực tế
        [AutomaticRetry(Attempts = 3)] // Thử lại 3 lần nếu job thất bại
        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)] // Ngăn chặn nhiều instance của cùng một job chạy đồng thời
        public async Task ProcessCreateInsuranceContractsJob(string requestId, CreateContractRequestDto request)
        {
            // Lấy RequestLog để cập nhật trạng thái
            var requestLog = await _context.RequestLogs.FirstOrDefaultAsync(r => r.RequestId == requestId);
            if (requestLog == null)
            {
                _logger.LogError($"RequestLog with RequestId {requestId} not found for processing.");
                return; // Không tìm thấy log, không thể xử lý
            }

            try
            {
                // Cập nhật trạng thái thành Processing
                requestLog.Status = RequestStatus.Processing;
                requestLog.LastUpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Processing job for RequestId: {requestId}");

                // Logic xử lý chính (tạo hợp đồng)
                var createdContracts = new List<InsuranceContract>();
                foreach (var dto in request.Data)
                {
                    // Validation từng hợp đồng con (nếu cần, đã có trong CreateContractRequestDtoValidator)
                    var validationResult = await _contractValidator.ValidateAsync(dto);
                    if (!validationResult.IsValid)
                    {
                        var errors = new Dictionary<string, string[]>();
                        foreach (var errorGroup in validationResult.Errors.GroupBy(e => e.PropertyName))
                        {
                            errors[errorGroup.Key] = errorGroup.Select(e => e.ErrorMessage).ToArray();
                        }
                        throw new pviBase.Helpers.ValidationException(errors); // Ném lỗi để bắt ở catch
                    }

                    var contract = _mapper.Map<InsuranceContract>(dto);
                    contract.CreatedAt = DateTime.UtcNow;

                    _context.InsuranceContracts.Add(contract);
                    createdContracts.Add(contract);

                    // Vô hiệu hóa cache cho hợp đồng cụ thể này nếu nó tồn tại
                    var cacheKey = $"InsuranceContract:{contract.LoanNo}";
                    await _cache.RemoveAsync(cacheKey);
                    _logger.LogInformation($"Removed cache key: {cacheKey}");
                }

                await _context.SaveChangesAsync(); // Lưu các hợp đồng vào DB

                // Cập nhật trạng thái thành Completed
                requestLog.Status = RequestStatus.Completed;
                requestLog.LastUpdatedAt = DateTime.UtcNow;
                requestLog.ErrorMessage = null; // Xóa lỗi nếu có
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Job for RequestId: {requestId} completed successfully.");
            }
            catch (Exception ex)
            {
                // Cập nhật trạng thái thành Failed nếu có lỗi
                requestLog.Status = RequestStatus.Failed;
                requestLog.LastUpdatedAt = DateTime.UtcNow;
                requestLog.ErrorMessage = ex.Message; // Lưu thông báo lỗi
                await _context.SaveChangesAsync();
                _logger.LogError(ex, $"Job for RequestId: {requestId} failed.");
                // Ném lại ngoại lệ để Hangfire ghi nhận thất bại và thử lại (nếu có AutomaticRetry)
                throw;
            }
        }

        public async Task<InsuranceContract?> GetContractByLoanNo(string loanNo)
        {
            var cacheKey = $"InsuranceContract:{loanNo}";
            string? cachedContractJson = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedContractJson))
            {
                _logger.LogInformation($"Cache hit for LoanNo: {loanNo}");
                return JsonSerializer.Deserialize<InsuranceContract>(cachedContractJson);
            }

            _logger.LogInformation($"Cache miss for LoanNo: {loanNo}, fetching from DB.");
            var contract = await _context.InsuranceContracts.FirstOrDefaultAsync(c => c.LoanNo == loanNo);

            if (contract != null)
            {
                var options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(contract), options);
                _logger.LogInformation($"Cached contract for LoanNo: {loanNo}");
            }

            return contract;
        }

        public async Task<GetContractByLoanNoResponseDataDto?> GetDetailedContractByLoanNo(string loanNo)
        {
            var contract = await _context.InsuranceContracts.FirstOrDefaultAsync(c => c.LoanNo == loanNo);
            if (contract == null)
            {
                return null;
            }
            var detailedDto = _mapper.Map<GetContractByLoanNoResponseDataDto>(contract);
            return detailedDto;
        }

        // THÊM PHƯƠNG THỨC NÀY: Lấy trạng thái của yêu cầu
        public async Task<RequestLog?> GetRequestLogById(string requestId)
        {
            return await _context.RequestLogs.FirstOrDefaultAsync(r => r.RequestId == requestId);
        }
    }
}