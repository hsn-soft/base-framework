using Abc.ProductService.Domain.ProductDomain.Consts;
using HsnSoft.Base;

namespace Abc.ProductService.Domain.ProductDomain.Entities;

/// <summary>Something the Abc system sells, and how many of it there are.</summary>
public sealed class Product
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

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The user name in the <c>sub</c> claim of the token of who made the product; <see langword="null"/> when nobody signed in did.</summary>
    public string? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>The same for the last change.</summary>
    public string? UpdatedBy { get; private set; }

    public static Product Create(string name, string sku, string? description, decimal price, int stock, DateTimeOffset now, string? by)
    {
        Product product = new()
        {
            Id = Guid.CreateVersion7(),
            Sku = Normalize(Required(sku, ProductConsts.SkuMaxLength, nameof(Sku))),
            IsActive = true,
            CreatedAt = now,
            CreatedBy = Author(by),
        };
        product.Change(name, description, price, stock, now, by);
        return product;
    }

    /// <summary>A name or a sku as it is compared and searched: trimmed, in capitals.</summary>
    public static string Normalize(string text) => text.Trim().ToUpperInvariant();

    public void Update(string name, string? description, decimal price, int stock, bool isActive, DateTimeOffset now, string? by)
    {
        Change(name, description, price, stock, now, by);
        IsActive = isActive;
    }

    private void Change(string name, string? description, decimal price, int stock, DateTimeOffset now, string? by)
    {
        Name = Required(name, ProductConsts.NameMaxLength, nameof(Name));
        NormalizedName = Normalize(Name);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim().Length > ProductConsts.DescriptionMaxLength
            ? throw new DomainException($"Description is at most {ProductConsts.DescriptionMaxLength} characters.")
            : description.Trim();
        Price = price < 0 ? throw new DomainException("Price cannot be negative.") : price;
        Stock = stock < 0 ? throw new DomainException("Stock cannot be negative.") : stock;
        UpdatedAt = now;
        UpdatedBy = Author(by);
    }

    private static string? Author(string? by) => string.IsNullOrWhiteSpace(by) ? null : by.Trim()[..Math.Min(by.Trim().Length, ProductConsts.AuthorMaxLength)];

    private static string Required(string value, int maxLength, string name)
    {
        string text = value?.Trim() ?? string.Empty;
        return text.Length == 0 || text.Length > maxLength
            ? throw new DomainException($"{name} is required and at most {maxLength} characters.")
            : text;
    }
}
