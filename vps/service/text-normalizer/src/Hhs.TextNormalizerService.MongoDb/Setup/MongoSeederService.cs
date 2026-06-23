using Hhs.TextNormalizerService.Domain.Enums;
using Hhs.TextNormalizerService.Domain.SettingDomain.Entities;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Subscribe;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.MongoDb.Setup;

public sealed class MongoSeederService(IServiceScopeFactory serviceScopeFactory) : IBasicDataSeeder
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<IAppConsoleLogger>();
        logger.LogDebug("{WorkerName} | {OperationStatus}", nameof(MongoSeederService), "START");

        bool isReadyDatabase = false;
        var dbContext = scope.ServiceProvider.GetRequiredService<TextNormalizerServiceDbContext>();
        var dataFilter = scope.ServiceProvider.GetRequiredService<IDataFilter>();
        try
        {
            // #region Check - Is ContentNormalizedRequest Collection Initialized
            //
            // long estimatedContentNormalizedRequestDocCount = await dbContext.ContentNormalizedRequests.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
            // if (estimatedContentNormalizedRequestDocCount < 1)
            // {
            //     var tempId = Guid.CreateVersion7();
            //     await dbContext.ContentNormalizedRequests.InsertOneAsync(new ContentNormalizedRequest(tempId, "test_scope",
            //         "test", Guid.CreateVersion7(), "test", ContentNormalizedRequestStates.CreatedWaitForScraping, "test"), cancellationToken: cancellationToken);
            //
            //     var filter = Builders<ContentNormalizedRequest>.Filter.Eq(doc => doc.Id, tempId);
            //     var delResult = await dbContext.ContentNormalizedRequests.DeleteOneAsync(filter, cancellationToken);
            //     if (delResult.DeletedCount < 1) throw new Exception($"{nameof(ContentNormalizedRequest)} First initialize error");
            //
            //     logger.LogDebug("{WorkerName} | MONGO DATABASE IS READY FOR {CollectionName}", nameof(MongoSeederService), nameof(ContentNormalizedRequest));
            // }
            //
            // #endregion
            //
            // #region Check - Is AnalysisNormalizedRequest Collection Initialized
            //
            // long estimatedAnalysisNormalizedRequestDocCount = await dbContext.AnalysisNormalizedRequests.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
            // if (estimatedAnalysisNormalizedRequestDocCount < 1)
            // {
            //     var tempId = Guid.CreateVersion7();
            //     await dbContext.AnalysisNormalizedRequests.InsertOneAsync(new AnalysisNormalizedRequest(tempId, "test_scope", "test", Guid.CreateVersion7(),
            //         DateTime.UtcNow.Date, [new AnalysisReferenceModel { AnalysisDataModel = new AnalysisDataModel { Title = "test", ImageUrl = "image", Spot = "spot" }, CustomerContentId = Guid.CreateVersion7(), ContentNormalizedRequestId = Guid.CreateVersion7() }], AnalysisNormalizedRequestStates.CreatedWaitForOutline, "test"), cancellationToken: cancellationToken);
            //
            //     var filter = Builders<AnalysisNormalizedRequest>.Filter.Eq(doc => doc.Id, tempId);
            //     var delResult = await dbContext.AnalysisNormalizedRequests.DeleteOneAsync(filter, cancellationToken);
            //     if (delResult.DeletedCount < 1) throw new Exception($"{nameof(AnalysisNormalizedRequest)} First initialize error");
            //
            //     logger.LogDebug("{WorkerName} | MONGO DATABASE IS READY FOR {CollectionName}", nameof(MongoSeederService), nameof(AnalysisNormalizedRequest));
            // }
            //
            // #endregion

            #region Check - Is CustomerVpSetting Collection Initialized

            long estimatedCustomerVpSettingDocCount = await dbContext.CustomerVpSettings.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
            if (estimatedCustomerVpSettingDocCount < 1)
            {
                var tempId = Guid.CreateVersion7();
                await dbContext.CustomerVpSettings.InsertOneAsync(new CustomerVpSetting(tempId, Guid.CreateVersion7(), "test_domain"), cancellationToken: cancellationToken);

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