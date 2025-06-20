using BingoBoard.Admin.Services;

namespace BingoBoard.Admin.Services
{
    /// <summary>
    /// Background service to clean up expired approval requests
    /// </summary>
    public class ApprovalCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ApprovalCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(30); // Run every 30 minutes

        public ApprovalCleanupService(IServiceProvider serviceProvider, ILogger<ApprovalCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var bingoService = scope.ServiceProvider.GetRequiredService<IBingoService>();
                    
                    await bingoService.CleanupExpiredApprovalsAsync();
                    _logger.LogInformation("Completed approval cleanup at {Timestamp}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during approval cleanup");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }
    }
}
