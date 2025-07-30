using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;
using pviBase.Services;
using pviBase.Models;

namespace pviBase.BackgroundTasks
{
    public class RetryFailedRequests : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RetryFailedRequests> _logger;

        public RetryFailedRequests(IServiceScopeFactory scopeFactory, ILogger<RetryFailedRequests> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var logService = scope.ServiceProvider.GetRequiredService<RequestLogService>();
                    var pviService = scope.ServiceProvider.GetRequiredService<PviApiForwardService>();

                    var failedRequests = await logService.GetFailedLogsAsync();

                    foreach (var req in failedRequests)
                    {
                        try
                        {
                            _logger.LogInformation($"[Retry] Forward lại request: {req.RequestId}");
                            await pviService.ForwardRawRequestToPviApi(req.RequestData, Array.Empty<byte>(), "", "");
                            await logService.UpdateLogStatusAsync(req.RequestId, RequestStatus.Completed);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"[Retry] Lỗi khi retry: {ex.Message}");
                            await logService.UpdateLogStatusAsync(req.RequestId, RequestStatus.Failed, ex.Message);
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Retry mỗi 1 phút
            }
        }
    }
}
