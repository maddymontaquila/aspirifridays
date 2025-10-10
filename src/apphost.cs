#:sdk Aspire.AppHost.Sdk@9.5.1
#:package Aspire.Hosting.Azure.AppContainers@9.5.1
#:package Aspire.Hosting.Azure.Redis@9.5.1
#:package Aspire.Hosting.Docker@9.5.1-preview.1.25502.11
#:package Aspire.Hosting.Redis@9.5.1
#:package Aspire.Hosting.NodeJS@9.5.1
#:package Aspire.Hosting.Yarp@9.5.1-preview.1.25502.11
#:package CommunityToolkit.Aspire.Hosting.NodeJS.Extensions@9.7.0
#:project ./BingoBoard.Admin
#:property UserSecretsId=aspire-samples-bingoboard

using Azure.Provisioning.AppContainers;

var builder = DistributedApplication.CreateBuilder(args);

Console.WriteLine($"Environment name: {builder.Environment.EnvironmentName}");

builder.AddAzureContainerAppEnvironment("env");

var password = builder.AddParameter("admin-password", secret: true);
var cache = builder.AddRedis("cache");

var admin = builder.AddProject<Projects.BingoBoard_Admin>("boardadmin")
    .WithReference(cache)
    .WaitFor(cache)
    .WithEnvironment("Authentication__AdminPassword", password)
    .WithExternalHttpEndpoints()
    .WithReplicas(builder.ExecutionContext.IsRunMode ? 1 : 2)
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
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
    .WithExplicitStart();

builder.Build().Run();
