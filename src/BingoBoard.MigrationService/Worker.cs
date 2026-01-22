using BingoBoard.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

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
            await SeedBingoSquaresAsync(dbContext, cancellationToken);
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

    private async Task SeedBingoSquaresAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        // Only seed if the table is empty
        if (await dbContext.BingoSquares.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Bingo squares already seeded, skipping...");
            return;
        }

        logger.LogInformation("Seeding bingo squares...");

        var defaultSquares = GetDefaultBingoSquares();
        var order = 0;

        foreach (var square in defaultSquares)
        {
            dbContext.BingoSquares.Add(new BingoSquareEntity
            {
                Id = square.Id,
                Label = square.Label,
                Type = square.Type,
                IsActive = true,
                DisplayOrder = order++,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} bingo squares", defaultSquares.Count);
    }

    private static List<BingoSquareData> GetDefaultBingoSquares() =>
    [
        new("free", "Free Space", "free"),
        new("council-of-aspirations", "\"Council of Aspirations\"", "quote"),
        new("screen-share-fail", "Screen share fail", "oops"),
        new("pine-mentioned", "David Pine 🌲 mentioned", "quote"),
        new("multiple-options", "\"Well, you can do this a few ways...\"", "quote"),
        new("app-bug", "Bug found in guest's app", "bug"),
        new("scared", "Someone is scared to try something", "dev"),
        new("damian-fowler-bicker", "Damian and Fowler bicker", "dev"),
        new("friday-behavior", "\"Friday Behavior\"", "quote"),
        new("ignore-docs", "Someone ignores the docs", "oops"),
        new("damian-tbc", "Damian says \"To be clear/To be specific\"", "quote"),
        new("different-opinions", "Disagreement on how to do something", "dev"),
        new("error-celly", "Excited to see an error", "dev"),
        new("av-issue", "AV/stream issues", "oops"),
        new("new-bug", "Found a new bug in Aspire", "bug"),
        new("old-bug", "Hit a bug we've already filed", "bug"),
        new("maddy-swears", "Maddy accidentally swears", "quote"),
        new("bathroom-break", "Bathroom break", "meta"),
        new("this-wont-work", "\"There's no way this works, right?\"", "quote"),
        new("did-that-work", "\"Wait, did that work?!\"", "quote"),
        new("aspire-pun", "Aspire pun made", "meta"),
        new("fowler-pause", "Fowler says \"PAUSE\" or \"WAIT\"", "quote"),
        new("restart-something", "Restarted editor/IDE", "oops"),
        new("do-it-live", "\"Let's do it live\"", "quote"),
        new("refactoring", "Impromptu refactoring", "dev"),
        new("port-problems", "Ports being difficult", "oops"),
        new("fowler-llm", "Fowler 💞 AI", "meta"),
        new("vibe-coding", "Vibe coding mentioned", "quote"),
        new("bad-ai", "AI autocomplete being annoying", "dev"),
        new("live-share", "Accidentally kills live share", "oops"),
        new("frustration", "Visible frustration", "dev"),
        new("coffee-mention", "Coffee mentioned", "meta"),
        new("github-issues", "GitHub issues discussion", "dev"),
        new("demo-gods", "\"Demo gods\" mentioned", "quote"),
        new("fowler-monorepo", "Fowler advocates monorepo", "dev"),
        new("private-key-shared", "Someone shares a private key", "oops"),
        new("one-line-add", "\"It's one line, so let's add it\"", "quote"),
        new("one-day-work", "\"One day, that'll work\"", "quote"),
        new("maddy-snack", "Maddy eats a snack live", "meta"),
        new("amazing", "Safia fixes a bug in 2 seconds", "meta")
    ];

    private record BingoSquareData(string Id, string Label, string? Type);
}
