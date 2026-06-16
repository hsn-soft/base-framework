using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.TextNormalizerService.Entities;
using Hhs.TextNormalizerService.Mongo;
using Hhs.TextNormalizerService.Providers;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Services;

public sealed class OutlineProviderPollingAppService
{
    private readonly NormalizerMongoContext _context;
    private readonly IOutlineProviderResolver _outlineProviderResolver;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OutlineProviderPollingAppService> _logger;

    public OutlineProviderPollingAppService(
        NormalizerMongoContext context,
        IOutlineProviderResolver outlineProviderResolver,
        IEventBus eventBus,
        ILogger<OutlineProviderPollingAppService> logger)
    {
        _context = context;
        _outlineProviderResolver = outlineProviderResolver;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task PollDueOutlineRequestsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await PollCustomerRequestsAsync(now, cancellationToken);
        await PollAnalysisRequestsAsync(now, cancellationToken);
    }

    private async Task PollCustomerRequestsAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var requests = await _context.CustomerRequests
            .Find(x =>
                x.Status == "OUTLINE_PROVIDER_POLLING" &&
                x.NextOutlinePollAtUtc != null &&
                x.NextOutlinePollAtUtc <= now &&
                x.OutlineProviderTrackId != null)
            .Limit(50)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                if (request.OutlinePollingCount >= request.MaxOutlinePollingCount)
                {
                    request.Status = "FAILED";
                    request.LastError = "Outline provider polling timeout.";
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceCustomerAsync(request, cancellationToken);

                    await _eventBus.PublishAsync(new StepFailedEvent
                    {
                        CorrelationId = request.CorrelationId,
                        CustomerContentId = request.CustomerContentId,
                        ContentProcessType = ContentProcessTypes.CustomerContent,
                        Step = EventNames.OutlineProviderPollingStarted,
                        ErrorMessage = request.LastError,
                        Retryable = false
                    }, cancellationToken);

                    continue;
                }

                var provider = _outlineProviderResolver.Resolve(request.OutlineProviderKey);

                var status = await provider.GetStatusAsync(
                    request.OutlineProviderTrackId!,
                    cancellationToken);

                if (status.IsFailed)
                {
                    request.Status = "FAILED";
                    request.LastError = status.ErrorMessage ?? "Outline provider failed.";
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceCustomerAsync(request, cancellationToken);

                    await _eventBus.PublishAsync(new StepFailedEvent
                    {
                        CorrelationId = request.CorrelationId,
                        CustomerContentId = request.CustomerContentId,
                        ContentProcessType = ContentProcessTypes.CustomerContent,
                        Step = EventNames.OutlineProviderPollingStarted,
                        ErrorMessage = request.LastError,
                        Retryable = false
                    }, cancellationToken);

                    continue;
                }

                if (!status.IsCompleted)
                {
                    request.OutlinePollingCount++;
                    request.NextOutlinePollAtUtc = DateTime.UtcNow.AddMinutes(5);
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceCustomerAsync(request, cancellationToken);
                    continue;
                }

                request.OutlinePollingCount++;
                request.NextOutlinePollAtUtc = null;
                request.Status = "OUTLINE_PROVIDER_COMPLETED";
                request.OutlineStatus = "PROVIDER_COMPLETED";
                request.CurrentStep = EventNames.OutlineProviderCompleted;
                request.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceCustomerAsync(request, cancellationToken);

                if (string.IsNullOrWhiteSpace(status.Script))
                    throw new InvalidOperationException("Outline provider completed but script is empty.");

