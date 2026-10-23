using System.Security.Cryptography;
using System.Text.Json;
using Def.Signer.Host.Events;
using Def.Signer.Host.Persistence;
using HsnSoft.Base.BlobStoring;
using HsnSoft.Base.EventBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Def.Signer.Host.Handlers;

public sealed class SignerOptions
{
    public string Bucket { get; set; } = "signer-documents";

    /// <summary>Stands in for the real, slow work of producing a document.</summary>
    public TimeSpan ProcessingDelay { get; set; } = TimeSpan.FromSeconds(2);
}

/// <summary>Runs on the Kafka consumer: produces the document, stores it in blob storage and records it in the database.</summary>
public sealed partial class SignRequestedHandler(
    SignerDbContext db,
    IBlobStore blobs,
    IOptions<SignerOptions> options,
    ILogger<SignRequestedHandler> logger) : IIntegrationEventHandler<SignRequested>
{
    private const string ContentType = "application/json";
    private static readonly JsonSerializerOptions s_indented = new(JsonSerializerOptions.Web) { WriteIndented = true };

    public async Task HandleAsync(SignRequested integrationEvent, CancellationToken cancellationToken)
    {
        // idempotent: a re-delivered message finds its document and stops
        if (await db.Documents.AnyAsync(x => x.RequestId == integrationEvent.EventId, cancellationToken))
        {
            return;
        }

        await Task.Delay(options.Value.ProcessingDelay, cancellationToken);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(
            new
            {
                requestId = integrationEvent.EventId,
                owner = integrationEvent.Owner,
                title = integrationEvent.Title,
                content = integrationEvent.Content,
                requestedAt = integrationEvent.OccurredAt,
                signedAt = DateTimeOffset.UtcNow,
                contentDigest = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(integrationEvent.Content))),
            },
            s_indented);

        string container = options.Value.Bucket;
        string blobName = $"{integrationEvent.Owner}/{integrationEvent.EventId}.json";

        // same name on a retry, so a failed database write below cannot leave a second file behind
        await blobs.SaveAsync(container, blobName, new MemoryStream(bytes), ContentType, cancellationToken);

        db.Documents.Add(new SignedDocument
        {
            RequestId = integrationEvent.EventId,
            Owner = integrationEvent.Owner,
            Title = integrationEvent.Title,
            Container = container,
            BlobName = blobName,
            ContentType = ContentType,
            SizeBytes = bytes.Length,
            Sha256 = Convert.ToHexString(SHA256.HashData(bytes)),
        });
        await db.SaveChangesAsync(cancellationToken);

        LogStored(logger, integrationEvent.EventId, container, blobName);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Request {RequestId} stored as {Container}/{BlobName}")]
    private static partial void LogStored(ILogger logger, Guid requestId, string container, string blobName);
}
