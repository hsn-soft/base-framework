namespace Edx.InvoiceService.Application.Importing;

/// <summary>Where an import went wrong: what <c>InvoiceImportFailedEto</c> and a failed invoice say.</summary>
public static class ImportStage
{
    /// <summary>The invoice could not be taken: another record of it is live, or its number is another invoice's.</summary>
    public const string Claim = "Claim";

    /// <summary>The file could not be read from the store of the firm invoice integration, or is not what the firm invoice integration said.</summary>
    public const string Fetch = "Fetch";

    /// <summary>The zip could not be opened, or does not hold the one file.</summary>
    public const string Extract = "Extract";

    /// <summary>The file does not follow its schema, or says another invoice than the sender did.</summary>
    public const string Validate = "Validate";

    /// <summary>The invoice amounts to more than the sender may make.</summary>
    public const string Limit = "Limit";

    /// <summary>The draft could not be made.</summary>
    public const string Convert = "Convert";

    /// <summary>The signed invoice did not reach its receiver (see <c>InvoiceDeliveryFailedEto</c> for where).</summary>
    public const string Delivery = "Delivery";
}
