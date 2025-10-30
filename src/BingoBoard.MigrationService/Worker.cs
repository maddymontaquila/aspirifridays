using BingoBoard.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BingoBoard.MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime,
    IConfiguration configuration)
    : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

    private const string AdminUserName = "admin";
    private const string AdminPasswordConfigurationKey = "Authentication:AdminPassword";

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = s_activitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var userStore = scope.ServiceProvider.GetRequiredService<IUserStore<ApplicationUser>>();

            await RunMigrationAsync(dbContext, cancellationToken);
            await SeedDataAsync(dbContext, userManager, userStore, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private static async Task RunMigrationAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await dbContext.Database.MigrateAsync(cancellationToken);
        });
    }

    private async Task SeedDataAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore,
        CancellationToken cancellationToken)
    {
        if (configuration[AdminPasswordConfigurationKey] is not { Length: > 0 } password)
        {
            throw new ArgumentException($"Admin password is not configured. Please set the '{AdminPasswordConfigurationKey}' environment variable.");
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            // Add the admin user if they don't already exist.
            var adminUser = await userStore.FindByNameAsync(AdminUserName, cancellationToken);
            if (adminUser is null)
            {
                adminUser = new ApplicationUser();
                await userStore.SetUserNameAsync(adminUser, AdminUserName, CancellationToken.None);
                var result = await userManager.CreateAsync(adminUser, password);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException("Unable to create the admin user.");
                }
            }

            await transaction.CommitAsync(cancellationToken);
        });
    }
}
