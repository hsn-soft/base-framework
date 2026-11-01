using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Dtos;
using Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Dtos.Filters;
using Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Dtos.Submits;
using Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Services;
using Hsx.VideoGeneratorService.Domain.VideoRequestDomain.Entities;
using Hsx.VideoGeneratorService.Domain.VideoRequestDomain.Exceptions;
using Hsx.VideoGeneratorService.Domain.VideoRequestDomain.Repositories;

namespace Hsx.VideoGeneratorService.Application.Services;

public sealed class VideoRequestAppService(IRepository<VideoRequest, Guid> repository, IServiceProvider services) : ApplicationServiceBase(services), IVideoRequestAppService
{
    // the lists read the fields of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<VideoRequestDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.FindAsync<VideoRequestDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw VideoRequestNotFound.For(id);

    public async Task<PagedDataResultDto<VideoRequestDto>> GetPagedListAsync(GetVideoRequestsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<VideoRequestDto> page = await repository.GetPagedListAsync<VideoRequestDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.ScopeKey, pagedInput.RefContentId, pagedInput.RefContentType, pagedInput.Status), VideoRequestSorting.Fields, VideoRequestSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<VideoRequestDto> CreateAsync(VideoRequestCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        EnsureScope(input.ScopeKey!);

        VideoRequest entity = VideoRequest.Create(input.ScopeKey!, input.RefContentId.GetValueOrDefault(), input.RefContentType!, input.Status!, input.CurrentMilestone!, input.MediaInputJson!, input.AudioProviderKey, input.VideoProviderKey!, input.VideoProviderTrackingId, input.VideoProviderUrl, input.VideoStorageUrl, input.VideoCdnUrl, input.RetryCount, input.LastError);
        await repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return await GetAsync(entity.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(VideoRequestUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        VideoRequest entity = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        entity.Update(input.Status!, input.CurrentMilestone!, input.MediaInputJson!, input.AudioProviderKey, input.VideoProviderKey!, input.VideoProviderTrackingId, input.VideoProviderUrl, input.VideoStorageUrl, input.VideoCdnUrl, input.RetryCount, input.LastError);
        await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await repository.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<VideoRequest> FindAsync(Guid id, CancellationToken cancellationToken)
        => await repository.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw VideoRequestNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<VideoRequest, bool>> Filter(string? searchText, string? scopeKey, Guid? refContentId, string? refContentType, string? status)
    {
        string? search = Normalized(searchText);
        string? scopeKeyText = Trimmed(scopeKey);
        string? refContentTypeText = Trimmed(refContentType);
        string? statusText = Trimmed(status);
        return new FilterBuilder<VideoRequest>()
            .And(scopeKeyText is null ? null : entity => entity.ScopeKey == scopeKeyText)
            .And(refContentId is null ? null : entity => entity.RefContentId == refContentId.Value)
            .And(refContentTypeText is null ? null : entity => entity.RefContentType == refContentTypeText)
            .And(statusText is null ? null : entity => entity.Status == statusText)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static string? Trimmed(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
