using Hhs.EventManagerService.Domain.Enums;
using Hhs.EventManagerService.Domain.EventDomain.Entities;
using Hhs.EventManagerService.MongoDb.Context;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.Users;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Hhs.EventManagerService.MongoDb.Setup;

public sealed class MongoSeederService : IBasicDataSeeder
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public MongoSeederService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<IAppConsoleLogger>();
        logger.LogDebug("{WorkerName} | {OperationStatus}", nameof(MongoSeederService), "START");

        try
        {
            var test = scope.ServiceProvider.GetRequiredService<ICurrentUser>();
            var dbContext = scope.ServiceProvider.GetRequiredService<EventManagerServiceDbContext>();
            long estimatedDocCount = await dbContext.FailedIntegrationEvents.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
            if (estimatedDocCount > 0)
            {
                logger.LogDebug("{WorkerName} | MONGO DATABASE IS READY", nameof(MongoSeederService));
                return;
            }

            var tempId = Guid.CreateVersion7();
            await dbContext.FailedIntegrationEvents.InsertOneAsync(new FailedIntegrationEvent(tempId, DateTime.UtcNow, "test",
                FailedIntegrationEventStates.CreatedWaitForHandling), cancellationToken: cancellationToken);

            var filter = Builders<FailedIntegrationEvent>.Filter.Eq(doc => doc.Id, tempId);
            var delResult = await dbContext.FailedIntegrationEvents.DeleteOneAsync(filter, cancellationToken);
            if (delResult.DeletedCount < 1) throw new Exception("First initialize error");

            logger.LogDebug("{WorkerName} | FIRST INITIALIZE SUCCESSFULLY COMPLETED", nameof(MongoSeederService));
        }
        catch (Exception e)
        {
            logger.LogError("{WorkerName} | {OperationStatus} | {Error}", nameof(MongoSeederService), "FAIL", e.Message);
        }
    }
}