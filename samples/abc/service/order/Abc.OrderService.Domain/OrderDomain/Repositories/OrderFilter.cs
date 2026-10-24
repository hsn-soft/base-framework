using Abc.OrderService.Domain.OrderDomain.Entities;

namespace Abc.OrderService.Domain.OrderDomain.Repositories;

/// <summary>What a list of orders is narrowed by: every given value must match (the customer by a part of the name, without regard to case).</summary>
public sealed record OrderFilter(string? CustomerUserName = null, OrderStatus? Status = null);
