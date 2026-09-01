using Azure.Core;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.PostgreSql;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Yarp;

#pragma warning disable ASPIREACADOMAINS001

internal static class DeploymentExtensions
{
    public static IResourceBuilder<RedisResource> PublishAsBingoCache(
        this IResourceBuilder<RedisResource> cache)
    {
        return cache.PublishAsAzureContainerApp((_, app) =>
        {
            app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
            app.Template.Scale.MaxReplicas = 1;
        });
    }

    public static IResourceBuilder<AzurePostgresFlexibleServerResource> ConfigureBingoPostgres(
        this IResourceBuilder<AzurePostgresFlexibleServerResource> postgres,
        string? azureLocation)
    {
        return postgres.ConfigureInfrastructure(infrastructure =>
        {
            const int minimumBackupRetentionDays = 7;
            const int minimumStorageSizeInGb = 32;

            var flexibleServer = infrastructure.GetProvisionableResources()
                .OfType<PostgreSqlFlexibleServer>()
                .Single();

            if (!string.IsNullOrWhiteSpace(azureLocation))
            {
                flexibleServer.Location = new AzureLocation(azureLocation);
            }

            flexibleServer.Sku = new PostgreSqlFlexibleServerSku
            {
                Name = "Standard_B1ms",
                Tier = PostgreSqlFlexibleServerSkuTier.Burstable
            };
            flexibleServer.Backup = new PostgreSqlFlexibleServerBackupProperties
            {
                BackupRetentionDays = minimumBackupRetentionDays,
                GeoRedundantBackup = PostgreSqlFlexibleServerGeoRedundantBackupEnum.Disabled
            };
            flexibleServer.HighAvailability = new PostgreSqlFlexibleServerHighAvailability
            {
                Mode = PostgreSqlFlexibleServerHighAvailabilityMode.Disabled
            };
            flexibleServer.Storage = new PostgreSqlFlexibleServerStorage
            {
                StorageSizeInGB = minimumStorageSizeInGb,
                AutoGrow = StorageAutoGrow.Disabled
            };
        });
    }

    public static IResourceBuilder<T> PublishAsBingoAdmin<T>(
        this IResourceBuilder<T> admin,
        IResourceBuilder<ParameterResource> domain,
        IResourceBuilder<ParameterResource> certificateName)
        where T : ProjectResource
    {
        return admin.PublishAsAzureContainerApp((_, app) =>
        {
            app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
            app.Template.Scale.MaxReplicas = 1;
            app.Template.Scale.MinReplicas = 1;
            app.ConfigureCustomDomain(domain, certificateName);
        });
    }

    public static IResourceBuilder<YarpResource> PublishAsBingoGateway(
        this IResourceBuilder<YarpResource> gateway,
        IResourceBuilder<ParameterResource> domain,
        IResourceBuilder<ParameterResource> certificateName)
    {
        return gateway.PublishAsAzureContainerApp((_, app) =>
        {
            app.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
            app.Template.Scale.MaxReplicas = 5;
            app.Template.Scale.MinReplicas = 1;
            app.ConfigureCustomDomain(domain, certificateName);
            app.Template.Scale.Rules.Add(new(
                new ContainerAppScaleRule
                {
                    Name = "http-scaler",
                    Http = new() { Metadata = new() { ["concurrentRequests"] = "100" } }
                }));
        });
    }
}