                await _eventBus.PublishAsync(new OutlineProviderCompletedEvent
                {
                    CustomerContentId = request.CustomerContentId,
                    ContentProcessType = ContentProcessTypes.CustomerContent,
                    CorrelationId = request.CorrelationId,
                    NormalizedRequestId = request.Id,
                    Script = status.Script!
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                request.OutlinePollingCount++;
                request.LastError = ex.Message;
                request.UpdatedAtUtc = DateTime.UtcNow;

                if (request.OutlinePollingCount >= request.MaxOutlinePollingCount)
                {
                    request.Status = "FAILED";
                    request.OutlineStatus = "FAILED";
                    request.CurrentStep = EventNames.OutlineProviderPollingStarted;
                    request.NextOutlinePollAtUtc = null;

                    await ReplaceCustomerAsync(request, cancellationToken);

                    await _eventBus.PublishAsync(new StepFailedEvent
                    {
                        CorrelationId = request.CorrelationId,
                        CustomerContentId = request.CustomerContentId,
                        ContentProcessType = ContentProcessTypes.CustomerContent,
                        Step = EventNames.OutlineProviderPollingStarted,
                        ErrorMessage = ex.Message,
                        Retryable = false
                    }, cancellationToken);
                }
                else
                {
                    request.Status = "OUTLINE_PROVIDER_POLLING";
                    request.OutlineStatus = "POLLING";
                    request.CurrentStep = EventNames.OutlineProviderPollingStarted;
                    request.NextOutlinePollAtUtc = DateTime.UtcNow.AddMinutes(5);

                    await ReplaceCustomerAsync(request, cancellationToken);
                }

                _logger.LogError(
                    ex,
                    "Customer outline polling failed. CustomerContentId: {CustomerContentId}",
                    request.CustomerContentId);
            }
        }
    }

    private async Task PollAnalysisRequestsAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var filter = Builders<AnalysisContentNormalizedRequest>.Filter.ElemMatch(
            x => x.Items,
            i =>
                i.OutlineStatus == "POLLING" &&
                i.NextOutlinePollAtUtc <= now &&
                i.OutlineProviderTrackId != null);

        var requests = await _context.AnalysisRequests
            .Find(filter)
            .Limit(50)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            var pollingItems = request.Items
                .Where(i =>
                    i.OutlineStatus == "POLLING" &&
                    i.NextOutlinePollAtUtc <= now &&
                    i.OutlineProviderTrackId != null)
                .OrderBy(i => i.SortOrder)
                .ToList();

            foreach (var item in pollingItems)
            {
                try
                {
                    var claimUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                        .Set("Items.$.NextOutlinePollAtUtc", DateTime.UtcNow.AddMinutes(1))
                        .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
                        .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                    var claimResult = await UpdateDuePollingAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        now,
                        claimUpdate,
                        cancellationToken);

                    if (claimResult.ModifiedCount == 0)
                        continue;

                    if (item.OutlinePollingCount >= item.MaxOutlinePollingCount)
                    {
                        await FailAnalysisPollingItemAsync(
                            request,
                            item,
                            "Outline provider polling timeout.",
                            false,
                            cancellationToken);

                        continue;
                    }

                    var provider = _outlineProviderResolver.Resolve(request.OutlineProviderKey);

                    var status = await provider.GetStatusAsync(
                        item.OutlineProviderTrackId!,
                        cancellationToken);

                    if (status.IsFailed)
                    {
                        await FailAnalysisPollingItemAsync(
                            request,
                            item,
                            status.ErrorMessage ?? "Outline provider failed.",
                            false,
                            cancellationToken);

                        continue;
                    }

                    if (!status.IsCompleted)
                    {
                        var update = Builders<AnalysisContentNormalizedRequest>.Update
                            .Inc("Items.$.OutlinePollingCount", 1)
                            .Set("Items.$.NextOutlinePollAtUtc", DateTime.UtcNow.AddMinutes(5))
                            .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
                            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                        await UpdateAnalysisItemAsync(
                            request.Id,
                            item.CustomerContentId,
                            update,
                            cancellationToken);

                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(status.Script))
                        throw new InvalidOperationException("Outline provider completed but script is empty.");

                    var completedUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                        .Inc("Items.$.OutlinePollingCount", 1)
                        .Set("Items.$.Status", "OUTLINE_PROVIDER_COMPLETED")
                        .Set("Items.$.CurrentStep", EventNames.OutlineProviderCompleted)
                        .Set("Items.$.NextOutlinePollAtUtc", (DateTime?)null)
                        .Set("Items.$.OutlineStatus", "PROVIDER_COMPLETED")
                        .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
                        .Set(x => x.CurrentStep, EventNames.OutlineProviderCompleted)
                        .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                    await UpdateAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        completedUpdate,
                        cancellationToken);

                    await _eventBus.PublishAsync(new OutlineProviderCompletedEvent
                    {
                        AnalysisContentId = request.AnalysisContentId,
                        CustomerContentId = item.CustomerContentId,
                        CustomerContentIdForItem = item.CustomerContentId,
                        ContentProcessType = ContentProcessTypes.AnalysisContent,
                        CorrelationId = request.CorrelationId,
                        NormalizedRequestId = request.Id,
                        SortOrder = item.SortOrder,
                        Script = status.Script!
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    var nextCount = item.OutlinePollingCount + 1;

                    if (nextCount >= item.MaxOutlinePollingCount)
                    {
                        await FailAnalysisPollingItemAsync(
                            request,
                            item,
                            ex.Message,
                            false,
                            cancellationToken);

                        _logger.LogError(
                            ex,
                            "Analysis outline item polling failed permanently. AnalysisContentId: {AnalysisContentId}, CustomerContentId: {CustomerContentId}",
                            request.AnalysisContentId,
                            item.CustomerContentId);

                        continue;
                    }

                    var update = Builders<AnalysisContentNormalizedRequest>.Update
                        .Set("Items.$.OutlinePollingCount", nextCount)
                        .Set("Items.$.Status", "OUTLINE_PROVIDER_POLLING")
                        .Set("Items.$.CurrentStep", EventNames.OutlineProviderPollingStarted)
                        .Set("Items.$.OutlineStatus", "POLLING")
                        .Set("Items.$.LastError", ex.Message)
                        .Set("Items.$.NextOutlinePollAtUtc", DateTime.UtcNow.AddMinutes(5))
                        .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
                        .Set(x => x.Status, "OUTLINE_PROVIDER_POLLING")
                        .Set(x => x.CurrentStep, EventNames.OutlineProviderPollingStarted)
                        .Set(x => x.LastError, ex.Message)
                        .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                    await UpdateAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        update,
                        cancellationToken);

                    _logger.LogError(
                        ex,
                        "Analysis outline item polling failed. AnalysisContentId: {AnalysisContentId}, CustomerContentId: {CustomerContentId}",
                        request.AnalysisContentId,
                        item.CustomerContentId);
                }
            }
        }
    }

    private Task<UpdateResult> UpdateDuePollingAnalysisItemAsync(
        Guid analysisRequestId,
        Guid customerContentId,
        DateTime now,
        UpdateDefinition<AnalysisContentNormalizedRequest> update,
        CancellationToken cancellationToken)
    {
        var filter = Builders<AnalysisContentNormalizedRequest>.Filter.And(
            Builders<AnalysisContentNormalizedRequest>.Filter.Eq(x => x.Id, analysisRequestId),
            Builders<AnalysisContentNormalizedRequest>.Filter.ElemMatch(
                x => x.Items,
                i =>
                    i.CustomerContentId == customerContentId &&
                    i.OutlineStatus == "POLLING" &&
                    i.NextOutlinePollAtUtc != null &&
                    i.NextOutlinePollAtUtc <= now &&
                    i.OutlineProviderTrackId != null));

        return _context.AnalysisRequests.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
    }

    private async Task FailAnalysisPollingItemAsync(
        AnalysisContentNormalizedRequest request,
        AnalysisNormalizedItem item,
        string errorMessage,
        bool retryable,
        CancellationToken cancellationToken)
    {
        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set("Items.$.Status", "FAILED")
            .Set("Items.$.CurrentStep", EventNames.OutlineProviderPollingStarted)
            .Set("Items.$.OutlineStatus", "FAILED")
            .Set("Items.$.LastError", errorMessage)
            .Set("Items.$.NextOutlinePollAtUtc", (DateTime?)null)
            .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
            .Set(x => x.Status, "FAILED")
            .Set(x => x.CurrentStep, EventNames.OutlineProviderPollingStarted)
            .Set(x => x.LastError, errorMessage)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        await UpdateAnalysisItemAsync(
            request.Id,
            item.CustomerContentId,
            update,
            cancellationToken);

        await _eventBus.PublishAsync(new StepFailedEvent
        {
            CorrelationId = request.CorrelationId,
            AnalysisContentId = request.AnalysisContentId,
            CustomerContentId = item.CustomerContentId,
            ContentProcessType = ContentProcessTypes.AnalysisContent,
            Step = EventNames.OutlineProviderPollingStarted,
            ErrorMessage = errorMessage,
            Retryable = retryable
        }, cancellationToken);
    }

    private Task UpdateAnalysisItemAsync(
        Guid analysisRequestId,
        Guid customerContentId,
        UpdateDefinition<AnalysisContentNormalizedRequest> update,
        CancellationToken cancellationToken)
    {
        var filter = Builders<AnalysisContentNormalizedRequest>.Filter.And(
            Builders<AnalysisContentNormalizedRequest>.Filter.Eq(x => x.Id, analysisRequestId),
            Builders<AnalysisContentNormalizedRequest>.Filter.ElemMatch(
                x => x.Items,
                i => i.CustomerContentId == customerContentId));

        return _context.AnalysisRequests.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
    }

    private Task ReplaceCustomerAsync(
        CustomerContentNormalizedRequest request,
        CancellationToken cancellationToken)
    {
        return _context.CustomerRequests.ReplaceOneAsync(
            x => x.Id == request.Id,
            request,
            cancellationToken: cancellationToken);
    }
}