using Abc.ProductService.Domain.ProductStockHistoryDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;

namespace Abc.ProductService.Domain.ProductStockHistoryDomain.Entities;

/// <summary>
/// One change of the stock of a product, kept in the stock database (its own MongoDB): how many there were, how many there are, and why. Who did it and when come from <see cref="FullAuditedEntity{TKey}"/>
/// (the soft delete too). The product is only referenced by its id: the stock history is in another database than the products.
/// </summary>
public sealed class ProductStockHistory : FullAuditedEntity<Guid>, IMultiTenant
{
    // for the database driver
    private ProductStockHistory()
    {
    }

    public Guid ProductId { get; private set; }

    /// <summary>The name the product had then (it may change later).</summary>
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>The name in capitals, so the lists can look for part of it without regard to case.</summary>
    public string NormalizedProductName { get; private set; } = string.Empty;

    public int OldStock { get; private set; }

    public int NewStock { get; private set; }

    /// <summary><see cref="NewStock"/> minus <see cref="OldStock"/>: negative when the stock went down.</summary>
    public int Change { get; private set; }

    /// <summary><see cref="ProductStockHistoryConsts.Created"/>, <see cref="ProductStockHistoryConsts.Updated"/> or any other word of at most <see cref="ProductStockHistoryConsts.ReasonMaxLength"/> characters, in small letters.</summary>
    public string Reason { get; private set; } = string.Empty;

    public string? Note { get; private set; }

    public Guid TenantId { get; private set; }

    public static ProductStockHistory Record(Guid productId, string productName, int oldStock, int newStock, string reason, string? note = null)
    {
        ProductStockHistory history = new()
        {
            Id = Guid.CreateVersion7(),
            ProductId = Guard.NotDefault<Guid>(productId, nameof(ProductId)),
            ProductName = Guard.NotNullOrWhiteSpace(productName, nameof(ProductName), ProductStockHistoryConsts.ProductNameMaxLength),
            OldStock = Guard.NotNegative(oldStock, nameof(OldStock)),
            NewStock = Guard.NotNegative(newStock, nameof(NewStock)),
        };
        history.Change = history.NewStock - history.OldStock;
        history.NormalizedProductName = StringHelper.Normalize(history.ProductName);
        history.Describe(reason, note);
        return history;
    }

    /// <summary>Changes the reason and the note; what the stock was and became is a fact and stays.</summary>
    public void Update(string reason, string? note) => Describe(reason, note);

    private void Describe(string reason, string? note)
    {
        Reason = Guard.NotNullOrWhiteSpace(reason, nameof(Reason), ProductStockHistoryConsts.ReasonMaxLength).Trim().ToLowerInvariant();
        Note = string.IsNullOrWhiteSpace(note) ? null : Guard.Length(note.Trim(), nameof(Note), ProductStockHistoryConsts.NoteMaxLength);
    }
}
