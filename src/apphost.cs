#:sdk Aspire.AppHost.Sdk@13.2.1
#:package Aspire.Hosting.Azure.AppContainers
#:package Aspire.Hosting.Azure.PostgreSQL
#:package Aspire.Hosting.Azure.Redis
#:package Aspire.Hosting.Docker
#:package Aspire.Hosting.Redis
#:package Aspire.Hosting.JavaScript
#:package Aspire.Hosting.Yarp
#:package Aspire.Hosting.Maui
#:package Aspire.Hosting.DevTunnels
#:project ./BingoBoard.Admin
#:project ./BingoBoard.MigrationService

#pragma warning disable

using System.Reflection;
using System.Text.Json;
using Azure.Core;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.PostgreSql;
using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var aspireAssembly = typeof(IDistributedApplicationBuilder).Assembly;
var aspireVersion = Environment.GetEnvironmentVariable("ASPIRE_VERSION")
    ?? aspireAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0] 
    ?? aspireAssembly.GetName().Version?.ToString(3) 
    ?? "unknown";
var dotnetVersion = Environment.GetEnvironmentVariable("DOTNET_VERSION")
    ?? Environment.Version.ToString();
var commitSha = Environment.GetEnvironmentVariable("COMMIT_SHA") ?? "dev";
var viteVersion = GetViteVersion(Path.Combine(builder.Environment.ContentRootPath, "bingo-board", "package.json"));
var postgresAzureLocation = builder.Configuration["Azure:PostgresLocation"];

Console.WriteLine($"Environment name: {builder.Environment.EnvironmentName}");

builder.AddAzureContainerAppEnvironment("env");

var password = builder.AddParameter("admin-password", secret: true);
var adminDomain = builder.AddParameter("admin-domain", "admin.aspireify.live");
var adminCertName = builder.AddParameter("admin-cert-name", "admin.aspireify.live-envvevso-251017190301");
var yarpDomain = builder.AddParameter("yarp-domain", "aspireify.live");
var yarpCertName = builder.AddParameter("yarp-cert-name", "aspireify.live-envvevso-251017185247");

var cache = builder.AddRedis("cache")
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        app.Template.Scale.MaxReplicas = 1;
    });

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .ConfigureInfrastructure(infra =>
    {
        const int minimumBackupRetentionDays = 7;
        const int minimumStorageSizeInGb = 32;

        var flexibleServer = infra.GetProvisionableResources()
            .OfType<PostgreSqlFlexibleServer>()
            .Single();

        if (!string.IsNullOrWhiteSpace(postgresAzureLocation))
        {
            flexibleServer.Location = new AzureLocation(postgresAzureLocation);
        }

        flexibleServer.Sku = new PostgreSqlFlexibleServerSku
        {
            Name = "Standard_B1ms",
            Tier = PostgreSqlFlexibleServerSkuTier.Burstable
        };

        flexibleServer.Backup = new PostgreSqlFlexibleServerBackupProperties
        {
            BackupRetentionDays = minimumBackupRetentionDays,
            GeoRedundantBackup = PostgreSqlFlexibleServerGeoRedundantBackupEnum.Disabled
        };

        flexibleServer.HighAvailability = new PostgreSqlFlexibleServerHighAvailability
        {
            Mode = PostgreSqlFlexibleServerHighAvailabilityMode.Disabled
        };

        flexibleServer.Storage = new PostgreSqlFlexibleServerStorage
        {
            StorageSizeInGB = minimumStorageSizeInGb,
            AutoGrow = StorageAutoGrow.Disabled
        };
    })
    .WithPasswordAuthentication()
    .RunAsContainer(container => container.WithLifetime(ContainerLifetime.Persistent));

var db = postgres.AddDatabase("db")
    .WithPostgresMcp();

var migrations = builder.AddProject<Projects.BingoBoard_MigrationService>("migrations")
    .WithEnvironment("Authentication__AdminPassword", password)
    .WithReference(db)
    .WaitFor(db);


