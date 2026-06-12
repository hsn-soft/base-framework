using Hhs.TextNormalizerService.Application.Contracts.DashboardDomain;
using Hhs.TextNormalizerService.Application.Contracts.DashboardDomain.Dtos;
using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.ContentDomain.Repositories;
using HsnSoft.Base.Domain.Models;

namespace Hhs.TextNormalizerService.Application.Services;

public sealed class DashboardAppService : ApplicationServiceBase, IDashboardAppService
{
    private readonly INormalizedRequestRepository _normalizedRequestRepository;

    public DashboardAppService(
        IServiceProvider provider,
        INormalizedRequestRepository normalizedRequestRepository
    ) : base(provider)
    {
        _normalizedRequestRepository = normalizedRequestRepository;
    }

    public async Task<GetContentTextDetailResultDto> GetContentTextDetailResultDtoAsync(GetContentTextDetailRequestDto input, CancellationToken cancellationToken = default)
    {
        var hasSearchKeyword = !string.IsNullOrWhiteSpace(input.SearchKeyword);
        var searchKeyword = input.SearchKeyword?.Trim().ToLowerInvariant();

        var items = await _normalizedRequestRepository.GetListAsync(new ListQueryOptions<NormalizedRequest>
            {
                Filter = x =>
                    x.CreationTime >= input.StartDate
                    && x.CreationTime <= input.EndDate
                    && (!hasSearchKeyword
                        || (x.ScrapingContentData != null && x.ScrapingContentData.Title != null && x.ScrapingContentData.Title.ToLower().Contains(searchKeyword))
                        || (x.OutlineContentData != null && x.OutlineContentData.ToLower().Contains(searchKeyword))),
                OrderByDynamic = $"{nameof(NormalizedRequest.CreationTime)} desc"
            });

        return new GetContentTextDetailResultDto
        {
            TotalCount = items?.Count ?? 0,
            Items = items?.Select(x => new ContentItemDto
            {
                Id = x.AppContentId,
                Title = x.ScrapingContentData?.Title,
                Spot = x.ScrapingContentData?.Spot,
                OperationStatus = x.OperationStatus.ToString(),
                ReleaseTime = x.ScrapingContentData?.ReleaseTime ?? x.CreationTime,
                Summary = x.OutlineContentData,
                ImageUrl = x.ScrapingContentData?.ImageUrl,
                ContentUrl = x.DomainName + x.DomainPath
            }).ToList() ?? []
        };
    }
}
