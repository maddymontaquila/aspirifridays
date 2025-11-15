#:sdk Aspire.AppHost.Sdk@13.0.0
#:package Aspire.Hosting.Azure.AppContainers
#:package Aspire.Hosting.Azure.Redis
#:package Aspire.Hosting.Docker
#:package Aspire.Hosting.Redis
#:package Aspire.Hosting.SqlServer
#:package Aspire.Hosting.JavaScript
#:package Aspire.Hosting.Yarp
#:package Aspire.Hosting.Maui
#:package Aspire.Hosting.DevTunnels
#:project ./BingoBoard.Admin
#:project ./BingoBoard.MigrationService
#:property UserSecretsId=aspire-samples-bingoboard

using Azure.Provisioning;
using Azure.Provisioning.AppContainers;

var builder = DistributedApplication.CreateBuilder(args);

Console.WriteLine($"Environment name: {builder.Environment.EnvironmentName}");

builder.AddAzureContainerAppEnvironment("env");

var password = builder.AddParameter("admin-password", secret: true);

var cache = builder.AddRedis("cache")
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        app.Template.Scale.MaxReplicas = 1;
    });

var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var db = sql.AddDatabase("db");

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
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        app.Template.Scale.MaxReplicas = 1; 
        app.Template.Scale.MinReplicas = 1;
        // app.ConfigureCustomDomain(
        //     builder.AddParameter("admin-domain", "admin.aspireify.live"),
        //     builder.AddParameter("admin-cert-name", "admin.aspireify.live-envvevso-251017190301")
        // );
    })
    .WithUrlForEndpoint("https", u => u.DisplayText = "Admin UI (https)")
    .WithUrlForEndpoint("http", u => u.DisplayText = "Admin UI (http)")
    .WithUrlForEndpoint("https", e => new ResourceUrlAnnotation() { Url = "/scalar", DisplayText = "OpenAPI Docs" });


builder.AddViteApp("bingoboard-dev", "./bingo-board", "aspire")
    .WithNpm()
    .ExcludeFromManifest()
    .WithReference(admin)
    .WaitFor(admin)
    .WithIconName("SerialPort");


var adminEndpoint = admin.GetEndpoint(builder.ExecutionContext.IsRunMode ? "http" : "https");
builder.AddYarp("bingoboard")
    .WithConfiguration(c =>
    {
        c.AddRoute("/bingohub/{**catch-all}", adminEndpoint);
    })
    .WithDockerfile("./bingo-board")
    .WithStaticFiles()
    .WaitFor(admin)
    .WithIconName("SerialPort")
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        app.Template.Scale.MaxReplicas = 5;
        app.Template.Scale.MinReplicas = 1;
        // app.ConfigureCustomDomain(
        //     builder.AddParameter("yarp-domain", "aspireify.live"),
        //     builder.AddParameter("yarp-cert-name", "aspireify.live-envvevso-251017185247")
        // );
        app.Template.Scale.Rules.Add(new(
            new ContainerAppScaleRule
            {
                Name = "http-scaler",
                Http = new() { Metadata = new() { ["concurrentRequests"] = "100" } }
            }));
    })
    .WithExplicitStart();


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

builder.Build().Run();
