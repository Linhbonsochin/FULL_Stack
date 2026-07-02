using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using API.Services;

namespace API.Services
{
    /// <summary>
    /// Background worker that periodically syncs employees from HR Service
    /// Runs every configured interval (default: 5 minutes)
    /// </summary>
    public class EmployeeSyncWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmployeeSyncWorker> _logger;
        private readonly IConfiguration _configuration;

        public EmployeeSyncWorker(
            IServiceProvider serviceProvider,
            ILogger<EmployeeSyncWorker> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Interval in seconds (default 300s = 5min)
            var intervalSeconds = 300;
            var configured = _configuration["EmployeeSync:IntervalSeconds"];
            if (!string.IsNullOrEmpty(configured) && int.TryParse(configured, out var parsed))
            {
                intervalSeconds = Math.Max(60, parsed);
            }

            _logger.LogInformation($"🚀 EmployeeSyncWorker started. Interval: {intervalSeconds}s");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Create scope for each sync to resolve scoped services
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var syncService = scope.ServiceProvider.GetRequiredService<IEmployeeSyncService>();
                        _logger.LogInformation("🔄 [WORKER] Starting scheduled employee sync from HR Service...");
                        var count = await syncService.SyncEmployeesAsync();
                        _logger.LogInformation($"✅ [WORKER] Employee sync completed: {count} employees synced");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"⚠️ [WORKER] Employee sync failed: {ex.Message}");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Shutdown requested
                }
            }

            _logger.LogInformation("EmployeeSyncWorker stopping");
        }
    }
}

