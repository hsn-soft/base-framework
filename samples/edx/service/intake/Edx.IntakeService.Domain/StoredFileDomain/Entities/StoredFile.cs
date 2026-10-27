using Edx.IntakeService.Domain.StoredFileDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Edx.IntakeService.Domain.StoredFileDomain.Entities;

/// <summary>
/// The record of a file that was received and kept in the blob store: what it is, who sent it and how, where its bytes are (the container and the object name, both from the settings of the service when it was stored) and in which folder of
/// the hierarchy it was put. The bytes are not here. The audit fields say who sent it and when; the tenant is kept here and given by the persistence.
/// </summary>
public sealed class StoredFile : CreationAuditedEntity<Guid>, IMultiTenant
{
    // for the database driver
    private StoredFile()
    {
    }

    public FileKind Kind { get; private set; }

    public IntakeChannel Channel { get; private set; }

    /// <summary>The name the customer gave the file (cleaned of any path), or the name the service gave a model.</summary>
    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public Guid FolderId { get; private set; }

    /// <summary>The path of the folder in the hierarchy (<c>{tenant}/2026/10/27/14/35</c>), kept here too so a record can be read without its folder.</summary>
    public string FolderPath { get; private set; } = string.Empty;

    /// <summary>The container (bucket) the bytes were written to.</summary>
    public string Container { get; private set; } = string.Empty;

    /// <summary>The name of the object in the container: the root path of the settings, the folder path and the file name.</summary>
    public string ObjectName { get; private set; } = string.Empty;

    /// <summary>The files of an archive; empty for a model.</summary>
    public IReadOnlyList<ArchiveEntry> Entries { get; private set; } = [];

    public Guid TenantId { get; private set; }

    public static StoredFile Create(
        Guid id,
        FileKind kind,
        IntakeChannel channel,
        string fileName,
        string contentType,
        long sizeBytes,
        StorageFolderRef folder,
        string container,
        string objectName,
        IReadOnlyList<ArchiveEntry> entries)
        => new()
        {
            Id = id,
            Kind = kind,
            Channel = channel,
            FileName = Guard.NotNullOrWhiteSpace(fileName, nameof(FileName), StoredFileConsts.FileNameMaxLength),
            ContentType = Guard.NotNullOrWhiteSpace(contentType, nameof(ContentType)),
            SizeBytes = Guard.NotNegative(sizeBytes, nameof(SizeBytes)),
            FolderId = folder.Id,
            FolderPath = folder.Path,
            Container = Guard.NotNullOrWhiteSpace(container, nameof(Container)),
            ObjectName = Guard.NotNullOrWhiteSpace(objectName, nameof(ObjectName), StoredFileConsts.ObjectNameMaxLength),
            Entries = entries,
        };
}

/// <summary>The folder a file is put in: its id and its path.</summary>
public readonly record struct StorageFolderRef(Guid Id, string Path);
