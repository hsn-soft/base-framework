using Edx.IntakeService.Domain.StorageDomain.Entities;
using Edx.IntakeService.Domain.StorageDomain.Repositories;
using Edx.Shared.Contracts.Events.Intake;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.Options;

namespace Edx.IntakeService.Application.Storage;

/// <summary>
/// Makes the folders a file goes in: <c>{tenant}/{yyyy}/{MM}/{dd}/{HH}/{mm}</c>, a record for each level that is not there yet, and the name of the object in the store (the root path of the settings in front). Every folder
/// record that is written is said to the rest of the system (<see cref="StorageFolderCreatedEto"/>).
/// </summary>
public sealed class StorageLayout(IStorageFolderRepository folders, ICurrentTenant tenant, IEventBus eventBus, IOptions<StorageOptions> options)
{
    private static readonly FolderLevel[] s_timeLevels = [FolderLevel.Year, FolderLevel.Month, FolderLevel.Day, FolderLevel.Hour, FolderLevel.Minute];

    /// <summary>The folder of the tenant of the request for the time given (made, with the levels above it, when it is not there).</summary>
    public async Task<StorageFolder> EnsureFolderAsync(DateTimeOffset time, CancellationToken cancellationToken = default)
    {
        Guid tenantId = tenant.Id ?? throw new InvalidOperationException("The files are kept by tenant: the request has no tenant.");

        StorageFolder folder = await EnsureLevelAsync(null, tenantId.ToString("N"), tenantId.ToString("N"), FolderLevel.Tenant, cancellationToken).ConfigureAwait(false);
        string[] segments = FileNaming.TimeSegments(time);
        for (int index = 0; index < segments.Length; index++)
        {
            folder = await EnsureLevelAsync(folder.Id, segments[index], $"{folder.Path}/{segments[index]}", s_timeLevels[index], cancellationToken).ConfigureAwait(false);
        }

        return folder;
    }

    /// <summary>The name of an object in the container: the root path of the settings, the folder path, the file name.</summary>
    public string ObjectName(string folderPath, string fileName)
    {
        string root = options.Value.RootPath.Trim('/');
        return root.Length == 0 ? $"{folderPath}/{fileName}" : $"{root}/{folderPath}/{fileName}";
    }

    private async Task<StorageFolder> EnsureLevelAsync(Guid? parentId, string name, string path, FolderLevel level, CancellationToken cancellationToken)
    {
        (StorageFolder folder, bool created) = await folders.GetOrAddAsync(parentId, name, path, level, cancellationToken).ConfigureAwait(false);
        if (created)
        {
            await eventBus.PublishAsync(new StorageFolderCreatedEto(folder.Id, folder.ParentId, folder.Name, folder.Path, folder.Level.ToString()), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return folder;
    }
}
