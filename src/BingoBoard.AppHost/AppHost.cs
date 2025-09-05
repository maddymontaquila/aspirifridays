var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("admin-password", secret: true);

var cache = builder.AddRedis("cache");

var admin = builder.AddProject<Projects.BingoBoard_Admin>("boardadmin")
    .WithReference(cache)
    .WithEnvironment("Authentication__AdminPassword", password)
    .WaitFor(cache);

var bingo = builder.AddViteApp("bingoboard", "../bingo-board")
    .WithNpmPackageInstallation()
    .WithReference(admin)
    .WaitFor(admin)
    .PublishAsDockerFile();

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