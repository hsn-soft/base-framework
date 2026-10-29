namespace Edx.InvoiceService.Domain.InvoiceDomain.Consts;

public static class InvoiceConsts
{
    public const string CollectionName = "SalesInvoice";

    public const int NumberMaxLength = 64;
    public const int NameMaxLength = 200;
    public const int TaxIdMaxLength = 32;
    public const int DescriptionMaxLength = 300;
    public const int ReasonMaxLength = 2000;
    public const int PathMaxLength = 512;
    public const int MaxLines = 500;
}
