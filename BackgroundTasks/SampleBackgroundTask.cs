// BackgroundTasks/SampleBackgroundTask.cs
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace pviBase.BackgroundTasks
{
    public class SampleBackgroundTask
    {
        private readonly ILogger<SampleBackgroundTask> _logger;

        public SampleBackgroundTask(ILogger<SampleBackgroundTask> logger)
        {
            _logger = logger;
        }

        public void PerformDailyDataCleanup()
        {
            _logger.LogInformation($"Performing daily data cleanup at {DateTime.Now}");
            // Your cleanup logic here, e.g., delete old records
            Task.Delay(1000).Wait(); // Simulate work
            _logger.LogInformation("Daily data cleanup completed.");
        }
    }
}