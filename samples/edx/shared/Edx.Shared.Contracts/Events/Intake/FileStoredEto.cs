using HsnSoft.Base.Domain.Entities.Events;

namespace Edx.Shared.Contracts.Events.Intake;

/// <summary>
/// A file was received and its record was written. Published by the intake service for every record it writes; the invoice service keeps a few facts about it. The file itself is in the blob store at
/// <see cref="Container"/> / <see cref="ObjectName"/>. Who sent it (the user, the tenant) is in the envelope.
/// </summary>
/// <param name="FileId">The id of the record.</param>
/// <param name="Kind">What the file is: <c>XmlArchive</c>, <c>TabularArchive</c>, <c>ModelJson</c> or <c>ModelXml</c>.</param>
/// <param name="Channel">How the customer sent it: <c>Rest</c>, <c>Grpc</c> or <c>Wcf</c>.</param>
/// <param name="FileName">The name the customer gave, or the name the service gave a model.</param>
/// <param name="SizeBytes">The size of the stored file.</param>
/// <param name="ContentType">The content type of the stored file.</param>
/// <param name="Container">The container (bucket) of the blob store.</param>
/// <param name="ObjectName">The name of the object in the container: the folder path and the file name.</param>
/// <param name="FolderPath">The folder of the file (tenant, year, month, day, hour, minute).</param>
/// <param name="EntryCount">How many files an archive holds; 1 for a model.</param>
/// <param name="ReceivedAt">When the file was received.</param>
public sealed record FileStoredEto(
    Guid FileId,
    string Kind,
    string Channel,
    string FileName,
    long SizeBytes,
    string ContentType,
    string Container,
    string ObjectName,
    string FolderPath,
    int EntryCount,
    DateTimeOffset ReceivedAt) : IIntegrationEventMessage;
