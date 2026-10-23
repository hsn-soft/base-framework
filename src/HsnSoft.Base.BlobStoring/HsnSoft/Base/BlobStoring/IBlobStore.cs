using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.BlobStoring;

/// <summary>
/// Stores files (blobs) in named containers (buckets). MinIO, Azure Blob Storage or any other store implements it in its own
/// project, so a service moves between them by changing one registration.
/// </summary>
public interface IBlobStore
{
    /// <summary>Writes the blob, creating the container when it does not exist and replacing a blob of the same name.</summary>
    Task SaveAsync(string container, string name, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Opens the blob for reading, or returns <see langword="null"/> when it does not exist.</summary>
    Task<Stream?> OpenReadAsync(string container, string name, CancellationToken cancellationToken = default);

    /// <summary>Deletes the blob; deleting a blob that does not exist is not an error.</summary>
    Task DeleteAsync(string container, string name, CancellationToken cancellationToken = default);
}
