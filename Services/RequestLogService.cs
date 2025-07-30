using pviBase.Data;
using pviBase.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace pviBase.Services
{
    public class RequestLogService
    {
        private readonly ApplicationDbContext _context;

        public RequestLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SaveLogAsync(string requestId, string jsonData, RequestStatus status, string? errorMessage = null)
        {
            var log = new RequestLog
            {
                RequestId = requestId,
                RequestData = jsonData,
                Status = status,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            _context.RequestLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<RequestLog>> GetFailedLogsAsync()
        {
            return await _context.RequestLogs
                .Where(r => r.Status == RequestStatus.Failed || r.Status == RequestStatus.Pending)
                .ToListAsync();
        }

        public async Task UpdateLogStatusAsync(string requestId, RequestStatus status, string? error = null)
        {
            var log = await _context.RequestLogs.FirstOrDefaultAsync(r => r.RequestId == requestId);
            if (log != null)
            {
                log.Status = status;
                log.ErrorMessage = error;
                log.LastUpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
