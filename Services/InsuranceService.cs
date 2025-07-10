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
using Hangfire;
using Microsoft.AspNetCore.Http;

namespace pviBase.Services
{
    public class InsuranceService : IInsuranceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<InsuranceService> _logger;
        private readonly IValidator<InsuranceContractRequestDto> _contractValidator;
        private readonly IValidator<CreateContractRequestDto> _createContractValidator;
        private readonly IValidator<GetContractByLoanNoRequestDto> _getContractValidator;

        public InsuranceService(
            ApplicationDbContext context,
            IMapper mapper,
            IDistributedCache cache,
            ILogger<InsuranceService> logger,
            IValidator<InsuranceContractRequestDto> contractValidator,
            IValidator<CreateContractRequestDto> createContractValidator,
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

        public async Task<string> EnqueueCreateInsuranceContractsJob(CreateContractRequestDto request)
        {
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

            string requestId = Guid.NewGuid().ToString();

            var requestLog = new RequestLog
            {
                RequestId = requestId,
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                RequestData = JsonSerializer.Serialize(request)
            };
            _context.RequestLogs.Add(requestLog);
            await _context.SaveChangesAsync();

            BackgroundJob.Enqueue(() => ProcessCreateInsuranceContractsJob(requestId, request));

            _logger.LogInformation($"Enqueued job for RequestId: {requestId}");
            return requestId;
        }

        [AutomaticRetry(Attempts = 3)]
        [DisableConcurrentExecution(timeoutInSeconds: 600)]
        public async Task ProcessCreateInsuranceContractsJob(string requestId, CreateContractRequestDto request)
        {
            var requestLog = await _context.RequestLogs.FirstOrDefaultAsync(r => r.RequestId == requestId);
            if (requestLog == null)
            {
                _logger.LogError($"RequestLog with RequestId {requestId} not found for processing.");
                return;
            }

            try
            {
                requestLog.Status = RequestStatus.Processing;
                requestLog.LastUpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Processing job for RequestId: {requestId}");

                _logger.LogInformation($"Tổng số file upload: {(request.AllAttachments == null ? 0 : request.AllAttachments.Count)}");
                if (request.AllAttachments != null)
                {
                    foreach (var f in request.AllAttachments)
                    {
                        _logger.LogInformation($"Tên file upload: {f.FileName}");
                    }
                }

                for (int i = 0; i < request.Data.Count; i++)
                {
                    var dto = request.Data[i];
                    _logger.LogInformation($"Đang xử lý LoanNo: {dto.LoanNo}, attachmentFileName: {dto.AttachmentFileName}");

                    var validationResult = await _contractValidator.ValidateAsync(dto);
                    if (!validationResult.IsValid)
                    {
                        var errors = validationResult.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                        throw new pviBase.Helpers.ValidationException(errors);
                    }

                    var contract = _mapper.Map<InsuranceContract>(dto);
                    contract.CreatedAt = DateTime.UtcNow;

                    if (dto.AttachmentData != null && !string.IsNullOrEmpty(dto.AttachmentFileName))
                    {
                        contract.AttachmentData = dto.AttachmentData;
                        contract.AttachmentFileName = dto.AttachmentFileName;
                        contract.AttachmentContentType = dto.AttachmentContentType;
                        _logger.LogInformation($"Stored file '{dto.AttachmentFileName}' for LoanNo: {dto.LoanNo}");
                    }
                    else
                    {
                        _logger.LogInformation($"No file matched for LoanNo: {dto.LoanNo}");
                    }

                    _context.InsuranceContracts.Add(contract);

                    var cacheKey = $"InsuranceContract:{contract.LoanNo}";
                    await _cache.RemoveAsync(cacheKey);
                }

                await _context.SaveChangesAsync();

                requestLog.Status = RequestStatus.Completed;
                requestLog.LastUpdatedAt = DateTime.UtcNow;
                requestLog.ErrorMessage = null;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Job for RequestId: {requestId} completed successfully.");
            }
            catch (Exception ex)
            {
                requestLog.Status = RequestStatus.Failed;
                requestLog.LastUpdatedAt = DateTime.UtcNow;
                requestLog.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync();
                _logger.LogError(ex, $"Job for RequestId: {requestId} failed.");
                throw;
            }
        }

        public async Task<InsuranceContract?> GetContractByLoanNo(string loanNo)
        {
            var cacheKey = $"InsuranceContract:{loanNo}";
            var cachedJson = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                _logger.LogInformation($"Cache hit for LoanNo: {loanNo}");
                return JsonSerializer.Deserialize<InsuranceContract>(cachedJson);
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
            return _mapper.Map<GetContractByLoanNoResponseDataDto>(contract);
        }

        public async Task<RequestLog?> GetRequestLogById(string requestId)
        {
            return await _context.RequestLogs.FirstOrDefaultAsync(r => r.RequestId == requestId);
        }
    }
}