var admin = builder.AddProject<Projects.BingoBoard_Admin>("boardadmin")
    .WithReference(cache)
    .WithReference(db)
    .WithReference(migrations)
    .WaitFor(cache)
    .WaitForCompletion(migrations)
    .WithEnvironment("COMMIT_SHA", commitSha)
    .WithEnvironment("DOTNET_VERSION", dotnetVersion)
    .WithEnvironment("ASPIRE_VERSION", aspireVersion)
    .WithEnvironment("VITE_VERSION", viteVersion)
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        app.Template.Scale.MaxReplicas = 1; 
        app.Template.Scale.MinReplicas = 1;
        app.ConfigureCustomDomain(adminDomain, adminCertName);
    });


var frontend = builder.AddViteApp("bingoboard-dev", "./bingo-board")
    .WithEnvironment("VITE_COMMIT_SHA", commitSha)
    .WithEnvironment("VITE_DOTNET_VERSION", dotnetVersion)
    .WithEnvironment("VITE_ASPIRE_VERSION", aspireVersion)
    .WithEnvironment("VITE_VERSION", viteVersion)
    .WithReference(admin)
    .WaitFor(admin);

builder.AddYarp("bingoboard")
    .WithConfiguration(c =>
    {
        c.AddRoute("/api/version-info", admin);
        c.AddRoute("/bingohub/{**catch-all}", admin);
    })
    .PublishWithStaticFiles(frontend)
    .WaitFor(admin)
    .WithIconName("SerialPort")
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        app.Template.Scale.MaxReplicas = 5;
        app.Template.Scale.MinReplicas = 1;
        app.ConfigureCustomDomain(yarpDomain, yarpCertName);
        app.Template.Scale.Rules.Add(new (
            new ContainerAppScaleRule
            {
                Name = "http-scaler",
                Http = new() { Metadata = new() { ["concurrentRequests"] = "100" } }
            }));
    })
    .WithExplicitStart();

var launchProfile = builder.Configuration["DOTNET_LAUNCH_PROFILE"];
if (!string.IsNullOrWhiteSpace(launchProfile) && 
    string.Equals(launchProfile, "maui", StringComparison.OrdinalIgnoreCase))
{
    var publicDevTunnel = builder.AddDevTunnel("devtunnel-public")
        .WithAnonymousAccess() // All ports on this tunnel default to allowing anonymous access
        .WithReference(admin.GetEndpoint("https"));


    var mauiapp = builder.AddMauiProject("mauiapp", @"BingoBoard.MauiHybrid/BingoBoard.MauiHybrid.csproj");

    // Add iOS simulator with default simulator (uses running or default simulator)
    var ios = mauiapp.AddiOSSimulator()
        .ExcludeFromManifest()
        .WithOtlpDevTunnel() // Needed to get the OpenTelemetry data to "localhost"
        .WithReference(admin, publicDevTunnel); // Needs a dev tunnel to reach "localhost"

    // Add Android emulator with default emulator (uses running or default emulator)
    mauiapp.AddAndroidEmulator()
        .ExcludeFromManifest()
        .WithParentRelationship(mauiapp)
        .WithOtlpDevTunnel() // Needed to get the OpenTelemetry data to "localhost"
        .WithReference(admin, publicDevTunnel); // Needs a dev tunnel to reach "localhost"

    // Add Mac Catalyst desktop
    mauiapp.AddMacCatalystDevice()
        .ExcludeFromManifest()
        .WithReference(admin);

    // Add Windows desktop
    mauiapp.AddWindowsDevice()
        .ExcludeFromManifest()
        .WithReference(admin);
}

builder.Build().Run();

static string GetViteVersion(string packageJsonPath)
{
    if (!File.Exists(packageJsonPath))
    {
        return "dev";
    }

    using var stream = File.OpenRead(packageJsonPath);
    using var document = JsonDocument.Parse(stream);
    if (!document.RootElement.TryGetProperty("devDependencies", out var devDependencies)
        || !devDependencies.TryGetProperty("vite", out var viteVersionProperty))
    {
        return "dev";
    }

    return viteVersionProperty.GetString()?.TrimStart('^', '~') ?? "dev";
}
