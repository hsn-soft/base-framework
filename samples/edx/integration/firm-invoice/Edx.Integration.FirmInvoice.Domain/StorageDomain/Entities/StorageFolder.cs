using Edx.Integration.FirmInvoice.Domain.StorageDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Edx.Integration.FirmInvoice.Domain.StorageDomain.Entities;

/// <summary>
/// A folder of the storage hierarchy: <c>{tenant}/{yyyy}/{MM}/{dd}/{HH}/{mm}</c>, one record for each level. The hierarchy is kept here, in the database, and not read from the blob store: where the bytes are (the container, the
/// root path) is a setting and can change with the store, while this says how what was received is organised. The tenant is kept here and given by the persistence.
/// </summary>
public sealed class StorageFolder : CreationAuditedEntity<Guid>, IMultiTenant
{
    // for the database driver
    private StorageFolder()
    {
    }

    /// <summary>The folder above this one; none for the folder of a tenant.</summary>
    public Guid? ParentId { get; private set; }

    /// <summary>The name of this folder alone (the tenant id, <c>2026</c>, <c>10</c>, ...).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>The names from the tenant folder down to this one, joined with <c>/</c>: unique in a tenant.</summary>
    public string Path { get; private set; } = string.Empty;

    public FolderLevel Level { get; private set; }

    public Guid TenantId { get; private set; }

    public static StorageFolder Create(Guid? parentId, string name, string path, FolderLevel level)
        => new()
        {
            Id = Guid.CreateVersion7(),
            ParentId = parentId,
            Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), StorageFolderConsts.NameMaxLength),
            Path = Guard.NotNullOrWhiteSpace(path, nameof(Path), StorageFolderConsts.PathMaxLength),
            Level = level,
        };
}
