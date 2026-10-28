using Edx.Shared.Contracts.Invoicing;
using HsnSoft.Base.Domain.Entities.Events;

namespace Edx.Shared.Contracts.Events.Invoicing;

/// <summary>
/// An invoice (a zip with a file, or a model) was checked and queued by the firm invoice integration, and its bytes are in its blob store. Published by the firm invoice integration; the invoice service takes it from there (copies it, unpacks it, checks it against its schema, makes the
/// draft). Who sent it (the user, the tenant) is in the envelope, and the invoice service works as that user.
/// </summary>
/// <param name="QueueId">The id of the queue item: what the sender asks the firm invoice integration about, and what the answers of the invoice service name.</param>
/// <param name="InvoiceId">The uuid of the invoice, as the sender gave it.</param>
/// <param name="InvoiceNumber">The number of the invoice, as the sender gave it.</param>
/// <param name="ReceivedBy">The host of the firm invoice integration that received it (REST, gRPC or WCF).</param>
/// <param name="SourceType">The form the invoice has: the type of the file in the zip, or <see cref="InvoiceSourceType.Model"/>.</param>
/// <param name="ChannelType">How it reached the system.</param>
/// <param name="FileHash">The SHA-256 (hex) of the stored bytes: of the zip, or of the model file.</param>
/// <param name="FileName">The name the sender gave the file (the zip's, or the model's).</param>
/// <param name="ContentType">The content type of the stored bytes.</param>
/// <param name="Container">The container of the firm invoice integration's blob store.</param>
/// <param name="ObjectName">The name of the object in the container.</param>
/// <param name="Zipped">Whether the bytes are a zip with one file (always: a model is kept as a zip too).</param>
/// <param name="ReceivedAt">When the firm invoice integration queued it.</param>
public sealed record InvoiceQueuedEto(
    Guid QueueId,
    Guid InvoiceId,
    string InvoiceNumber,
    string ReceivedBy,
    InvoiceSourceType SourceType,
    InvoiceChannelType ChannelType,
    string FileHash,
    string FileName,
    string ContentType,
    string Container,
    string ObjectName,
    bool Zipped,
    DateTimeOffset ReceivedAt) : IIntegrationEventMessage;
