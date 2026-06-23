using Hhs.Shared.Contracts.Events;
using Hhs.TextNormalizerService.MongoDb.Context;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Application.Services;

public sealed class NormalizerOperationAppService(
    IServiceProvider provider,
    TextNormalizerServiceDbContext context,
    ILogger<NormalizerOperationAppService> logger) : ApplicationServiceBase(provider)
{
    public async Task CreateCustomerContentNormalizeRequestAsync(CustomerContentCreatedEto @event, Guid eventId)
    {
        logger.LogInformation($"CreateCustomerContentNormalizeRequestAsync started for RefContentId: {@event.CustomerContentId}, EventId: {eventId}");

        var existing = await context.CustomerContentNormalizedRequests
            .Find(x => x.SourceEventId == eventId || x.CustomerContentId == @event.CustomerContentId)
            .FirstOrDefaultAsync();

        logger.LogInformation($"Creating new request...");

        var requestId = Guid.NewGuid();
    }
}