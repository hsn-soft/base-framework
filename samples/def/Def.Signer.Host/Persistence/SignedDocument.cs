namespace Def.Signer.Host.Persistence;

/// <summary>What the database knows about a document; the document itself lives in blob storage.</summary>
public sealed class SignedDocument
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The id of the request that produced it; unique, so a re-delivered message cannot create a second document.</summary>
    public Guid RequestId { get; set; }

    public string Owner { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Container { get; set; } = string.Empty;

    public string BlobName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
