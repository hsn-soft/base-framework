using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Xyz.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Dtos;
using Xyz.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Dtos.Filters;
using Xyz.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Dtos.Submits;
using Xyz.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Services;
using Xyz.ShipmentService.Domain.ShipmentHistoryDomain.Entities;
using Xyz.ShipmentService.Domain.ShipmentHistoryDomain.Exceptions;
using Xyz.ShipmentService.Domain.ShipmentHistoryDomain.Repositories;

namespace Xyz.ShipmentService.Application.Services;

public sealed class ShipmentHistoryAppService(IRepository<ShipmentHistory, Guid> histories, IServiceProvider services) : ApplicationServiceBase(services), IShipmentHistoryAppService
{
    // the lists read the columns of the DTO only (the select is made from it, no mapper): the entity is loaded only to be changed
    public async Task<ShipmentHistoryDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await histories.FindAsync<ShipmentHistoryDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw ShipmentHistoryNotFound.For(id);

    public async Task<PagedDataResultDto<ShipmentHistoryDto>> GetPagedListAsync(GetShipmentHistoriesPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<ShipmentHistoryDto> page = await histories.GetPagedListAsync<ShipmentHistoryDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.ShipmentId, pagedInput.OrderId, pagedInput.TrackingNumber, pagedInput.Outcome), ShipmentHistorySorting.Fields, ShipmentHistorySorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<List<ShipmentHistoryDto>> GetFilterListAsync(GetShipmentHistoriesFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        return [.. await histories.GetListAsync<ShipmentHistoryDto>(
            filterInput.ToListOptions(Filter(null, filterInput.ShipmentId, filterInput.OrderId, filterInput.TrackingNumber, filterInput.Outcome), ShipmentHistorySorting.Fields, ShipmentHistorySorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false)];
    }

    public async Task<List<ShipmentHistorySearchDto>> GetSearchListAsync(GetShipmentHistoriesSearch searchInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchInput);

        return [.. await histories.GetListAsync<ShipmentHistorySearchDto>(
            searchInput.ToListOptions(Filter(searchInput.SearchText, null, null, null, null), ShipmentHistorySorting.Fields, ShipmentHistorySorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false)];
    }

    public async Task<ShipmentHistoryDto> CreateAsync(ShipmentHistoryCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ShipmentHistory history = ShipmentHistory.Record(input.ShipmentId.GetValueOrDefault(), input.OrderId.GetValueOrDefault(), input.TrackingNumber!, input.Outcome!, input.Note);
        await histories.AddAsync(history, cancellationToken).ConfigureAwait(false);
        return await GetAsync(history.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ShipmentHistoryUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ShipmentHistory history = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        history.Update(input.Outcome!, input.Note);
        await histories.UpdateAsync(history, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await histories.DeleteAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async Task<ShipmentHistory> FindAsync(Guid id, CancellationToken cancellationToken)
        => await histories.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw ShipmentHistoryNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<ShipmentHistory, bool>> Filter(string? searchText, Guid? shipmentId, Guid? orderId, string? trackingNumber, string? outcome)
    {
        string? search = Normalized(searchText);
        string? trackingText = Normalized(trackingNumber);
        string? outcomeText = string.IsNullOrWhiteSpace(outcome) ? null : outcome.Trim().ToLowerInvariant();
        string? outcomePart = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLowerInvariant();
        return new FilterBuilder<ShipmentHistory>()
            .And(search is null ? null : history => history.TrackingNumber.Contains(search) || history.Outcome.Contains(outcomePart!))
            .And(shipmentId is null ? null : history => history.ShipmentId == shipmentId.Value)
            .And(orderId is null ? null : history => history.OrderId == orderId.Value)
            .And(trackingText is null ? null : history => history.TrackingNumber.Contains(trackingText))
            .And(outcomeText is null ? null : history => history.Outcome == outcomeText)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);
}
