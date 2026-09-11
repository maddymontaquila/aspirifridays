using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

internal static class MauiResources
{
    public static void AddBingoMauiClients(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> admin)
    {
        var launchProfile = builder.Configuration["DOTNET_LAUNCH_PROFILE"];
        if (!string.Equals(launchProfile, "maui", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var publicDevTunnel = builder.AddDevTunnel("devtunnel-public")
            .WithAnonymousAccess()
            .WithReference(admin.GetEndpoint("https"));

        var mauiApp = builder.AddMauiProject(
            "mauiapp",
            @"BingoBoard.MauiHybrid/BingoBoard.MauiHybrid.csproj");

        mauiApp.AddiOSSimulator()
            .ExcludeFromManifest()
            .WithOtlpDevTunnel()
            .WithReference(admin, publicDevTunnel);

        mauiApp.AddAndroidEmulator()
            .ExcludeFromManifest()
            .WithParentRelationship(mauiApp)
            .WithOtlpDevTunnel()
            .WithReference(admin, publicDevTunnel);

        mauiApp.AddMacCatalystDevice()
            .ExcludeFromManifest()
            .WithReference(admin);

        mauiApp.AddWindowsDevice()
            .ExcludeFromManifest()
            .WithReference(admin);
    }
}
