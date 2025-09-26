using Aspire.Hosting.Yarp;
using Azure.Provisioning.AppContainers;

var builder = DistributedApplication.CreateBuilder(args);

// builder.AddDockerComposeEnvironment("env")
//     .WithDashboard(db => db.WithHostPort(8083));

builder.AddAzureContainerAppEnvironment("env");

var password = builder.AddParameter("admin-password", secret: true);

#pragma warning disable
var cache = builder.AddRedis("cache");

var admin = builder.AddProject<Projects.BingoBoard_Admin>("boardadmin")
    .WithReference(cache)
    .WithEnvironment("Authentication__AdminPassword", password)
    .WaitFor(cache)
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Ingress.AllowInsecure = true;
        app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
    });

var bingo = builder.AddViteApp("bingoboard", "../bingo-board")
    .WithNpmPackageInstallation()
    .WithReference(admin)
    .WaitFor(admin)
    .WithExternalHttpEndpoints()
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
        .WithDockerfile("../bingo-board")
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