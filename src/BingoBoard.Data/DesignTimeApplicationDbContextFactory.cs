using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BingoBoard.Data;

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
