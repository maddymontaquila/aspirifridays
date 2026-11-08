#!/usr/bin/env dotnet
#:sdk Aspire.AppHost.Sdk@17.1.0-preview.1.26558.1
#:property AspireUseCliBundle=true
#:package Aspire.Hosting.Azure.AppContainers
#:package Aspire.Hosting.Azure.PostgreSQL
#:package Aspire.Hosting.Azure.Redis
#:package Aspire.Hosting.Docker
#:package Aspire.Hosting.Redis
#:package Aspire.Hosting.JavaScript
#:package Aspire.Hosting.Yarp
#:package Aspire.Hosting.Maui
#:package Aspire.Hosting.DevTunnels
#:package Aspire.Hosting.Browsers
#:package Aspire.Hosting.EntityFrameworkCore
#:package Aspire.Hosting.Dotnet
#:project ./BingoBoard.Admin
#:project ./BingoBoard.MigrationService
#:include ./apphost/*.cs

#pragma warning disable ASPIREBROWSERLOGS001, ASPIREPOSTGRES001, ASPIRETERMINAL001

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

builder.AddDotnetProject("producer-console", "./producerconsole.cs")
    .WithReference(admin).WaitFor(admin)
    .WithTerminal()
    .ExcludeFromManifest();

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
