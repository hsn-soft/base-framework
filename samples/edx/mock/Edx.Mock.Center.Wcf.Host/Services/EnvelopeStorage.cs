using Edx.Shared.Efatura.Envelope;
using HsnSoft.Base.BlobStoring;
using Microsoft.Extensions.Options;

namespace Edx.Mock.Center.Wcf.Host.Services;

/// <summary>Where the envelopes are kept: one container of the blob store, <c>received/</c> for what was handed to the center and <c>center/</c> for what it made, by month and day.</summary>
public sealed class EnvelopeStorage(IBlobStore blobs, IOptions<CenterOptions> options)
{
    private const string ZipContentType = "application/zip";

    public string ReceivedName(Guid id, string envelopeId, DateTimeOffset at) => Name("received", id, envelopeId, at);

    public string CenterName(Guid id, string envelopeId, DateTimeOffset at) => Name("center", id, envelopeId, at);

    public async Task SaveAsync(string objectName, byte[] zip, CancellationToken cancellationToken)
    {
        using MemoryStream stream = new(zip, writable: false);
        await blobs.SaveAsync(options.Value.Container, objectName, stream, ZipContentType, cancellationToken).ConfigureAwait(false);
    }

    public async Task<byte[]?> ReadAsync(string objectName, CancellationToken cancellationToken)
    {
        Stream? stream = await blobs.OpenReadAsync(options.Value.Container, objectName, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        await using (stream.ConfigureAwait(false))
        {
            using MemoryStream copy = new();
            await stream.CopyToAsync(copy, cancellationToken).ConfigureAwait(false);
            return copy.ToArray();
        }
    }

    public Task DeleteAsync(string objectName, CancellationToken cancellationToken) => blobs.DeleteAsync(options.Value.Container, objectName, cancellationToken);

    /// <summary>The party the center is in an envelope it makes.</summary>
    public EnvelopeParty CenterParty => new(options.Value.Alias, options.Value.RegisterNumber, options.Value.Title);

    private static string Name(string folder, Guid id, string envelopeId, DateTimeOffset at)
        => $"{folder}/{at.UtcDateTime:yyyyMM}/{at.UtcDateTime:dd}/{id:N}_{(Guid.TryParse(envelopeId, out Guid guid) ? guid.ToString("N") : envelopeId)}.zip";
}
