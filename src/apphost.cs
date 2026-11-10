#!/usr/bin/env dotnet
#:sdk Aspire.AppHost.Sdk@13.6.4
#:package Aspire.Hosting.Azure.AppContainers@13.6.4
#:package Aspire.Hosting.Azure.PostgreSQL@13.6.4
#:package Aspire.Hosting.Azure.Redis@13.6.4
#:package Aspire.Hosting.Docker@13.6.4
#:package Aspire.Hosting.Redis@13.6.4
#:package Aspire.Hosting.JavaScript@13.6.4
#:package Aspire.Hosting.Yarp@13.6.4
#:package Aspire.Hosting.Maui@13.6.4-preview.1.26523.11
#:package Aspire.Hosting.DevTunnels@13.6.4
#:package Aspire.Hosting.Browsers@13.6.4-preview.1.26523.11
#:package Aspire.Hosting.EntityFrameworkCore@13.6.4-preview.1.26523.11
#:project ./BingoBoard.Admin
#:project ./BingoBoard.MigrationService
#:include ./apphost/*.cs

using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var buildInfo = BuildInfo.Load(builder);
var postgresAzureLocation = builder.Configuration["Azure:PostgresLocation"];

Console.WriteLine($"Environment name: {builder.Environment.EnvironmentName}");

builder.AddAzureContainerAppEnvironment("env");

var password = builder.AddParameter("admin-password", secret: true);
var adminDomain = builder.AddParameter("admin-domain", "admin.aspireify.live");
var adminCertName = builder.AddParameter("admin-cert-name", "admin.aspireify.live-envvevso-251017190301");
var yarpDomain = builder.AddParameter("yarp-domain", "aspireify.live");
var yarpCertName = builder.AddParameter("yarp-cert-name", "aspireify.live-envvevso-251017185247");

var cache = builder.AddRedis("cache")
    .PublishAsBingoCache();

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .ConfigureBingoPostgres(postgresAzureLocation)
    .WithPasswordAuthentication()
    .RunAsContainer(container => container.WithLifetime(ContainerLifetime.Persistent));

var db = postgres.AddDatabase("db")
    .WithPostgresMcp();

var migrations = builder.AddProject<Projects.BingoBoard_MigrationService>("migrations")
    .WithEnvironment("Authentication__AdminPassword", password)
    .WithReference(db)
    .WaitFor(db);


var admin = builder.AddProject<Projects.BingoBoard_Admin>("boardadmin")
    .WithReference(cache)
    .WithReference(db)
    .WithReference(migrations)
    .WaitFor(cache)
    .WaitForCompletion(migrations)
    .WithBuildInfo(buildInfo)
    .WithExternalHttpEndpoints()
    .WithImportSquaresCommand()
    .PublishAsBingoAdmin(adminDomain, adminCertName);

admin.AddEFMigrations("ef-migrations", "BingoBoard.Data.ApplicationDbContext",
        configureToolResource: tool => tool.WithToolVersion("10.0.12"))
    .WithMigrationsProject("./BingoBoard.Data/BingoBoard.Data.csproj");

var frontend = builder.AddViteApp("bingoboard-dev", "./bingo-board")
    .WithBuildInfo(buildInfo, prefix: "VITE_")
    .WithReference(admin)
    .WaitFor(admin)
    .WithBrowserLogs();

builder.AddCSharpApp("producer-console", "./producerconsole.cs")
    .WithReference(admin).WaitFor(admin)
    .WithTerminal();

builder.AddYarp("bingoboard")
    .WithConfiguration(c =>
    {
        c.AddRoute("/api/version-info", admin);
        c.AddRoute("/bingohub/{**catch-all}", admin);
    })
    .PublishWithStaticFiles(frontend)
    .WaitFor(admin)
    .WithIconName("SerialPort")
    .WithExternalHttpEndpoints()
    .PublishAsBingoGateway(yarpDomain, yarpCertName)
    .WithExplicitStart();

builder.AddBingoMauiClients(admin);

builder.Build().Run();
