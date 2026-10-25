using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using Xyz.ProductService.Domain.ProductDomain.Consts;

namespace Xyz.ProductService.Domain.ProductDomain.Entities;

/// <summary>
/// Something the Xyz system sells, and how many of it there are.
/// The audit fields and the soft delete come from <see cref="FullAuditedEntity{TKey}"/>; the tenant is kept here and given by the persistence.
/// </summary>
public sealed class Product : FullAuditedEntity<Guid>, IMultiTenant
{
    // for the database driver
    private Product()
    {
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>The name in capitals, so the lists can look for part of it and sort without regard to case.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>The stock keeping unit, in capitals: what is unique, and what never changes.</summary>
    public string Sku { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    /// <summary>How many there are in stock; 0 is out of stock.</summary>
    public int Stock { get; private set; }

    public bool IsActive { get; private set; }

    public Guid TenantId { get; private set; }

    public static Product Create(string name, string sku, string? description, decimal price, int stock)
    {
        Product product = new()
        {
            Id = Guid.CreateVersion7(),
            Sku = StringHelper.Normalize(Guard.NotNullOrWhiteSpace(sku, nameof(Sku), ProductConsts.SkuMaxLength)),
            IsActive = true,
        };
        product.Change(name, description, price, stock);
        return product;
    }

    public void Update(string name, string? description, decimal price, int stock, bool isActive)
    {
        Change(name, description, price, stock);
        IsActive = isActive;
    }

    /// <summary>Takes <paramref name="quantity"/> out of the stock for an order: only an active product with enough in stock gives it.</summary>
    public void Reserve(int quantity)
    {
        Guard.Range(quantity, nameof(quantity), 1, int.MaxValue);
        if (!IsActive)
        {
            throw new DomainException($"The product {Name} is not for sale.").WithKey("product_not_active", Name);
        }

        if (Stock < quantity)
        {
            throw new DomainException($"There are {Stock} of {Name} in stock, {quantity} were asked for.").WithKey("product_stock_insufficient", Name, Stock, quantity);
        }

        Stock -= quantity;
    }

    private void Change(string name, string? description, decimal price, int stock)
    {
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), ProductConsts.NameMaxLength);
        NormalizedName = StringHelper.Normalize(Name);
        Description = string.IsNullOrWhiteSpace(description) ? null : Guard.Length(description.Trim(), nameof(Description), ProductConsts.DescriptionMaxLength);
        Price = Guard.NotNegative(price, nameof(Price));
        Stock = Guard.NotNegative(stock, nameof(Stock));
    }
}
