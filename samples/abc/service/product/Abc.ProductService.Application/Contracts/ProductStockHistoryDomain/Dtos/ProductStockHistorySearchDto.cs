using HsnSoft.Base.Application.Dtos;

namespace Abc.ProductService.Application.Contracts.ProductStockHistoryDomain.Dtos;

/// <summary>What a search shows of a stock history record: enough to recognize and pick one.</summary>
public sealed class ProductStockHistorySearchDto : EntityDto<Guid>
{
    public string ProductName { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;
}
