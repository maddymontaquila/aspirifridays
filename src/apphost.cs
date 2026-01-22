#pragma warning disable
#:sdk Aspire.AppHost.Sdk@13.2.0-preview.1.26072.3
#:package Aspire.Hosting.Azure.AppContainers@13.2.0-preview.1.26072.3
#:package Aspire.Hosting.Azure.Redis@13.2.0-preview.1.26072.3
#:package Aspire.Hosting.Docker@13.2.0-preview.1.26072.3
#:package Aspire.Hosting.Redis@13.2.0-preview.1.26072.3
#:package Aspire.Hosting.JavaScript@13.2.0-preview.1.26072.3
#:package Aspire.Hosting.Yarp@13.2.0-preview.1.26072.3
#:project ./BingoBoard.Admin
#:property UserSecretsId=aspire-samples-bingoboard

using System.Reflection;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;

// Get version info programmatically
var aspireVersion = typeof(IDistributedApplicationBuilder).Assembly.GetName().Version?.ToString(3) ?? "unknown";
var dotnetVersion = Environment.Version.Major.ToString();
var commitSha = Environment.GetEnvironmentVariable("COMMIT_SHA") ?? "dev";

var builder = DistributedApplication.CreateBuilder(args);

Console.WriteLine($"Environment name: {builder.Environment.EnvironmentName}");

builder.AddAzureContainerAppEnvironment("env");

var password = builder.AddParameter("admin-password", secret: true);
var cache = builder.AddRedis("cache")
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        app.Template.Scale.MaxReplicas = 1;
    }); ;

var admin = builder.AddProject<Projects.BingoBoard_Admin>("boardadmin")
    .WithReference(cache)
    .WaitFor(cache)
    .WithEnvironment("Authentication__AdminPassword", password)
    .WithEnvironment("COMMIT_SHA", commitSha)
    .WithEnvironment("ASPIRE_VERSION", aspireVersion)
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        app.Template.Scale.MaxReplicas = 1;
        app.Template.Scale.MinReplicas = 1;
        app.ConfigureCustomDomain(
            builder.AddParameter("admin-domain", "admin.aspireify.live"),
            builder.AddParameter("admin-cert-name", "admin.aspireify.live-envvevso-251017190301")
        );
    });

if (builder.ExecutionContext.IsRunMode)
{
    builder.AddViteApp("bingoboard-dev", "./bingo-board")
        .WithReference(admin)
        .WaitFor(admin)
        .WithIconName("SerialPort");
}

var adminEndpoint = admin.GetEndpoint(builder.ExecutionContext.IsRunMode ? "http" : "https");
builder.AddYarp("bingoboard")
    .WithConfiguration(c =>
    {
        c.AddRoute("/bingohub/{**catch-all}", adminEndpoint);
    })
    .WithDockerfile("./bingo-board")
    .WithBuildArg("VITE_COMMIT_SHA", commitSha)
    .WithBuildArg("VITE_DOTNET_VERSION", dotnetVersion)
    .WithBuildArg("VITE_ASPIRE_VERSION", aspireVersion)
    .WithStaticFiles()
    .WaitFor(admin)
    .WithIconName("SerialPort")
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        app.Template.Scale.MaxReplicas = 5;
        app.Template.Scale.MinReplicas = 1;
        app.ConfigureCustomDomain(
            builder.AddParameter("yarp-domain", "aspireify.live"),
            builder.AddParameter("yarp-cert-name", "aspireify.live-envvevso-251017185247")
        );
        app.Template.Scale.Rules.Add(new (
            new ContainerAppScaleRule
            {
                Name = "http-scaler",
                Http = new() { Metadata = new() { ["concurrentRequests"] = "100" } }
            }));
    })
    .WithExplicitStart();

builder.Build().Run();
