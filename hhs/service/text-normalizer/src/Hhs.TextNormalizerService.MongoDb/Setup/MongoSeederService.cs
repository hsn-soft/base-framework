using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.Enums;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging.Abstracts;
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

        var dbContext = scope.ServiceProvider.GetRequiredService<TextNormalizerServiceDbContext>();

        try
        {
            #region Check - Is NormalizedRequest Collection Initialized

            long estimatedNormalizedRequestDocCount = await dbContext.NormalizedRequests.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
            if (estimatedNormalizedRequestDocCount < 1)
            {
                var tempId = Guid.CreateVersion7();
                await dbContext.NormalizedRequests.InsertOneAsync(new NormalizedRequest(tempId, Guid.CreateVersion7(), Guid.CreateVersion7(),
                    "test", Guid.CreateVersion7(), "test", NormalizedRequestStates.CreatedWaitForScraping, "test"), cancellationToken: cancellationToken);

                var filter = Builders<NormalizedRequest>.Filter.Eq(doc => doc.Id, tempId);
                var delResult = await dbContext.NormalizedRequests.DeleteOneAsync(filter, cancellationToken);
                if (delResult.DeletedCount < 1) throw new Exception("NormalizedRequests First initialize error");

                logger.LogDebug("{WorkerName} | MONGO DATABASE IS READY FOR NORMALIZED REQUEST", nameof(MongoSeederService));
            }

            #endregion


            #region Check - Is NormalizedAnalysis Collection Initialized

            long estimatedNormalizedAnalysisDocCount = await dbContext.NormalizedAnalysis.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
            if (estimatedNormalizedAnalysisDocCount < 1)
            {
                var tempId = Guid.CreateVersion7();
                await dbContext.NormalizedAnalysis.InsertOneAsync(new NormalizedAnalysis(tempId, Guid.CreateVersion7(), Guid.CreateVersion7(), "test", Guid.CreateVersion7(),
                    DateTime.UtcNow.Date, new List<AnalysisReferenceModel> { new() { AnalysisDataModel = new AnalysisDataModel { Title = "test", ImageUrl = "image", Spot = "spot" }, AppContentId = Guid.CreateVersion7(), NormalizedRequestId = Guid.CreateVersion7() } }, NormalizedAnalysisStates.CreatedWaitForOutline, "test"), cancellationToken: cancellationToken);

                var filter = Builders<NormalizedAnalysis>.Filter.Eq(doc => doc.Id, tempId);
                var delResult = await dbContext.NormalizedAnalysis.DeleteOneAsync(filter, cancellationToken);
                if (delResult.DeletedCount < 1) throw new Exception("NormalizedAnalysis First initialize error");

                logger.LogDebug("{WorkerName} | MONGO DATABASE IS READY FOR NORMALIZED ANALYSIS", nameof(MongoSeederService));
            }

            #endregion


            #region Check - Is CustomerConfigurations Collection Initialized

            await ClientConfigurationSeeder.SeedAsync(dbContext, logger);

            #endregion

            logger.LogDebug("{WorkerName} | INITIALIZE SUCCESSFULLY COMPLETED", nameof(MongoSeederService));
        }
        catch (Exception e)
        {
            logger.LogError("{WorkerName} | {OperationStatus} | {Error}", nameof(MongoSeederService), "FAIL", e.Message);
        }
    }
}