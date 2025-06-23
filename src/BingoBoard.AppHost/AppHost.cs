var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var admin = builder.AddProject<Projects.BingoBoard_Admin>("boardadmin")
    .WithReference(cache)
    .WaitFor(cache);

var bingo = builder.AddViteApp("bingoboard", "../bingo-board", "dev")
    .WithNpmPackageInstallation()
    .WithReference(admin)
    .WaitFor(admin);

builder.Build().Run();
