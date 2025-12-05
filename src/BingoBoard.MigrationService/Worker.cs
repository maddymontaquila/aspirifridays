using BingoBoard.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BingoBoard.MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime,
    IConfiguration configuration,
    ILogger<Worker> logger)
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

    private async Task RunMigrationAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            logger.LogInformation("Migrating the database...");
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
                logger.LogInformation("Creating the admin user...");

                adminUser = new ApplicationUser();
                await userStore.SetUserNameAsync(adminUser, AdminUserName, CancellationToken.None);
                var result = await userManager.CreateAsync(adminUser);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException("Unable to create the admin user.");
                }

                logger.LogInformation("Admin user created!");
            }

            var passwordStore = (IUserPasswordStore<ApplicationUser>)userStore;
            await UpdatePasswordHashAsync(userManager, passwordStore, adminUser, password, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    private async Task UpdatePasswordHashAsync(
        UserManager<ApplicationUser> userManager,
        IUserPasswordStore<ApplicationUser> passwordStore,
        ApplicationUser user,
        string newPassword,
        CancellationToken cancellationToken)
    {
        var passwordHasher = userManager.PasswordHasher;
        var oldHash = await passwordStore.GetPasswordHashAsync(user, cancellationToken);
        if (oldHash is not null && passwordHasher.VerifyHashedPassword(user, oldHash, newPassword) is not PasswordVerificationResult.Failed)
        {
            // The new password matches the previous password.
            logger.LogInformation("The configured password matches the current password.");
            return;
        }

        // The password doesn't exist yet or has changed - update it.
        logger.LogInformation("The configured password has changed. Updating the password...");

        var newHash = passwordHasher.HashPassword(user, newPassword);
        await passwordStore.SetPasswordHashAsync(user, newHash, cancellationToken);
        await userManager.UpdateSecurityStampAsync(user);

        logger.LogInformation("Password updated!");
    }
}
