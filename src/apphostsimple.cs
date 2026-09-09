#:sdk Aspire.AppHost.Sdk@13.3.0
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

#region Usings
using Azure.Core;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.PostgreSql;
#endregion

var builder = DistributedApplication.CreateBuilder(args);

#region Parameters & Environment
var postgresAzureLocation = builder.Configuration["Azure:PostgresLocation"];

builder.AddAzureContainerAppEnvironment("env");

var password = builder.AddParameter("admin-password", secret: true);
var adminDomain = builder.AddParameter("admin-domain", "admin.aspireify.live");
var adminCertName = builder.AddParameter("admin-cert-name", "admin.aspireify.live-envvevso-251017190301");
var yarpDomain = builder.AddParameter("yarp-domain", "aspireify.live");
var yarpCertName = builder.AddParameter("yarp-cert-name", "aspireify.live-envvevso-251017185247");
#endregion

#region Redis Cache
var cache = builder.AddRedis("cache")
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        app.Template.Scale.MaxReplicas = 1;
    });
#endregion

#region Postgres
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

var db = postgres.AddDatabase("db");
#endregion

#region Migrations
var migrations = builder.AddProject<Projects.BingoBoard_MigrationService>("migrations")
    .WithEnvironment("Authentication__AdminPassword", password)
    .WithReference(db)
    .WaitFor(db);
#endregion

#region Admin (Blazor Site + SignalR Hub)
var admin = builder.AddProject<Projects.BingoBoard_Admin>("boardadmin")
    .WithReference(cache)
    .WithReference(db)
    .WithReference(migrations)
    .WaitFor(cache)
    .WaitForCompletion(migrations)
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        app.Template.Scale.MaxReplicas = 1;
        app.Template.Scale.MinReplicas = 1;
        app.ConfigureCustomDomain(adminDomain, adminCertName);
    });
#endregion

#region Frontend (Vite)
var frontend = builder.AddViteApp("bingoboard-dev", "./bingo-board")
    .WithReference(admin)
    .WaitFor(admin);
#endregion

#region YARP Gateway
builder.AddYarp("bingoboard")
    .WithConfiguration(c =>
    {
        c.AddRoute("/api/version-info", admin);
        c.AddRoute("/bingohub/{**catch-all}", admin);
    })
    .PublishWithStaticFiles(frontend)
    .WaitFor(admin)
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        app.Template.Scale.MaxReplicas = 5;
        app.Template.Scale.MinReplicas = 1;
        app.ConfigureCustomDomain(yarpDomain, yarpCertName);
        app.Template.Scale.Rules.Add(new(
            new ContainerAppScaleRule
            {
                Name = "http-scaler",
                Http = new() { Metadata = new() { ["concurrentRequests"] = "100" } }
            }));
    });
#endregion

builder.Build().Run();
