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
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.OperationalInsights;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

var buildInfo = BuildInfo.Load(builder);
var postgresAzureLocation = builder.Configuration["Azure:PostgresLocation"];

Console.WriteLine($"Environment name: {builder.Environment.EnvironmentName}");

builder.AddAzureContainerAppEnvironment("env")
    .ConfigureInfrastructure(infrastructure =>
    {
        var environment = infrastructure.GetProvisionableResources()
            .OfType<ContainerAppManagedEnvironment>()
            .Single();
        environment.AppLogsConfiguration = new ContainerAppLogsConfiguration
        {
            // Azure's CLI "none" option sends a JSON null, not the string "none".
            Destination = new BicepValue<string>(literal: null)
        };

        var workspace = infrastructure.GetProvisionableResources()
            .OfType<OperationalInsightsWorkspace>()
            .Single();
        workspace.RetentionInDays = 30;
        workspace.WorkspaceCapping = new OperationalInsightsWorkspaceCapping
        {
            DailyQuotaInGB = 1
        };
    });

var adminDomain = builder.AddParameter("admin-domain", "admin.aspireify.live");
var adminCertName = builder.AddParameter("admin-cert-name", "admin.aspireify.live-envvevso-251017190301");
var yarpDomain = builder.AddParameter("yarp-domain", "aspireify.live");
var yarpCertName = builder.AddParameter("yarp-cert-name", "aspireify.live-envvevso-251017185247");

var cache = builder.AddRedis("cache")
    .PublishAsBingoCache();

var admin = builder.AddProject<Projects.BingoBoard_Admin>("boardadmin")
    .WithReference(cache)
    .WaitFor(cache)
    .WithBuildInfo(buildInfo)
    .WithExternalHttpEndpoints()
    .WithImportSquaresCommand()
    .PublishAsBingoAdmin(adminDomain, adminCertName);

if (builder.ExecutionContext.IsRunMode)
{
    var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
        .ConfigureBingoPostgres(postgresAzureLocation)
        .WithPasswordAuthentication()
        .RunAsContainer(container => container.WithLifetime(ContainerLifetime.Persistent));
    var db = postgres.AddDatabase("db").WithPostgresMcp();
    var password = builder.AddParameter("admin-password", secret: true);
    var migrations = builder.AddProject<Projects.BingoBoard_MigrationService>("migrations")
        .WithEnvironment("Authentication__AdminPassword", password)
        .WithReference(db)
        .WaitFor(db);

    admin.WithReference(db).WithReference(migrations).WaitForCompletion(migrations);
}
else
{
    // Production owns neither the existing server nor its credentials or schema.
    admin.WithReference(builder.AddConnectionString("db"));
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
