#:sdk Aspire.AppHost.Sdk@9.5.0
#:package Aspire.Hosting.Azure.AppContainers@9.5.0
#:package Aspire.Hosting.Azure.AppService@9.5.0-preview.1.25474.7
#:package Aspire.Hosting.Azure.Redis@9.5.0
#:package Aspire.Hosting.Docker@9.5.0-preview.1.25474.7
#:package Aspire.Hosting.Redis@9.5.0
#:package Aspire.Hosting.NodeJS@9.5.0
#:package Aspire.Hosting.Yarp@9.5.0-preview.1.25474.7
#:package CommunityToolkit.Aspire.Hosting.NodeJS.Extensions@9.7.0
#:project ./BingoBoard.Admin
#pragma warning disable

using Aspire.Hosting.Yarp;
using Azure.Provisioning.AppContainers;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("env");

var password = builder.AddParameter("admin-password", secret: true);

var cache = builder.AddRedis("cache")
    .WithPassword(builder.AddParameter("redis-password", secret: true));

var admin = builder.AddProject<Projects.BingoBoard_Admin>("boardadmin")
    .WithReference(cache)
    .WithEnvironment("Authentication__AdminPassword", password)
    .WaitFor(cache)
    .WithExternalHttpEndpoints()
    .WithReplicas(builder.ExecutionContext.IsRunMode ? 1 : 2)
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.AllowInsecure = true;
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
    });

var bingo = builder.AddViteApp("bingoboard", "./bingo-board")
    .WithNpmPackageInstallation()
    .WithReference(admin)
    .WaitFor(admin)
    .WithExternalHttpEndpoints()
    .WithIconName("SerialPort")
    .PublishAsYarp(y =>
    {
        y.WithStaticFiles();
        y.WithExternalHttpEndpoints();
        y.WithConfiguration(c => c.AddRoute("/bingohub/{**catch-all}", admin));
    });

// during debugging, make sure the container build also works
if (builder.ExecutionContext.IsRunMode)
{
    builder.AddYarp("containerFE")
        .WithConfiguration(c =>
        {
            c.AddRoute("/bingohub/{**catch-all}", admin.GetEndpoint("http"));
        })
        .WithDockerfile("./bingo-board")
        .WithStaticFiles()
        .WaitFor(admin)
        .WithIconName("SerialPort")
        .WithExplicitStart();
}

builder.Build().Run();

static class NodeAppResourceExtensions
{
    /// <summary>
    /// Configures the NodeAppResource to publish as a YARP resource.
    /// </summary>
    /// <param name="builder">The resource builder for YARP.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<NodeAppResource> PublishAsYarp(this IResourceBuilder<NodeAppResource> builder, Action<IResourceBuilder<YarpResource>>? configure = null)
    {
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }
        var nodeResource = builder.Resource;

        builder.ApplicationBuilder.Resources.Remove(nodeResource);

        var yarpBuilder = builder.ApplicationBuilder.AddYarp(nodeResource.Name)
            .WithDockerfile(nodeResource.WorkingDirectory);

        configure?.Invoke(yarpBuilder);

        return builder;
    }
}
