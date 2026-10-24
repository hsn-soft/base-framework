namespace Abc.ProductService.Domain.ProductDomain.Repositories;

/// <summary>What a list of products is narrowed by: every given value must match (names and skus by a part of them, without regard to case).</summary>
public sealed record ProductFilter(string? Name = null, string? Sku = null, bool? IsActive = null, bool? InStock = null);
