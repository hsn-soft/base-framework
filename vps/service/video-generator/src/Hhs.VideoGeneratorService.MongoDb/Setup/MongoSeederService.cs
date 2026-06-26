using Hhs.VideoGeneratorService.Domain.SettingDomain.Entities;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Subscribe;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.MongoDb.Setup;

public sealed class MongoSeederService(IServiceScopeFactory serviceScopeFactory) : IBasicDataSeeder
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<IAppConsoleLogger>();
        logger.LogDebug("{WorkerName} | {OperationStatus}", nameof(MongoSeederService), "START");

        bool isReadyDatabase = false;
        var dbContext = scope.ServiceProvider.GetRequiredService<VideoGeneratorServiceDbContext>();
        var dataFilter = scope.ServiceProvider.GetRequiredService<IDataFilter>();
        try
        {
            // #region Check - Is VideoRequest Collection Initialized
            //
            // long estimatedVideoRequestDocCount = await dbContext.VideoRequests.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
            // if (estimatedVideoRequestDocCount < 1)
            // {
            //     var tempId = Guid.NewGuid();
            //     await dbContext.VideoRequests.InsertOneAsync(new VideoRequest(tempId, "test_cope", "test", ReferenceContentTypes.CUSTOMER_CONTENT, Guid.NewGuid(),
            //         VideoRequestStates.CreatedWaitForVideoSent, [new NormalizedContentData { NormalizedContent = "test" }]), cancellationToken: cancellationToken);
            //
            //     var filter = Builders<VideoRequest>.Filter.Eq(doc => doc.Id, tempId);
            //     var delResult = await dbContext.VideoRequests.DeleteOneAsync(filter, cancellationToken);
            //     if (delResult.DeletedCount < 1) throw new Exception($"{nameof(VideoRequest)} First initialize error");
            //
            //     logger.LogDebug("{WorkerName} | MONGO DATABASE IS READY FOR {CollectionName}", nameof(MongoSeederService), nameof(VideoRequest));
            // }
            //
            // #endregion

            #region Check - Is CustomerVpSetting Collection Initialized

            long estimatedCustomerVpSettingDocCount = await dbContext.CustomerVpSettings.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
            if (estimatedCustomerVpSettingDocCount < 1)
            {
                var tempId = Guid.CreateVersion7();
                await dbContext.CustomerVpSettings.InsertOneAsync(new CustomerVpSetting(tempId, Guid.CreateVersion7(), "test","test"), cancellationToken: cancellationToken);

                var filter = Builders<CustomerVpSetting>.Filter.Eq(doc => doc.Id, tempId);
                var delResult = await dbContext.CustomerVpSettings.DeleteOneAsync(filter, cancellationToken);
                if (delResult.DeletedCount < 1) throw new Exception($"{nameof(CustomerVpSetting)} First initialize error");

                logger.LogDebug("{WorkerName} | MONGO DATABASE IS READY FOR {CollectionName}", nameof(MongoSeederService), nameof(CustomerVpSetting));
            }

            #endregion

            logger.LogDebug("{WorkerName} | INITIALIZE SUCCESSFULLY COMPLETED", nameof(MongoSeederService));

            isReadyDatabase = true;
        }
        catch (Exception e)
        {
            logger.LogError("{WorkerName} | {OperationStatus} | {Error}", nameof(MongoSeederService), "FAIL", e.Message);
        }

        if (isReadyDatabase)
        {
            try
            {
                using (dataFilter.Disable<IScopeSubscription>())
                {
                    await CustomerVpSettingSeeder.SeedAsync(dbContext, logger);
                }
            }
            catch (Exception e)
            {
                logger.LogError("{WorkerName} | {OperationStatus}: {Error}", nameof(MongoSeederService), "SEED_ERROR", e.Message);
            }
        }
    }
}