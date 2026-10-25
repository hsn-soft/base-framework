using HsnSoft.Base.Application.Dtos;

namespace Abc.OrderService.Application.Contracts.OrderDomain.Dtos;

public sealed class OrderSearchDto : EntityDto<Guid>
{
    public string CustomerUserName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public decimal TotalAmount { get; init; }
}
