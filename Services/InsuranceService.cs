// Services/InsuranceService.cs
using AutoMapper;
using pviBase.Data;
using pviBase.Dtos;
using pviBase.Helpers; // Đảm bảo bạn đã có using này
using pviBase.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FluentValidation; // Vẫn giữ using này cho các Validator khác
using System.Linq; // Thêm using này cho .ToArray() và .GroupBy()

namespace pviBase.Services // Đảm bảo namespace này khớp
{
    public class InsuranceService : IInsuranceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<InsuranceService> _logger;
        private readonly IValidator<InsuranceContractRequestDto> _validator;

        public InsuranceService(
            ApplicationDbContext context,
            IMapper mapper,
            IDistributedCache cache,
            ILogger<InsuranceService> logger,
            IValidator<InsuranceContractRequestDto> validator)
        {
            _context = context;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
            _validator = validator;
        }

        public async Task<List<InsuranceContract>> CreateInsuranceContracts(List<InsuranceContractRequestDto> contractDtos)
        {
            var createdContracts = new List<InsuranceContract>();

            foreach (var dto in contractDtos)
            {
                var validationResult = await _validator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    var errors = new Dictionary<string, string[]>();
                    foreach (var errorGroup in validationResult.Errors.GroupBy(e => e.PropertyName))
                    {
                        errors[errorGroup.Key] = errorGroup.Select(e => e.ErrorMessage).ToArray();
                    }
                    // Sửa lỗi ở đây: Chỉ rõ namespace cho ValidationException
                    throw new pviBase.Helpers.ValidationException(errors); // <-- Dòng đã sửa
                }

                var contract = _mapper.Map<InsuranceContract>(dto);
                contract.CreatedAt = DateTime.UtcNow;

                _context.InsuranceContracts.Add(contract);
                createdContracts.Add(contract);

                var cacheKey = $"InsuranceContract:{contract.LoanNo}";
                await _cache.RemoveAsync(cacheKey);
                _logger.LogInformation($"Removed cache key: {cacheKey}");
            }

            await _context.SaveChangesAsync();
            return createdContracts;
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
            // Lấy hợp đồng từ DB (hoặc từ cache nếu bạn muốn cache chi tiết)
            var contract = await _context.InsuranceContracts.FirstOrDefaultAsync(c => c.LoanNo == loanNo);

            if (contract == null)
            {
                return null;
            }

            // Ánh xạ Model sang DTO phản hồi chi tiết
            var detailedDto = _mapper.Map<GetContractByLoanNoResponseDataDto>(contract);

            return detailedDto;
        }
    }
}