using Abc.ProductService.Domain.ProductDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.MultiTenancy;

namespace Abc.ProductService.Domain.ProductDomain.Entities;

/// <summary>
/// Something the Abc system sells, and how many of it there are.
/// The audit fields, the soft delete and the tenant are done by the persistence (<see cref="ICreationAudited"/>, <see cref="IModificationAudited"/>, <see cref="ISoftDelete"/>,
/// <see cref="IMultiTenant"/>): the entity only keeps them.
/// </summary>
public sealed class Product : ICreationAudited, IModificationAudited, ISoftDelete, IMultiTenant
{
    // for the database driver
    private Product()
    {
    }

    public Guid Id { get; private set; }

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

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    public void MarkCreated(DateTimeOffset at, Guid? by) => (CreatedAt, CreatedBy) = (at, by);

    public void MarkModified(DateTimeOffset at, Guid? by) => (UpdatedAt, UpdatedBy) = (at, by);

    public void MarkDeleted(DateTimeOffset at, Guid? by) => (IsDeleted, DeletedAt, DeletedBy) = (true, at, by);

    public static Product Create(string name, string sku, string? description, decimal price, int stock)
    {
        Product product = new()
        {
            Id = Guid.CreateVersion7(),
            Sku = Normalize(Required(sku, ProductConsts.SkuMaxLength, nameof(Sku))),
            IsActive = true,
        };
        product.Change(name, description, price, stock);
        return product;
    }

    /// <summary>A name or a sku as it is compared and searched: trimmed, in capitals.</summary>
    public static string Normalize(string text) => text.Trim().ToUpperInvariant();

    public void Update(string name, string? description, decimal price, int stock, bool isActive)
    {
        Change(name, description, price, stock);
        IsActive = isActive;
    }

    private void Change(string name, string? description, decimal price, int stock)
    {
        Name = Required(name, ProductConsts.NameMaxLength, nameof(Name));
        NormalizedName = Normalize(Name);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim().Length > ProductConsts.DescriptionMaxLength
            ? throw new DomainException($"Description is at most {ProductConsts.DescriptionMaxLength} characters.").WithKey("field_too_long", "Description", ProductConsts.DescriptionMaxLength)
            : description.Trim();
        Price = price < 0 ? throw new DomainException("Price cannot be negative.").WithKey("field_negative", "Price") : price;
        Stock = stock < 0 ? throw new DomainException("Stock cannot be negative.").WithKey("field_negative", "Stock") : stock;
    }

    private static string Required(string value, int maxLength, string name)
    {
        string text = value?.Trim() ?? string.Empty;
        return text.Length == 0 || text.Length > maxLength
            ? throw new DomainException($"{name} is required and at most {maxLength} characters.").WithKey("field_required", name, maxLength)
            : text;
    }
}
