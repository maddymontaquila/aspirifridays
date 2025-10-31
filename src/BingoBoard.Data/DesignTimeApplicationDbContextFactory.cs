using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BingoBoard.Data;

/// <summary>
/// Constructs <see cref="ApplicationDbContext"/> instances during design time.
/// </summary>
/// <remarks>
/// This class gets automatically discovered while running migrations.
/// When running the app normally, the connection string gets supplied by the Aspire app host,
/// but the app host does not run during migrations. This class uses a temporary connection
/// string so database migration can succeed.
/// </remarks>
public class DesignTimeApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string DesignTimeDbConnectionString = "Server=(localdb)\\mssqllocaldb;Database=TicketDb;Trusted_Connection=true";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.AddDefaultIdentity();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder
            .UseSqlServer(DesignTimeDbConnectionString)
            .UseApplicationServiceProvider(services.BuildServiceProvider());

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
