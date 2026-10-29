using HsnSoft.Base;

namespace Edx.Integration.FirmInvoice.Domain.QueueDomain.Exceptions;

/// <summary>There is no such queue item (or it is another tenant's): answered as 404.</summary>
public static class QueueItemNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Queue item {id} was not found.").WithKey("queue_item_not_found", id);

    public static NotFoundException ForInvoice(Guid invoiceId) => new NotFoundException($"Nothing of the invoice {invoiceId} is in the queue.").WithKey("queue_invoice_not_found", invoiceId);
}

/// <summary>What the sender gave is not what the endpoint takes (a known error: the sender is told at once, with one of the codes below, and nothing is kept).</summary>
public sealed class InvalidUploadException : BusinessException
{
    public const string HashMismatch = "upload_hash_mismatch";
    public const string NotAZip = "upload_not_a_zip";
    public const string NotOneFile = "upload_not_one_file";
    public const string TooLarge = "upload_too_large";
    public const string TypeMismatch = "upload_type_mismatch";
    public const string EntryNameUnsafe = "upload_entry_name_unsafe";
    public const string XmlNotWellFormed = "upload_xml_not_well_formed";
    public const string FieldMissing = "upload_field_missing";

    public InvalidUploadException(string code, string message, params object[] args)
        : base(code, message) => WithKey(code, args);
}
