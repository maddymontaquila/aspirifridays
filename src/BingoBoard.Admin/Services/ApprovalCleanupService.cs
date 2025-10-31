using BingoBoard.Admin.Services;

namespace BingoBoard.Admin.Services;

/// <summary>
/// Background service to clean up expired approval requests
/// </summary>
public class ApprovalCleanupService(IServiceProvider serviceProvider, ILogger<ApprovalCleanupService> logger) : BackgroundService
{
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(30); // Run every 30 minutes

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var bingoService = scope.ServiceProvider.GetRequiredService<IBingoService>();
                
                await bingoService.CleanupExpiredApprovalsAsync(stoppingToken);
                logger.LogInformation("Completed approval cleanup at {Timestamp}", DateTime.UtcNow);
            }
            catch (OperationCanceledException)
            {
                // Expected when the service is stopping
                logger.LogInformation("Approval cleanup service is stopping");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during approval cleanup");
            }

            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when the service is stopping
                logger.LogInformation("Approval cleanup service delay was cancelled");
                break;
            }
        }
    }
}
