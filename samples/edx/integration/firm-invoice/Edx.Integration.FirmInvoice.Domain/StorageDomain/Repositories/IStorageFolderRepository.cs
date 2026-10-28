using Edx.Integration.FirmInvoice.Domain.StorageDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Edx.Integration.FirmInvoice.Domain.StorageDomain.Repositories;

/// <summary>Where the folders are kept: what every repository does, and the one thing that must be a single step of the store: a folder that two uploads of the same minute both need is made once.</summary>
public interface IStorageFolderRepository : IRepository<StorageFolder, Guid>
{
    /// <summary>
    /// The folder of the tenant that has this path, made when there is none. Two callers that need the same new folder at once get the same one (the path is unique in a tenant, so one insert wins and the other reads it), and only the
    /// one that made it is told so (<c>Created</c>), which is what lets it say so to the rest of the system.
    /// </summary>
    Task<(StorageFolder Folder, bool Created)> GetOrAddAsync(Guid? parentId, string name, string path, FolderLevel level, CancellationToken cancellationToken = default);
}
