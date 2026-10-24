namespace Abc.OrderService.Application.Contracts.OrderDomain.Dtos;

public sealed class OrderSearchDto
{
    public Guid Id { get; init; }

    public string CustomerUserName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public decimal TotalAmount { get; init; }
}
