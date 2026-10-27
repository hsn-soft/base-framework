using Edx.InvoiceService.Application.Contracts.ReceivedFileDomain.Services;
using Edx.Shared.Contracts.Events.Intake;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Edx.InvoiceService.Http.Host.EventHandlers.Intake;

/// <summary>
/// A file was received by the intake service: keeps a few facts about it. It runs as the user who sent the file, in the tenant of the file (the envelope says who), so the record is the tenant's and its audit fields have the sender.
/// The events of the folders of the hierarchy (<see cref="StorageFolderCreatedEto"/>) are published too, and this service does not subscribe to them: it has no use for them.
/// </summary>
public sealed class FileStoredHandler(IReceivedFileAppService receivedFiles) : IIntegrationEventHandler<FileStoredEto>
{
    public Task HandleAsync(MessageEnvelope<FileStoredEto> envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return receivedFiles.RecordAsync(envelope.Message, cancellationToken);
    }
}
