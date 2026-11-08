#!/usr/bin/env dotnet
#:sdk Aspire.AppHost.Sdk@13.5.3
#:package Aspire.Hosting.Azure.AppContainers
#:package Aspire.Hosting.Azure.PostgreSQL
#:package Aspire.Hosting.Azure.Redis
#:package Aspire.Hosting.Docker
#:package Aspire.Hosting.Redis
#:package Aspire.Hosting.JavaScript
#:package Aspire.Hosting.Yarp
#:package Aspire.Hosting.Maui
#:package Aspire.Hosting.DevTunnels
#:project ./BingoBoard.Admin
#:project ./BingoBoard.MigrationService
#:include ./apphost/*.cs

#pragma warning disable

using Microsoft.Extensions.Hosting;
using Azure.Provisioning.OperationalInsights;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

var buildInfo = BuildInfo.Load(builder);
var postgresAzureLocation = builder.Configuration["Azure:PostgresLocation"];
var postgresUserName = builder.ExecutionContext.IsPublishMode
    ? builder.AddParameter("postgres-username")
    : null;
var postgresPassword = builder.ExecutionContext.IsPublishMode
    ? builder.AddParameter("postgres-password", secret: true)
    : null;

Console.WriteLine($"Environment name: {builder.Environment.EnvironmentName}");

builder.AddAzureContainerAppEnvironment("env")
    .ConfigureInfrastructure(infrastructure =>
    {
        var workspace = infrastructure.GetProvisionableResources()
            .OfType<OperationalInsightsWorkspace>()
            .Single();
        workspace.RetentionInDays = 30;
        workspace.WorkspaceCapping = new OperationalInsightsWorkspaceCapping
        {
            DailyQuotaInGB = 1
        };
    });

var password = builder.AddParameter("admin-password", secret: true);
var adminDomain = builder.AddParameter("admin-domain", "admin.aspireify.live");
var adminCertName = builder.AddParameter("admin-cert-name", "admin.aspireify.live-envvevso-251017190301");
var yarpDomain = builder.AddParameter("yarp-domain", "aspireify.live");
var yarpCertName = builder.AddParameter("yarp-cert-name", "aspireify.live-envvevso-251017185247");

var cache = builder.AddRedis("cache")
    .PublishAsBingoCache();

var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
    .ConfigureBingoPostgres(postgresAzureLocation)
    .WithPasswordAuthentication(userName: postgresUserName, password: postgresPassword)
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

if (builder.ExecutionContext.IsPublishMode)
{
    var secureDbConnection = ReferenceExpression.Create($"{db.Resource};SSL Mode=VerifyFull");
    migrations.WithEnvironment("ConnectionStrings__db", secureDbConnection);
    admin.WithEnvironment("ConnectionStrings__db", secureDbConnection);
}

var frontend = builder.AddViteApp("bingoboard-dev", "./bingo-board")
    .WithBuildInfo(buildInfo, prefix: "VITE_")
    .WithReference(admin)
    .WaitFor(admin);

if (builder.ExecutionContext.IsRunMode)
{
    builder.AddCSharpApp("producer-console", "./producerconsole.cs")
        .WithReference(admin).WaitFor(admin)
        .WithTerminal();
}

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
