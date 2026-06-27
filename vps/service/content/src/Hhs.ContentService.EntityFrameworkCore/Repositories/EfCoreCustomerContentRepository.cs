using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Exceptions;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.Domain.Localization;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Helper;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreCustomerContentRepository(
    IServiceProvider provider,
    IStringLocalizerFactory stringLocalizerFactory,
    ContentServiceDbContext dbContext
) : EfCoreGenericRepository<CustomerContent, Guid>(provider, dbContext), ICustomerContentRepository
{
    [NotNull]
    protected IStringLocalizer L { get; } = stringLocalizerFactory.CreateMultiple
    (
        [
            typeof(ContentServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        ]
    );

    public async Task SetNormalizedReferenceAsync(Guid id, Guid normalizedRequestId)
        => await UpdateByExpressionAsync(x => x.Id == id && x.NormalizeRequestId == null,
            s => s
                .SetProperty(a => a.NormalizeRequestId, normalizedRequestId)
                .SetProperty(a => a.NormalizeStatus, StatusNames.Created)
                .SetProperty(a => a.LastFacility, EventNames.AnalysisContentNormalizeRequestCreated)
        );

    public async Task SetScrapeTimeAsync(Guid id, DateTime? scrapeTime)
    {
        if (scrapeTime.HasValue && scrapeTime.Value == default)
        {
            scrapeTime = null;
        }

        if (scrapeTime.HasValue && scrapeTime.Value.Kind != DateTimeKind.Utc)
        {
            scrapeTime = scrapeTime.Value.ToUniversalTime();
        }

        await UpdateByExpressionAsync(x => x.Id == id && x.ScrapReleaseTimeUtc == null,
            s => s
                .SetProperty(a => a.ScrapReleaseTimeUtc, scrapeTime)
        );
    }

    public async Task SetVideoReferenceAsync(Guid id, Guid videoRequestId)
        => await UpdateByExpressionAsync(x => x.Id == id && x.VideoRequestId == null,
            s => s
                .SetProperty(a => a.VideoRequestId, videoRequestId)
                .SetProperty(a => a.VideoStatus, StatusNames.Created)
                .SetProperty(a => a.LastFacility, EventNames.VideoRequestCreated)
        );

    public async Task SetVideoGenerationApprovedAsync(Guid id) =>
        await UpdateByExpressionAsync(x => x.Id == id && x.VideoRequestId == null,
            s => s
                .SetProperty(a => a.NormalizeStatus, StatusNames.Completed)
                .SetProperty(a => a.LastFacility, EventNames.NormalizerResultPublished)
                .SetProperty(a => a.LastError, (string)null)
                .SetProperty(a => a.VideoStatus, StatusNames.Approved)
        );

    public async Task SetVideoGenerationRejectedAsync(Guid id, [CanBeNull] string rejectReason) =>
        await UpdateByExpressionAsync(x => x.Id == id && x.VideoRequestId == null,
            s => s
                .SetProperty(a => a.NormalizeStatus, StatusNames.Completed)
                .SetProperty(a => a.LastFacility, EventNames.NormalizerResultPublished)
                .SetProperty(a => a.LastError, rejectReason)
                .SetProperty(a => a.VideoStatus, StatusNames.Rejected)
        );

    public async Task<CustomerContent> CreateAsync(string scopeKey, string contentKey, string correlationId = null)
    {
        // Create draft
        var draft = new CustomerContent(
            id: Guid.CreateVersion7(),
            scopeKey: scopeKey,
            contentKey: contentKey,
            correlationId: correlationId
        );

        //Domain Rules
        await ContentDuplicateControlAsync(scopeKey: draft.ScopeKey, draft.SlugKey);
        _ = await InsertAsync(draft);
        return draft;
    }


    public async Task<CustomerContent> GetByScopeKeyAndSlugKeyAsync(string scopeKey, string slugKey, CancellationToken cancellationToken = default)
        => await GetFirstOrDefaultAsync(
            x => x.ScopeKey == scopeKey && x.SlugKey == slugKey,
            cancellationToken: cancellationToken
        );

    public async Task<CustomerContent> GetByIdWithTrackingAsync(Guid id, CancellationToken cancellationToken = default)
        => await GetFirstOrDefaultAsync(
            x => x.Id == id,
            q => q.AsTracking(),
            cancellationToken: cancellationToken
        );

    public async Task<List<CustomerContent>> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<CustomerContent> { Filter = x => x.ScopeKey == scopeKey };
        return await GetListAsync(options, cancellationToken);
    }





    public Task<List<Guid>> GetCustomerDailyTrendContentIdsAsync(string scopeKey, ushort dailyTrendVideoWaitStatisticHour, CancellationToken cancellationToken = default)
    {
        // TODO: query uses OperationStatus (enum) which was replaced with string-based VideoStatus.
        // Re-implement once the video status transition model is finalised.
        return Task.FromResult(new List<Guid>());
    }

    public Task<List<Guid>> GetCustomerDailyAnalysisContentIdsAsync(string scopeKey, CancellationToken cancellationToken = default)
    {
        // TODO: query uses OperationStatus (enum) which was replaced with string-based NormalizeStatus.
        // Re-implement once the content status query requirements are finalised.
        return Task.FromResult(new List<Guid>());
    }





    private async Task ContentDuplicateControlAsync([NotNull] string scopeKey, [NotNull] string slugKey)
    {
        var old = await GetSingleOrDefaultAsync(x => x.ScopeKey == scopeKey && x.SlugKey == slugKey);
        if (old != null)
        {
            throw new CustomerContentDuplicateException(L, old.Id.ToString())
                .WithData(nameof(scopeKey), scopeKey)
                .WithData(nameof(slugKey), slugKey);
        }
    }
}