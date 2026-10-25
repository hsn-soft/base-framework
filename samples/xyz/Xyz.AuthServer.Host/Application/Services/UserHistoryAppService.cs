using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Xyz.AuthServer.Application.Contracts.UserHistoryDomain.Dtos;
using Xyz.AuthServer.Application.Contracts.UserHistoryDomain.Dtos.Filters;
using Xyz.AuthServer.Application.Contracts.UserHistoryDomain.Dtos.Submits;
using Xyz.AuthServer.Application.Contracts.UserHistoryDomain.Services;
using Xyz.AuthServer.Domain.UserHistoryDomain.Entities;
using Xyz.AuthServer.Domain.UserHistoryDomain.Exceptions;
using Xyz.AuthServer.Domain.UserHistoryDomain.Repositories;

namespace Xyz.AuthServer.Application.Services;

public sealed class UserHistoryAppService(IRepository<UserHistory, Guid> histories, IServiceProvider services) : ApplicationServiceBase(services), IUserHistoryAppService
{
    // the lists read the columns of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<UserHistoryDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await histories.FindAsync<UserHistoryDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw UserHistoryNotFound.For(id);

    public async Task<PagedDataResultDto<UserHistoryDto>> GetPagedListAsync(GetUserHistoriesPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<UserHistoryDto> page = await histories.GetPagedListAsync<UserHistoryDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.UserId, pagedInput.UserName, pagedInput.Action), UserHistorySorting.Fields, UserHistorySorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<List<UserHistoryDto>> GetFilterListAsync(GetUserHistoriesFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        return [.. await histories.GetListAsync<UserHistoryDto>(
            filterInput.ToListOptions(Filter(null, filterInput.UserId, filterInput.UserName, filterInput.Action), UserHistorySorting.Fields, UserHistorySorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false)];
    }

    public async Task<List<UserHistorySearchDto>> GetSearchListAsync(GetUserHistoriesSearch searchInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchInput);

        return [.. await histories.GetListAsync<UserHistorySearchDto>(
            searchInput.ToListOptions(Filter(searchInput.SearchText, null, null, null), UserHistorySorting.Fields, UserHistorySorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false)];
    }

    public async Task<UserHistoryDto> CreateAsync(UserHistoryCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        UserHistory history = UserHistory.Record(input.UserId.GetValueOrDefault(), input.UserName!, input.Action!, input.Note);
        await histories.AddAsync(history, cancellationToken).ConfigureAwait(false);
        return await GetAsync(history.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(UserHistoryUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        UserHistory history = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        history.Update(input.Action!, input.Note);
        await histories.UpdateAsync(history, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await histories.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<UserHistory> FindAsync(Guid id, CancellationToken cancellationToken)
        => await histories.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw UserHistoryNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<UserHistory, bool>> Filter(string? searchText, Guid? userId, string? userName, string? action)
    {
        string? search = Normalized(searchText);
        string? nameText = Normalized(userName);
        string? actionText = string.IsNullOrWhiteSpace(action) ? null : action.Trim().ToLowerInvariant();
        string? actionPart = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLowerInvariant();
        return new FilterBuilder<UserHistory>()
            .And(search is null ? null : history => history.NormalizedUserName.Contains(search) || history.Action.Contains(actionPart!))
            .And(userId is null ? null : history => history.UserId == userId.Value)
            .And(nameText is null ? null : history => history.NormalizedUserName.Contains(nameText))
            .And(actionText is null ? null : history => history.Action == actionText)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);
}
