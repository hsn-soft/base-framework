using Abc.ShipmentService.Application.Contracts.PaymentDomain.Dtos;
using Abc.ShipmentService.Application.Contracts.PaymentDomain.Dtos.Filters;
using Abc.ShipmentService.Application.Contracts.PaymentDomain.Dtos.Submits;
using HsnSoft.Base.Application.Dtos;

namespace Abc.ShipmentService.Application.Contracts.PaymentDomain.Services;

/// <summary>What can be done with the payments. The controller only calls these; the rules and the data access are behind them.</summary>
public interface IPaymentAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such payment.</exception>
    Task<PaymentDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<PaymentDto>> GetPagedListAsync(GetPaymentsPaged pagedInput, CancellationToken cancellationToken = default);

    Task<List<PaymentDto>> GetFilterListAsync(GetPaymentsFilter filterInput, CancellationToken cancellationToken = default);

    /// <summary>The payments of the orders (the order and the amount), shown briefly; the text is an order id.</summary>
    Task<List<PaymentSearchDto>> GetSearchListAsync(GetPaymentsSearch searchInput, CancellationToken cancellationToken = default);

    Task<PaymentDto> CreateAsync(PaymentCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such payment.</exception>
    Task UpdateAsync(PaymentUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such payment.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
