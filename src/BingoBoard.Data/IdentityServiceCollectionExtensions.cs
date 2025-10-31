using BingoBoard.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class IdentityServiceCollectionExtensions
{
    /// <summary>
    /// Adds the default Identity configuration for the application.
    /// </summary>
    /// <remarks>
    /// This helper exists so all services use the same DB configuration.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    public static IdentityBuilder AddDefaultIdentity(this IServiceCollection services)
    {
        return services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;

                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 5;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>();
    }
}
