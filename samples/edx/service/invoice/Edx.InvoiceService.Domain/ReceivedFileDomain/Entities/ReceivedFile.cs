using Edx.InvoiceService.Domain.ReceivedFileDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Edx.InvoiceService.Domain.ReceivedFileDomain.Entities;

/// <summary>
/// What the invoice service knows of a file the intake service received: a few facts, kept in its own database, from the event the intake service published. The file itself stays where the intake service put it (the container and
/// the object name say where); the invoice service never reads it from here. The audit fields are the sender's (the event is handled as the user who sent the file); the tenant is kept here and given by the persistence.
/// </summary>
public sealed class ReceivedFile : CreationAuditedEntity<Guid>, IMultiTenant
{
    // for the database driver
    private ReceivedFile()
    {
    }

    /// <summary>The id of the file in the intake service: unique, so an event that comes twice is one record.</summary>
    public Guid FileId { get; private set; }

    public string Kind { get; private set; } = string.Empty;

    public string Channel { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string ContentType { get; private set; } = string.Empty;

    public string Container { get; private set; } = string.Empty;

    public string ObjectName { get; private set; } = string.Empty;

    public string FolderPath { get; private set; } = string.Empty;

    /// <summary>How many files an archive holds; 1 for a model.</summary>
    public int EntryCount { get; private set; }

    /// <summary>When the intake service received the file.</summary>
    public DateTimeOffset ReceivedAt { get; private set; }

    public Guid TenantId { get; private set; }

    public static ReceivedFile Record(
        Guid fileId,
        string kind,
        string channel,
        string fileName,
        long sizeBytes,
        string contentType,
        string container,
        string objectName,
        string folderPath,
        int entryCount,
        DateTimeOffset receivedAt)
        => new()
        {
            Id = Guid.CreateVersion7(),
            FileId = Guard.NotDefault<Guid>(fileId, nameof(FileId)),
            Kind = Guard.NotNullOrWhiteSpace(kind, nameof(Kind), ReceivedFileConsts.TextMaxLength),
            Channel = Guard.NotNullOrWhiteSpace(channel, nameof(Channel), ReceivedFileConsts.TextMaxLength),
            FileName = Guard.NotNullOrWhiteSpace(fileName, nameof(FileName), ReceivedFileConsts.TextMaxLength),
            SizeBytes = Guard.NotNegative(sizeBytes, nameof(SizeBytes)),
            ContentType = Guard.NotNullOrWhiteSpace(contentType, nameof(ContentType), ReceivedFileConsts.TextMaxLength),
            Container = Guard.NotNullOrWhiteSpace(container, nameof(Container), ReceivedFileConsts.TextMaxLength),
            ObjectName = Guard.NotNullOrWhiteSpace(objectName, nameof(ObjectName), ReceivedFileConsts.TextMaxLength),
            FolderPath = Guard.NotNullOrWhiteSpace(folderPath, nameof(FolderPath), ReceivedFileConsts.TextMaxLength),
            EntryCount = Guard.Range(entryCount, nameof(EntryCount), 1, int.MaxValue),
            ReceivedAt = receivedAt,
        };
}
