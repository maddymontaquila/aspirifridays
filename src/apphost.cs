#:sdk Aspire.AppHost.Sdk@13.0.0-preview.1.25517.3
#:package Aspire.Hosting.Azure.AppContainers@13.0.0-preview.1.25517.3
#:package Aspire.Hosting.Azure.Redis@13.0.0-preview.1.25517.3
#:package Aspire.Hosting.Docker@13.0.0-preview.1.25517.3
#:package Aspire.Hosting.Redis@13.0.0-preview.1.25517.3
#:package Aspire.Hosting.NodeJs@13.0.0-preview.1.25517.3
#:package Aspire.Hosting.Yarp@13.0.0-preview.1.25517.3
#:package CommunityToolkit.Aspire.Hosting.NodeJS.Extensions@9.7.0
#:project ./BingoBoard.Admin
#:property UserSecretsId=aspire-samples-bingoboard

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
    });;

var admin = builder.AddProject<Projects.BingoBoard_Admin>("boardadmin")
    .WithReference(cache)
    .WaitFor(cache)
    .WithEnvironment("Authentication__AdminPassword", password)
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        app.Template.Scale.MaxReplicas = 1;
        app.Template.Scale.MinReplicas = 1;
    });

if (builder.ExecutionContext.IsRunMode)
{
    builder.AddViteApp("bingoboard-dev", "./bingo-board")
        .WithNpmPackageInstallation()
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
    .WithStaticFiles()
    .WaitFor(admin)
    .WithIconName("SerialPort")
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
        app.Template.Scale.MaxReplicas = 5;
        app.Template.Scale.MinReplicas = 1;
    })
    .WithExplicitStart();

builder.Build().Run();
