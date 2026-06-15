using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.TextNormalizerService.Entities;
using Hhs.TextNormalizerService.Mongo;
using Hhs.TextNormalizerService.Providers;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Services;

public sealed class NormalizerOperationAppService
{
    private readonly NormalizerMongoContext _context;
    private readonly IContentScraper _scraper;
    private readonly IOutlineProvider _outlineProvider;
    private readonly IEventBus _eventBus;
    private readonly ILogger<NormalizerOperationAppService> _logger;

    public NormalizerOperationAppService(
        NormalizerMongoContext context,
        IContentScraper scraper,
        IOutlineProvider outlineProvider,
        IEventBus eventBus,
        ILogger<NormalizerOperationAppService> logger)
    {
        _context = context;
        _scraper = scraper;
        _outlineProvider = outlineProvider;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task CreateCustomerNormalizeRequestAsync(
        CustomerNormalizeRequestCreatedEvent @event,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid();

        await _context.CustomerRequests.InsertOneAsync(new CustomerContentNormalizedRequest
        {
            Id = requestId,
            CustomerContentId = @event.CustomerContentId!.Value,
            Url = @event.Url,
            Status = "CREATED",
            CurrentStep = EventNames.CustomerNormalizeRequestCreated,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        }, cancellationToken: cancellationToken);

        await _eventBus.PublishAsync(new CustomerScrapingStartedEvent
        {
            CustomerContentId = @event.CustomerContentId,
            CorrelationId = @event.CorrelationId
        }, cancellationToken);
    }

    public async Task StartCustomerScrapingAsync(
        CustomerScrapingStartedEvent @event,
        CancellationToken cancellationToken)
    {
        var request = await _context.CustomerRequests
            .Find(x => x.CustomerContentId == @event.CustomerContentId)
            .FirstAsync(cancellationToken);

        try
        {
            request.Status = "SCRAPING";
            request.ScrapingStatus = "STARTED";
            request.CurrentStep = EventNames.CustomerScrapingStarted;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);

            var result = await _scraper.ScrapeAsync(request.Url, cancellationToken);

            request.Status = "SCRAPING_COMPLETED";
            request.ScrapingStatus = "COMPLETED";
            request.CurrentStep = EventNames.CustomerScrapingCompleted;
            request.ScrapingResult = new ScrapingResult
            {
                Title = result.Title,
                Text = result.Text,
                ReleaseTimeUtc = result.ReleaseTimeUtc
            };
            request.LastError = null;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);

            await _eventBus.PublishAsync(new CustomerScrapingCompletedEvent
            {
                CustomerContentId = request.CustomerContentId,
                CorrelationId = @event.CorrelationId,
                Title = result.Title,
                Text = result.Text,
                ReleaseTimeUtc = result.ReleaseTimeUtc
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await FailCustomerAsync(request, EventNames.CustomerScrapingStarted, ex, cancellationToken);
            throw;
        }
    }

    public async Task CompleteCustomerScrapingAsync(
        CustomerScrapingCompletedEvent @event,
        CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(new CustomerOutlineStartedEvent
        {
            CustomerContentId = @event.CustomerContentId,
            CorrelationId = @event.CorrelationId
        }, cancellationToken);
    }

    public async Task StartCustomerOutlineAsync(
        CustomerOutlineStartedEvent @event,
        CancellationToken cancellationToken)
    {
        var request = await _context.CustomerRequests
            .Find(x => x.CustomerContentId == @event.CustomerContentId)
            .FirstAsync(cancellationToken);

        try
        {
            request.Status = "OUTLINE";
            request.OutlineStatus = "STARTED";
            request.CurrentStep = EventNames.CustomerOutlineStarted;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);

            var script = await _outlineProvider.CreateOutlineAsync(
                request.ScrapingResult!.Text,
                cancellationToken);

            request.Status = "OUTLINE_COMPLETED";
            request.OutlineStatus = "COMPLETED";
            request.CurrentStep = EventNames.CustomerOutlineCompleted;
            request.OutlineResult = new OutlineResult
            {
                Script = script
            };
            request.LastError = null;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceCustomerAsync(request, cancellationToken);

            await _eventBus.PublishAsync(new CustomerOutlineCompletedEvent
            {
                CustomerContentId = request.CustomerContentId,
                CorrelationId = @event.CorrelationId,
                Script = script
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await FailCustomerAsync(request, EventNames.CustomerOutlineStarted, ex, cancellationToken);
            throw;
        }
    }

    public async Task CompleteCustomerOutlineAsync(
        CustomerOutlineCompletedEvent @event,
        CancellationToken cancellationToken)
    {
        var request = await _context.CustomerRequests
            .Find(x => x.CustomerContentId == @event.CustomerContentId)
            .FirstAsync(cancellationToken);

        var videoInput = new
        {
            type = "customer",
            customerContentId = request.CustomerContentId,
            title = request.ScrapingResult?.Title,
            script = request.OutlineResult?.Script,
            audioItems = new[]
            {
                new
                {
                    customerContentId = request.CustomerContentId,
                    sortOrder = 1,
                    text = request.OutlineResult?.Script
                }
            }
        };

        request.Status = "COMPLETED";
        request.CurrentStep = EventNames.NormalizerResultPublished;
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceCustomerAsync(request, cancellationToken);

        await _eventBus.PublishAsync(new NormalizerResultPublishedEvent
        {
            CustomerContentId = request.CustomerContentId,
            CorrelationId = @event.CorrelationId,
            ContentProcessType = ContentProcessTypes.CustomerContent,
            VideoInputJson = System.Text.Json.JsonSerializer.Serialize(videoInput)
        }, cancellationToken);
    }

    public async Task CreateAnalysisNormalizeRequestAsync(
        AnalysisNormalizeRequestCreatedEvent @event,
        CancellationToken cancellationToken)
    {
        var request = new AnalysisContentNormalizedRequest
        {
            Id = Guid.NewGuid(),
            AnalysisContentId = @event.AnalysisContentId!.Value,
            Status = "CREATED",
            CurrentStep = EventNames.AnalysisNormalizeRequestCreated,
            Items = @event.Items.Select(x => new AnalysisNormalizedItem
            {
                CustomerContentId = x.CustomerContentId,
                SortOrder = x.SortOrder,
                Url = x.Url,
                Path = x.Path
            }).ToList(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _context.AnalysisRequests.InsertOneAsync(request, cancellationToken: cancellationToken);

        foreach (var item in request.Items.OrderBy(x => x.SortOrder))
        {
            await _eventBus.PublishAsync(new AnalysisItemScrapingStartedEvent
            {
                AnalysisContentId = request.AnalysisContentId,
                CustomerContentId = item.CustomerContentId,
                SortOrder = item.SortOrder,
                CorrelationId = @event.CorrelationId
            }, cancellationToken);
        }
    }

    public async Task StartAnalysisItemScrapingAsync(
        AnalysisItemScrapingStartedEvent @event,
        CancellationToken cancellationToken)
    {
        var request = await GetAnalysisAsync(@event.AnalysisContentId!.Value, cancellationToken);
        var item = request.Items.First(x => x.CustomerContentId == @event.CustomerContentId);

        try
        {
            item.ScrapingStatus = "STARTED";
            item.UpdatedAtUtc = DateTime.UtcNow;
            request.Status = "SCRAPING";
            request.CurrentStep = EventNames.AnalysisItemScrapingStarted;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAnalysisAsync(request, cancellationToken);

            var result = await _scraper.ScrapeAsync(item.Url, cancellationToken);

            item.ScrapingStatus = "COMPLETED";
            item.ScrapingResult = new ScrapingResult
            {
                Title = result.Title,
                Text = result.Text,
                ReleaseTimeUtc = result.ReleaseTimeUtc
            };
            item.LastError = null;
            item.UpdatedAtUtc = DateTime.UtcNow;

            request.CurrentStep = EventNames.AnalysisItemScrapingCompleted;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAnalysisAsync(request, cancellationToken);

            await _eventBus.PublishAsync(new AnalysisItemScrapingCompletedEvent
            {
                AnalysisContentId = request.AnalysisContentId,
                CustomerContentId = item.CustomerContentId,
                SortOrder = item.SortOrder,
                CorrelationId = @event.CorrelationId,
                Title = result.Title,
                Text = result.Text,
                ReleaseTimeUtc = result.ReleaseTimeUtc
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            item.ScrapingStatus = "FAILED";
            item.LastError = ex.Message;
            request.LastError = ex.Message;
            await ReplaceAnalysisAsync(request, cancellationToken);
            throw;
        }
    }

    public async Task CompleteAnalysisItemScrapingAsync(
        AnalysisItemScrapingCompletedEvent @event,
        CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(new AnalysisItemOutlineStartedEvent
        {
            AnalysisContentId = @event.AnalysisContentId,
            CustomerContentId = @event.CustomerContentId,
            SortOrder = @event.SortOrder,
            CorrelationId = @event.CorrelationId
        }, cancellationToken);
    }

    public async Task StartAnalysisItemOutlineAsync(
        AnalysisItemOutlineStartedEvent @event,
        CancellationToken cancellationToken)
    {
        var request = await GetAnalysisAsync(@event.AnalysisContentId!.Value, cancellationToken);
        var item = request.Items.First(x => x.CustomerContentId == @event.CustomerContentId);

        try
        {
            item.OutlineStatus = "STARTED";
            item.UpdatedAtUtc = DateTime.UtcNow;
            request.Status = "OUTLINE";
            request.CurrentStep = EventNames.AnalysisItemOutlineStarted;

            await ReplaceAnalysisAsync(request, cancellationToken);

            var script = await _outlineProvider.CreateOutlineAsync(
                item.ScrapingResult!.Text,
                cancellationToken);

            item.OutlineStatus = "COMPLETED";
            item.OutlineResult = new OutlineResult { Script = script };
            item.LastError = null;
            item.UpdatedAtUtc = DateTime.UtcNow;

            request.CurrentStep = EventNames.AnalysisItemOutlineCompleted;
            request.UpdatedAtUtc = DateTime.UtcNow;

            await ReplaceAnalysisAsync(request, cancellationToken);

            await _eventBus.PublishAsync(new AnalysisItemOutlineCompletedEvent
            {
                AnalysisContentId = request.AnalysisContentId,
                CustomerContentId = item.CustomerContentId,
                SortOrder = item.SortOrder,
                CorrelationId = @event.CorrelationId,
                Script = script
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            item.OutlineStatus = "FAILED";
            item.LastError = ex.Message;
            request.LastError = ex.Message;
            await ReplaceAnalysisAsync(request, cancellationToken);
            throw;
        }
    }

    public async Task CompleteAnalysisItemOutlineAsync(
        AnalysisItemOutlineCompletedEvent @event,
        CancellationToken cancellationToken)
    {
        var request = await GetAnalysisAsync(@event.AnalysisContentId!.Value, cancellationToken);

        if (request.Items.Any(x => x.OutlineStatus != "COMPLETED"))
        {
            return;
        }

        var videoInput = new
        {
            type = "analysis",
            analysisContentId = request.AnalysisContentId,
            audioItems = request.Items
                .OrderBy(x => x.SortOrder)
                .Select(x => new
                {
                    customerContentId = x.CustomerContentId,
                    sortOrder = x.SortOrder,
                    title = x.ScrapingResult?.Title,
                    text = x.OutlineResult?.Script
                })
                .ToList()
        };

        request.Status = "COMPLETED";
        request.CurrentStep = EventNames.NormalizerResultPublished;
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceAnalysisAsync(request, cancellationToken);

        await _eventBus.PublishAsync(new NormalizerResultPublishedEvent
        {
            AnalysisContentId = request.AnalysisContentId,
            CorrelationId = @event.CorrelationId,
            ContentProcessType = ContentProcessTypes.AnalysisContent,
            VideoInputJson = System.Text.Json.JsonSerializer.Serialize(videoInput)
        }, cancellationToken);
    }

    public async Task CompleteCustomerScrapingManuallyAsync(
        Guid customerContentId,
        ManualScrapingInput input,
        CancellationToken cancellationToken)
    {
        var request = await _context.CustomerRequests
            .Find(x => x.CustomerContentId == customerContentId)
            .FirstAsync(cancellationToken);

        request.ScrapingStatus = "COMPLETED";
        request.Status = "SCRAPING_COMPLETED";
        request.CurrentStep = EventNames.CustomerScrapingCompleted;
        request.ScrapingResult = new ScrapingResult
        {
            Title = input.Title,
            Text = input.Text,
            ReleaseTimeUtc = input.ReleaseTimeUtc,
            Source = "MANUAL"
        };
        request.LastError = null;
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceCustomerAsync(request, cancellationToken);

        await _eventBus.PublishAsync(new CustomerScrapingCompletedEvent
        {
            CustomerContentId = customerContentId,
            CorrelationId = input.CorrelationId,
            Title = input.Title,
            Text = input.Text,
            ReleaseTimeUtc = input.ReleaseTimeUtc,
            IsManual = true
        }, cancellationToken);
    }

    private Task ReplaceCustomerAsync(CustomerContentNormalizedRequest request, CancellationToken cancellationToken)
    {
        return _context.CustomerRequests.ReplaceOneAsync(
            x => x.Id == request.Id,
            request,
            cancellationToken: cancellationToken);
    }

    private Task ReplaceAnalysisAsync(AnalysisContentNormalizedRequest request, CancellationToken cancellationToken)
    {
        return _context.AnalysisRequests.ReplaceOneAsync(
            x => x.Id == request.Id,
            request,
            cancellationToken: cancellationToken);
    }

    private Task<AnalysisContentNormalizedRequest> GetAnalysisAsync(Guid analysisContentId, CancellationToken cancellationToken)
    {
        return _context.AnalysisRequests
            .Find(x => x.AnalysisContentId == analysisContentId)
            .FirstAsync(cancellationToken);
    }

    private async Task FailCustomerAsync(
        CustomerContentNormalizedRequest request,
        string step,
        Exception ex,
        CancellationToken cancellationToken)
    {
        request.Status = "FAILED";
        request.CurrentStep = step;
        request.LastError = ex.Message;
        request.RetryCount++;
        request.NextRetryAtUtc = DateTime.UtcNow.AddMinutes(1);
        request.UpdatedAtUtc = DateTime.UtcNow;

        await ReplaceCustomerAsync(request, cancellationToken);

        await _eventBus.PublishAsync(new StepFailedEvent
        {
            CustomerContentId = request.CustomerContentId,
            ContentProcessType = ContentProcessTypes.CustomerContent,
            Step = step,
            ErrorMessage = ex.Message,
            Retryable = true
        }, cancellationToken);
    }
}

public sealed class ManualScrapingInput
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = default!;
    public string Text { get; set; } = default!;
    public DateTime? ReleaseTimeUtc { get; set; }
}