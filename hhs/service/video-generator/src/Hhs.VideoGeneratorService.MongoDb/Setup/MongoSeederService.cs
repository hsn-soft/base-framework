using Hhs.Shared.Helper.Enums;
using Hhs.VideoGeneratorService.Domain.Enums;
using Hhs.VideoGeneratorService.Domain.VideoDomain.Entities;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging.Abstracts;
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

        var dbContext = scope.ServiceProvider.GetRequiredService<VideoGeneratorServiceDbContext>();

        try
        {
            #region Check - Is VideoRequest Collection Initialized

            long estimatedVideoRequestDocCount = await dbContext.VideoRequests.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
            if (estimatedVideoRequestDocCount < 1)
            {
                var tempId = Guid.NewGuid();
                await dbContext.VideoRequests.InsertOneAsync(new VideoRequest(tempId, Guid.NewGuid(), Guid.NewGuid(), "test", ReferenceContentTypes.APP_REQUEST_CONTENT, Guid.NewGuid(),
                    VideoRequestStates.CreatedWaitForVideoSent, new List<NormalizedContentData> { new() { NormalizedContent = "test" } }), cancellationToken: cancellationToken);

                var filter = Builders<VideoRequest>.Filter.Eq(doc => doc.Id, tempId);
                var delResult = await dbContext.VideoRequests.DeleteOneAsync(filter, cancellationToken);
                if (delResult.DeletedCount < 1) throw new Exception("VideoRequests First initialize error");

                logger.LogDebug("{WorkerName} | MONGO DATABASE IS READY FOR VIDEO REQUEST", nameof(MongoSeederService));
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