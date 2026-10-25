using HsnSoft.Base.Application.Dtos;

namespace Abc.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos;

public sealed class ProductStockHistoryDto : AuditedEntityDto<Guid>
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public int OldStock { get; init; }

    public int NewStock { get; init; }

    public int Change { get; init; }

    public string Reason { get; init; } = string.Empty;

    public string? Note { get; init; }

    public string? Reference { get; init; }

    public Guid TenantId { get; init; }
}
