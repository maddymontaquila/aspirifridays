var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("admin-password", secret: true);

// Add a development mode parameter to disable authentication
var developmentMode = builder.AddParameter("development-mode")
    .WithDescription("Set to 'true' to disable authentication for development (default: true in Development environment)");

var cache = builder.AddRedis("cache");

var admin = builder.AddProject<Projects.BingoBoard_Admin>("boardadmin")
    .WithReference(cache)
    .WithEnvironment("Development__DisableAuthentication", developmentMode)
    .WithEnvironment("Authentication__AdminPassword", password)
    .WaitFor(cache);

var bingo = builder.AddViteApp("bingoboard", "../bingo-board")
    .WithNpmPackageInstallation()
    .WithEnvironment("PORT", "5173")
    .WithReference(admin)
    .WithEnvironment("VITE_ADMIN_URL", admin.GetEndpoint("https"))
    .WaitFor(admin);

admin.WithReference(bingo);

builder.Build().Run();
