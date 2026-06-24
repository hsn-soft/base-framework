using Hhs.FeedRService.Domain.ConfigurationDomain.Entities;
using Hhs.FeedRService.Domain.ConfigurationDomain.Models;
using Hhs.FeedRService.MongoDb.Context;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.FeedRService.MongoDb;

public sealed class MongoSeederService(IServiceScopeFactory serviceScopeFactory) : IBasicDataSeeder
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<IAppConsoleLogger>();
        logger.LogDebug("{WorkerName} | {OperationStatus}", nameof(MongoSeederService), "START");

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FeedRServiceDbContext>();

            long customerConfigurationsDocCount = await dbContext.CustomerConfigurations.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
            if (customerConfigurationsDocCount < 1)
            {
            
                var t24ClientId = Guid.Parse("46f859a8-0737-452a-8fd6-258156fdf901");
                var t24TenantId = Guid.Parse("f05ad9c0-52c7-4ad8-8349-1f4bcc1b9bf9");
                string t24DomainName = "www.t24.com.tr";
                string t24Network = "21852615636";
                string t24AdUnitIdTopLevel = "23211338768";
                List<string> t24AdUnitIdList = new List<string> { "23322576727"};
                string t24AdUnitName = "VideoReklamEnvanteri-t24";
                await dbContext.CustomerConfigurations.InsertOneAsync(InitCustomerConfiguration(t24TenantId, t24ClientId, t24DomainName,t24Network, t24AdUnitName, t24AdUnitIdTopLevel, t24AdUnitIdList
                ), cancellationToken: cancellationToken);

                var cnbceClientId = Guid.Parse("67f812b7-0737-452a-8fd6-258156abc123");
                var cnbceTenantId = Guid.Parse("f05ad9c0-52c7-4ad8-8349-1f4bcc1b9bf9");
                string cnbceDomainName = "www.cnbce.com.tr";
                string cnbceNetwork = "21852615636";
                string cnbceAdUnitIdTopLevel = "23211338768";
                List<string> cnbceAdUnitIdList = new List<string> { "23322582028"};
                string cnbceAdUnitName = "VideoReklamEnvanteri-Cnbce";
                await dbContext.CustomerConfigurations.InsertOneAsync(InitCustomerConfiguration(cnbceTenantId, cnbceClientId, cnbceDomainName,cnbceNetwork, cnbceAdUnitName, cnbceAdUnitIdTopLevel, cnbceAdUnitIdList
                ), cancellationToken: cancellationToken);

                var tamindirClientId = Guid.Parse("59f812b7-0737-452a-8fd6-258156abc159");
                var tamindirTenantId = Guid.Parse("a15ad9c0-52c7-4ad8-8349-1f4bcc1b9ba1");
                string tamindirDomainName = "www.tamindir.com.tr";
                string tamindirNetwork = "21852615636";
                string tamindirAdUnitIdTopLevel = "23211338768";
                List<string> tamindirAdUnitIdList = new List<string> { "23321938350"};
                string tamindirAdUnitName = "VideoReklamEnvanteri-Tamindir";
                await dbContext.CustomerConfigurations.InsertOneAsync(InitCustomerConfiguration(tamindirTenantId, tamindirClientId, tamindirDomainName,tamindirNetwork, tamindirAdUnitName, tamindirAdUnitIdTopLevel, tamindirAdUnitIdList
                ), cancellationToken: cancellationToken);

                logger.LogDebug("{WorkerName} | MONGO DATABASE IS READY FOR CustomerConfigurations", nameof(MongoSeederService));
            }

            long networkConfigurationsDocCount = await dbContext.NetworkConfigurations.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
            if (networkConfigurationsDocCount < 1)
            {
                // Seed: Bond network — client-to-AdUnitId mappings live in CustomerConfiguration, not here.
                var bondNetworkId = Guid.Parse("67f812b7-0737-452a-8fd6-258156abc123");
                var bondTenantId = Guid.Parse("b15ad9c0-52c7-4ad8-8349-1f4bcc1b9bb1");
                await dbContext.NetworkConfigurations.InsertOneAsync(new NetworkConfiguration(
                    networkId: Guid.NewGuid(),
                    tenantId: bondTenantId,
                    networkCode: "21852615636",
                    displayName: "Bond Network",
                    isActive: true,
                    topLevelGroups: new List<TopLevelGroupConfig>
                    {
                        new TopLevelGroupConfig("23211338768", "tech-summus", true)
                    }), cancellationToken: cancellationToken);

                logger.LogDebug("{WorkerName} | MONGO DATABASE IS READY FOR NetworkConfigurations", nameof(MongoSeederService));
            }

            logger.LogDebug("{WorkerName} | INITIALIZE SUCCESSFULLY COMPLETED", nameof(MongoSeederService));
        }
        catch (Exception e)
        {
            logger.LogError("{WorkerName} | {OperationStatus} | {Error}", nameof(MongoSeederService), "FAIL", e.Message);
        }
    }

    private CustomerConfiguration InitCustomerConfiguration(Guid tenantId, Guid clientId, string domainName, string network, string adUnitName, string adUnitIdTopLevel, List<string> adUnitId)
    {
        var conf = new CustomerConfiguration(id: Guid.NewGuid(), tenantId: tenantId, clientId: clientId, clientName: domainName, network: network, adUnitName: adUnitName, adUnitIdTopLevel: adUnitIdTopLevel, adUnitId: adUnitId);
        return conf;
    }
}