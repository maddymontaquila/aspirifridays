var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.BingoBoard_Squares>("api");

builder.AddViteApp("frontend", "../frontend")
    .WithNpmPackageInstallation()
    .WithReference(api).WaitFor(api);

builder.Build().Run();
