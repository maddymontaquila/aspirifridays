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
    });

var bingo = builder.AddViteApp("bingoboard", "../bingo-board")
    .WithNpmPackageInstallation()
    .WithReference(admin)
    .WaitFor(admin)
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile(c => 
    {
        c.WithEndpoint("http", e => e.TargetPort = 80);
    });

// during debugging, make sure the container build also works
if (builder.ExecutionContext.IsRunMode)
{
    builder.AddDockerfile("containerFE", "../bingo-board/")
    .WithReference(admin)
    .WaitFor(admin)
    .WithHttpEndpoint(targetPort: 80)
    .WithExplicitStart();
}

builder.Build().Run();