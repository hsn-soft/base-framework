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
                request.NextOutlinePollAtUtc = DateTime.UtcNow.AddMinutes(5);
                request.LastError = ex.Message;
                request.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceCustomerAsync(request, cancellationToken);

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
        var filter = Builders<AnalysisContentNormalizedRequest>.Filter.And(
            Builders<AnalysisContentNormalizedRequest>.Filter.Eq(x => x.Status, "OUTLINE_PROVIDER_POLLING"),
            Builders<AnalysisContentNormalizedRequest>.Filter.ElemMatch(x => x.Items,
                i => i.OutlineStatus == "POLLING" &&
                     i.NextOutlinePollAtUtc <= now &&
                     i.OutlineProviderTrackId != null)
        );

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
                    if (item.OutlinePollingCount >= item.MaxOutlinePollingCount)
                    {
                        item.OutlineStatus = "FAILED";
                        item.LastError = "Outline provider polling timeout.";
                        item.UpdatedAtUtc = DateTime.UtcNow;

                        request.LastError = item.LastError;
                        request.UpdatedAtUtc = DateTime.UtcNow;

                        await ReplaceAnalysisAsync(request, cancellationToken);

                        await _eventBus.PublishAsync(new StepFailedEvent
                        {
                            CorrelationId = request.CorrelationId,
                            AnalysisContentId = request.AnalysisContentId,
                            CustomerContentId = item.CustomerContentId,
                            ContentProcessType = ContentProcessTypes.AnalysisContent,
                            Step = EventNames.OutlineProviderPollingStarted,
                            ErrorMessage = item.LastError,
                            Retryable = false
                        }, cancellationToken);

                        continue;
                    }

                    var provider = _outlineProviderResolver.Resolve(request.OutlineProviderKey);

                    var status = await provider.GetStatusAsync(
                        item.OutlineProviderTrackId!,
                        cancellationToken);

                    if (status.IsFailed)
                    {
                        item.OutlineStatus = "FAILED";
                        item.LastError = status.ErrorMessage ?? "Outline provider failed.";
                        item.UpdatedAtUtc = DateTime.UtcNow;

                        request.LastError = item.LastError;
                        request.UpdatedAtUtc = DateTime.UtcNow;

                        await ReplaceAnalysisAsync(request, cancellationToken);

                        await _eventBus.PublishAsync(new StepFailedEvent
                        {
                            CorrelationId = request.CorrelationId,
                            AnalysisContentId = request.AnalysisContentId,
                            CustomerContentId = item.CustomerContentId,
                            ContentProcessType = ContentProcessTypes.AnalysisContent,
                            Step = EventNames.OutlineProviderPollingStarted,
                            ErrorMessage = item.LastError,
                            Retryable = false
                        }, cancellationToken);

                        continue;
                    }

                    if (!status.IsCompleted)
                    {
                        item.OutlinePollingCount++;
                        item.NextOutlinePollAtUtc = DateTime.UtcNow.AddMinutes(5);
                        item.UpdatedAtUtc = DateTime.UtcNow;

                        request.UpdatedAtUtc = DateTime.UtcNow;

                        await ReplaceAnalysisAsync(request, cancellationToken);
                        continue;
                    }

                    item.OutlinePollingCount++;
                    item.NextOutlinePollAtUtc = null;
                    item.OutlineStatus = "PROVIDER_COMPLETED";
                    item.UpdatedAtUtc = DateTime.UtcNow;

                    request.CurrentStep = EventNames.OutlineProviderCompleted;
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    if (request.Items.All(x =>
                            x.OutlineStatus is "PROVIDER_COMPLETED" or "COMPLETED"))
                    {
                        request.Status = "OUTLINE_PROVIDER_COMPLETED";
                    }

                    await ReplaceAnalysisAsync(request, cancellationToken);

                    if (string.IsNullOrWhiteSpace(status.Script))
                        throw new InvalidOperationException("Outline provider completed but script is empty.");

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
                    item.OutlinePollingCount++;
                    item.NextOutlinePollAtUtc = DateTime.UtcNow.AddMinutes(5);
                    item.LastError = ex.Message;
                    item.UpdatedAtUtc = DateTime.UtcNow;

                    request.LastError = ex.Message;
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceAnalysisAsync(request, cancellationToken);

                    _logger.LogError(
                        ex,
                        "Analysis outline item polling failed. AnalysisContentId: {AnalysisContentId}, CustomerContentId: {CustomerContentId}",
                        request.AnalysisContentId,
                        item.CustomerContentId);
                }
            }
        }
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

    private Task ReplaceAnalysisAsync(
        AnalysisContentNormalizedRequest request,
        CancellationToken cancellationToken)
    {
        return _context.AnalysisRequests.ReplaceOneAsync(
            x => x.Id == request.Id,
            request,
            cancellationToken: cancellationToken);
    }
}