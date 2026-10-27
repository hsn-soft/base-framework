using HsnSoft.Base;

namespace Edx.IntakeService.Domain.StoredFileDomain.Exceptions;

/// <summary>What a customer sent is not what the endpoint takes (a known error: the client is told, with one of the codes below).</summary>
public sealed class InvalidArchiveException : BusinessException
{
    public const string NotAZip = "archive_not_a_zip";
    public const string Empty = "archive_empty";
    public const string TooManyEntries = "archive_too_many_entries";
    public const string TooLarge = "archive_too_large";
    public const string EntryNotAllowed = "archive_entry_not_allowed";
    public const string EntryNameUnsafe = "archive_entry_name_unsafe";
    public const string XmlNotWellFormed = "archive_xml_not_well_formed";

    public InvalidArchiveException(string code, string message, params object[] args)
        : base(code, message) => WithKey(code, args);
}
