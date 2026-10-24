using Abc.ShipmentService.Application.Contracts.Dtos;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Filters;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Dtos.Submits;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Services;
using Abc.ShipmentService.Application.Security;
using Abc.ShipmentService.Domain.ShipmentDomain.Entities;
using Abc.ShipmentService.Domain.ShipmentDomain.Exceptions;
using Abc.ShipmentService.Domain.ShipmentDomain.Repositories;
using HsnSoft.Base;

namespace Abc.ShipmentService.Application.Services;

public sealed class ShipmentAppService(IShipmentRepository shipments, TimeProvider clock, ICurrentUser currentUser) : IShipmentAppService
{
    public async Task<ShipmentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await FindAsync(id, cancellationToken).ConfigureAwait(false));

    public async Task<PagedDataResultDto<ShipmentDto>> GetPagedListAsync(GetShipmentsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        ShipmentFilter filter = new(pagedInput.OrderId, pagedInput.TrackingNumber, pagedInput.Carrier, ParseStatus(pagedInput.Status));
        IReadOnlyList<Shipment> page = await shipments.GetListAsync(filter, pagedInput.Skip, pagedInput.Take, cancellationToken).ConfigureAwait(false);
        long total = await shipments.CountAsync(filter, cancellationToken).ConfigureAwait(false);
        return new PagedDataResultDto<ShipmentDto> { Items = [.. page.Select(ToDto)], TotalCount = (int)total, Skip = pagedInput.Skip, Take = pagedInput.Take };
    }

    public async Task<List<ShipmentDto>> GetFilterListAsync(GetShipmentsFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        ShipmentFilter filter = new(filterInput.OrderId, filterInput.TrackingNumber, filterInput.Carrier, ParseStatus(filterInput.Status));
        IReadOnlyList<Shipment> all = await shipments.GetListAsync(filter, 0, filterInput.Limit, cancellationToken).ConfigureAwait(false);
        return [.. all.Select(ToDto)];
    }

    public async Task<List<ShipmentSearchDto>> GetSearchListAsync(GetShipmentsSearch searchInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchInput);

        IReadOnlyList<Shipment> found = await shipments.SearchAsync(searchInput.SearchText, searchInput.Limit, cancellationToken).ConfigureAwait(false);
        return [.. found.Select(shipment => new ShipmentSearchDto { Id = shipment.Id, TrackingNumber = shipment.TrackingNumber, Carrier = shipment.Carrier, Status = shipment.Status.ToString() })];
    }

    public async Task<ShipmentDto> CreateAsync(ShipmentCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Shipment shipment = Shipment.Create(input.OrderId, input.Carrier, input.Address, clock.GetUtcNow(), currentUser.UserName);
        await shipments.AddAsync(shipment, cancellationToken).ConfigureAwait(false);
        return ToDto(shipment);
    }

    public async Task UpdateAsync(ShipmentUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Shipment shipment = await FindAsync(input.Id, cancellationToken).ConfigureAwait(false);
        shipment.Update(input.Carrier, input.Address, ParseStatus(input.Status) ?? throw new DomainException("Status is required."), clock.GetUtcNow(), currentUser.UserName);
        await shipments.UpdateAsync(shipment, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Shipment shipment = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        await shipments.DeleteAsync(shipment, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Shipment> FindAsync(Guid id, CancellationToken cancellationToken)
        => await shipments.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw ShipmentNotFound.For(id);

    private static ShipmentStatus? ParseStatus(string? status)
        => string.IsNullOrWhiteSpace(status)
            ? null
            : Enum.TryParse(status.Trim(), ignoreCase: true, out ShipmentStatus parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new DomainException($"'{status}' is not a status of a shipment (Preparing, InTransit, Delivered, Returned).");

    private static ShipmentDto ToDto(Shipment shipment) => new()
    {
        Id = shipment.Id,
        OrderId = shipment.OrderId,
        TrackingNumber = shipment.TrackingNumber,
        Carrier = shipment.Carrier,
        Address = shipment.Address,
        Status = shipment.Status.ToString(),
        CreatedAt = shipment.CreatedAt,
        CreatedBy = shipment.CreatedBy,
        UpdatedAt = shipment.UpdatedAt,
        UpdatedBy = shipment.UpdatedBy,
    };
}
