using System.Linq.Expressions;
using HsnSoft.Base;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Xyz.ShipmentService.Application.Contracts.ShipmentDomain.Dtos;
using Xyz.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Filters;
using Xyz.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Submits;
using Xyz.ShipmentService.Application.Contracts.ShipmentDomain.Services;
using Xyz.ShipmentService.Domain.ShipmentDomain.Entities;
using Xyz.ShipmentService.Domain.ShipmentDomain.Exceptions;
using Xyz.ShipmentService.Domain.ShipmentDomain.Repositories;
using Xyz.ShipmentService.Domain.ShipmentHistoryDomain.Consts;
using Xyz.ShipmentService.Domain.ShipmentHistoryDomain.Entities;

namespace Xyz.ShipmentService.Application.Services;

public sealed class ShipmentAppService(IRepository<Shipment, Guid> shipments, IRepository<ShipmentHistory, Guid> history, IServiceProvider services) : ApplicationServiceBase(services), IShipmentAppService
{
    public async Task<ShipmentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await FindAsync(id, cancellationToken).ConfigureAwait(false));

    public async Task<PagedDataResultDto<ShipmentDto>> GetPagedListAsync(GetShipmentsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<Shipment> page = await shipments.GetPagedListAsync(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.OrderId, pagedInput.TrackingNumber, pagedInput.Carrier, ParseStatus(pagedInput.Status)), ShipmentSorting.Fields, ShipmentSorting.Default), cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, ToDto);
    }

    public async Task<List<ShipmentDto>> GetFilterListAsync(GetShipmentsFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        IReadOnlyList<Shipment> all = await shipments.GetListAsync(
            filterInput.ToListOptions(Filter(null, filterInput.OrderId, filterInput.TrackingNumber, filterInput.Carrier, ParseStatus(filterInput.Status)), ShipmentSorting.Fields, ShipmentSorting.Default), cancellationToken).ConfigureAwait(false);
        return [.. all.Select(ToDto)];
    }

    public async Task<List<ShipmentSearchDto>> GetSearchListAsync(GetShipmentsSearch searchInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchInput);

        IReadOnlyList<Shipment> found = await shipments.GetListAsync(
            searchInput.ToListOptions(Filter(searchInput.SearchText, null, null, null, null), ShipmentSorting.Fields, ShipmentSorting.Default), cancellationToken).ConfigureAwait(false);
        return [.. found.Select(shipment => new ShipmentSearchDto { Id = shipment.Id, TrackingNumber = shipment.TrackingNumber, Carrier = shipment.Carrier, Status = shipment.Status.ToString() })];
    }

    public async Task<ShipmentDto> CreateAsync(ShipmentCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Shipment shipment = Shipment.Create(input.OrderId.GetValueOrDefault(), input.Carrier, input.Address);
        await shipments.AddAsync(shipment, cancellationToken).ConfigureAwait(false);
        return ToDto(shipment);
    }

    public async Task UpdateAsync(ShipmentUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Shipment shipment = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        ShipmentStatus before = shipment.Status;
        shipment.Update(input.Carrier, input.Address, ParseStatus(input.Status) ?? throw new DomainException("Status is required.").WithKey("status_required"));
        await shipments.UpdateAsync(shipment, cancellationToken).ConfigureAwait(false);

        // how a delivery ended is written to the shipment history (its own database): delivered, or failed when the parcel was returned. There is no transaction over the two databases.
        if (shipment.Status != before && shipment.Status is ShipmentStatus.Delivered or ShipmentStatus.Returned)
        {
            string outcome = shipment.Status == ShipmentStatus.Delivered ? ShipmentHistoryConsts.Delivered : ShipmentHistoryConsts.Failed;
            await history.AddAsync(ShipmentHistory.Record(shipment.Id, shipment.OrderId, shipment.TrackingNumber, outcome), cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Shipment shipment = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        await shipments.DeleteAsync(shipment, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Shipment> FindAsync(Guid id, CancellationToken cancellationToken)
        => await shipments.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw ShipmentNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<Shipment, bool>> Filter(string? searchText, Guid? orderId, string? trackingNumber, string? carrier, ShipmentStatus? status)
    {
        string? search = Normalized(searchText);
        string? tracking = Normalized(trackingNumber);
        string? carrierText = Normalized(carrier);
        return new FilterBuilder<Shipment>()
            .And(search is null ? null : shipment => shipment.TrackingNumber.Contains(search) || shipment.NormalizedCarrier.Contains(search))
            .And(orderId is null ? null : shipment => shipment.OrderId == orderId.Value)
            .And(tracking is null ? null : shipment => shipment.TrackingNumber.Contains(tracking))
            .And(carrierText is null ? null : shipment => shipment.NormalizedCarrier.Contains(carrierText))
            .And(status is null ? null : shipment => shipment.Status == status.Value)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static ShipmentStatus? ParseStatus(string? status)
        => string.IsNullOrWhiteSpace(status)
            ? null
            : Enum.TryParse(status.Trim(), ignoreCase: true, out ShipmentStatus parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new DomainException($"'{status}' is not a status of a shipment (Preparing, InTransit, Delivered, Returned).").WithKey("shipment_status_invalid", status);

    private ShipmentDto ToDto(Shipment shipment) => new()
    {
        Id = shipment.Id,
        OrderId = shipment.OrderId,
        TrackingNumber = shipment.TrackingNumber,
        Carrier = shipment.Carrier,
        Address = shipment.Address,
        Status = shipment.Status.ToString(),
        StatusText = L[$"ShipmentStatus:{shipment.Status}"].Value,
        TenantId = shipment.TenantId,
        CreatedAt = shipment.CreatedAt,
        CreatedBy = shipment.CreatedBy,
        UpdatedAt = shipment.UpdatedAt,
        UpdatedBy = shipment.UpdatedBy,
    };
}
