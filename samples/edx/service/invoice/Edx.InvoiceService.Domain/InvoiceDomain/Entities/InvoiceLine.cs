using Edx.InvoiceService.Domain.InvoiceDomain.Consts;
using HsnSoft.Base;

namespace Edx.InvoiceService.Domain.InvoiceDomain.Entities;

/// <summary>One line of an invoice: what was sold, how many, at what price and with what VAT.</summary>
public sealed class InvoiceLine
{
    // for the database driver
    private InvoiceLine()
    {
    }

    public string Description { get; private set; } = string.Empty;

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    /// <summary>The VAT rate in percent (<c>20</c>).</summary>
    public decimal VatRate { get; private set; }

    /// <summary>Quantity times price, with the VAT, to two decimals.</summary>
    public decimal Total => Math.Round(Quantity * UnitPrice * (1 + (VatRate / 100)), 2, MidpointRounding.AwayFromZero);

    public static InvoiceLine Of(string description, decimal quantity, decimal unitPrice, decimal vatRate)
        => new()
        {
            Description = Guard.NotNullOrWhiteSpace(description, nameof(Description), InvoiceConsts.DescriptionMaxLength),
            Quantity = Guard.Range(quantity, nameof(Quantity), 0.0001m, 1_000_000_000m),
            UnitPrice = Guard.Range(unitPrice, nameof(UnitPrice), 0m, 1_000_000_000m),
            VatRate = Guard.Range(vatRate, nameof(VatRate), 0m, 100m),
        };
}
