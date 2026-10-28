using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Security.Cryptography;
using Edx.InvoiceService.Application.Importing;
using Edx.InvoiceService.Application.Signing;
using Edx.InvoiceService.Application.Storage;
using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using Edx.InvoiceService.Domain.InvoiceDomain.Exceptions;
using Edx.InvoiceService.Domain.InvoiceDomain.Repositories;
using Edx.Shared.Contracts.Events.Invoicing;
using Edx.Shared.Contracts.Invoicing;
using Edx.Shared.Helper;
using HsnSoft.Base;
using HsnSoft.Base.Authorization;
using HsnSoft.Base.BlobStoring;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Edx.InvoiceService.Application.Services;

/// <summary>
/// What the invoice service does with the events of the pipeline, in the order the invoice goes: <see cref="OnQueuedAsync"/> takes an invoice from the queue of the intake service (claims it, copies it, unpacks it, reads it by its type, checks it against its schema and against what the
/// sender may make, writes its draft XML), and <see cref="OnApprovedAsync"/> signs the invoice a person approved and keeps it. Both run as the user the event came from, in the tenant of the event, so the limits are the sender's and the approver's. What goes wrong with an invoice is
/// the invoice's failure, told to the intake service (<see cref="InvoiceImportFailedEto"/>) and kept on the record, and is not retried: the sender has to correct the file. There is no transaction over the store, the database and the broker; a record that was claimed and whose
/// work did not end (the service was stopped) stays <see cref="InvoiceStatus.Processing"/> and blocks the invoice until somebody looks at it, which the sample does not automate.
/// </summary>
public sealed class InvoiceImportService(
    IInvoiceRepository invoices,
    IBlobStore blobs,
    IEventBus eventBus,
    IPermissionChecker permissions,
    IInvoiceSigner signer,
    TimeProvider time,
    IOptions<InvoiceStorageOptions> storage,
    IOptions<InvoiceImportOptions> import,
    ILogger<InvoiceImportService> logger)
{
    private const string ErrorPrefix = "error_";
    private const string ZipContentType = "application/zip";
    private const string XmlContentType = "application/xml";

    public async Task OnQueuedAsync(InvoiceQueuedEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // the claim: an invoice is at one place at a time. What is already there decides (the store decides when two come at once).
        Invoice? live = await invoices.FindLiveAsync(message.InvoiceId, cancellationToken).ConfigureAwait(false);
        if (live is not null)
        {
            await RefuseAsync(message, live, cancellationToken).ConfigureAwait(false);
            return;
        }

        Invoice record = Invoice.Begin(message.InvoiceId, message.InvoiceNumber, message.QueueId, message.SourceType, message.ChannelType, time.GetUtcNow());
        if (!await invoices.TryAddAsync(record, cancellationToken).ConfigureAwait(false))
        {
            await RefuseAsync(message, await invoices.FindLiveAsync(message.InvoiceId, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return;
        }

        string stage = ImportStage.Fetch;
        try
        {
            stage = await ImportAsync(record, message, cancellationToken).ConfigureAwait(false);
        }
        catch (ImportFailure failure)
        {
            await FailAsync(record, message, failure.Stage, failure.Message, cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // whatever else went wrong (the store, the database) is not the sender's to correct, but a record that stays Processing would block the invoice for good
            logger.LogError(exception, "Invoice {InvoiceId} ({QueueItemId}) could not be imported at {ImportStage}", message.InvoiceId, message.QueueId, stage);
            await FailAsync(record, message, stage, "The invoice could not be processed because of an error of the service; send it again.", cancellationToken).ConfigureAwait(false);
            return;
        }

        // the draft stands: the older failed or rejected records of the invoice are replaced by it (deleted softly, and their queue items too)
        foreach (Invoice old in await invoices.FindReplaceableAsync(message.InvoiceId, record.Id, cancellationToken).ConfigureAwait(false))
        {
            await invoices.DeleteAsync(old, cancellationToken).ConfigureAwait(false);
            await eventBus.PublishAsync(new InvoiceSupersededEto(old.QueueId, old.InvoiceId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        await eventBus.PublishAsync(new InvoiceDraftCreatedEto(message.QueueId, message.InvoiceId), cancellationToken: cancellationToken).ConfigureAwait(false);
        logger.LogInformation("The draft of invoice {InvoiceId} ({InvoiceNumber}) was made: {InvoiceTotal}", record.InvoiceId, record.Number, record.Total);
    }

    public async Task OnApprovedAsync(InvoiceApprovedEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        Invoice? invoice = await invoices.FindAsync(message.RecordId, cancellationToken).ConfigureAwait(false);
        if (invoice is not { Status: InvoiceStatus.Approved, DraftPath: { } draftPath })
        {
            // signed already (the event came twice), or the invoice is not there: nothing to do
            logger.LogWarning("Invoice record {InvoiceRecordId} was said approved and is {InvoiceStatus}: nothing to sign", message.RecordId, invoice?.Status);
            return;
        }

        byte[] draft = await ReadAsync(storage.Value.Container, draftPath, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException($"The draft '{draftPath}' of invoice {invoice.InvoiceId} is not in the store.");
        string signedPath = $"signed/{invoice.TenantId:N}/signed_{invoice.InvoiceId:N}.xml";
        using (MemoryStream signed = new(signer.Sign(draft), writable: false))
        {
            await blobs.SaveAsync(storage.Value.Container, signedPath, signed, XmlContentType, cancellationToken).ConfigureAwait(false);
        }

        invoice.Sign(signedPath, time.GetUtcNow());
        await invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
        await blobs.DeleteAsync(storage.Value.Container, draftPath, cancellationToken).ConfigureAwait(false);
        await eventBus.PublishAsync(new InvoiceCompletedEto(invoice.QueueId, invoice.InvoiceId), cancellationToken: cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Invoice {InvoiceId} ({InvoiceNumber}) was signed", invoice.InvoiceId, invoice.Number);
    }

    // the whole import of a claimed invoice; returns the stage it reached (for the log of an unexpected error)
    private async Task<string> ImportAsync(Invoice record, InvoiceQueuedEto message, CancellationToken cancellationToken)
    {
        string container = storage.Value.Container;
        string tenant = record.TenantId.ToString("N");
        string name = FileName(message.FileName);

        // fetch: the bytes the intake service kept, checked against the hash it said, and copied into the container of this service
        byte[] bytes = await ReadAsync(message.Container, message.ObjectName, cancellationToken).ConfigureAwait(false) ?? throw new ImportFailure(ImportStage.Fetch, "The file of the invoice is not in the store of the intake service.");
        if (!string.Equals(Convert.ToHexStringLower(SHA256.HashData(bytes)), message.FileHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new ImportFailure(ImportStage.Fetch, "The file in the store is not the one that was received (its hash is not the one that was checked).");
        }

        string uploadPath = $"uploads/{tenant}/{message.QueueId:N}_{name}";
        await SaveAsync(container, uploadPath, bytes, message.Zipped ? ZipContentType : XmlContentType, cancellationToken).ConfigureAwait(false);
        record.SetUploadPath(uploadPath);

        // extract: the one file of the zip, unpacked in memory, kept under the queue id
        byte[] content = bytes;
        string contentName = name;
        if (message.Zipped)
        {
            (contentName, content) = Unzip(bytes);
            string extractedPath = $"extracted/{tenant}/{message.QueueId:N}_{contentName}";
            await SaveAsync(container, extractedPath, content, XmlContentType, cancellationToken).ConfigureAwait(false);
            record.SetExtractedPath(extractedPath);
        }

        // validate: the file is read by its type, says what the sender said, and follows the schema
        InvoiceSubmissionDto submission = Read(message, content, contentName);

        // limit: what the sender may make, as the sender; it is checked on the draft, which has the total (nothing is written until it passes)
        string draftPath = $"draft/{tenant}/draft_{record.InvoiceId:N}.xml";
        try
        {
            record.MarkDrafted(submission, draftPath, time.GetUtcNow());
        }
        catch (Exception exception) when (exception is DomainException or ArgumentException)
        {
            throw new ImportFailure(ImportStage.Validate, exception.Message);
        }

        decimal limit;
        try
        {
            limit = await permissions.RequireDecimalAsync(EdxConstraintPermissions.Invoice.MaxCreateAmount, cancellationToken).ConfigureAwait(false);
        }
        catch (BaseHttpException exception)
        {
            throw new ImportFailure(ImportStage.Limit, exception.Message);
        }

        if (record.Total > limit)
        {
            throw new ImportFailure(ImportStage.Limit, new InvoiceOverLimitException(record.Total.Value, limit).Message);
        }

        // convert: the draft XML, then the record is the draft
        await SaveAsync(container, draftPath, InvoiceXml.Write(submission), XmlContentType, cancellationToken).ConfigureAwait(false);
        await invoices.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
        return ImportStage.Convert;
    }

    private static InvoiceSubmissionDto Read(InvoiceQueuedEto message, byte[] content, string fileName)
    {
        InvoiceSubmissionDto submission;
        try
        {
            submission = InvoiceFileParser.Parse(message.SourceType, content, fileName, message.InvoiceId);
        }
        catch (InvoiceFileException exception)
        {
            throw new ImportFailure(ImportStage.Validate, exception.Message);
        }

        // a file that carries its own uuid and number (an XML, a model) says the same as the sender did
        if (submission.InvoiceId != message.InvoiceId)
        {
            throw new ImportFailure(ImportStage.Validate, $"The invoice id in the file is {submission.InvoiceId}, and the one given with it is {message.InvoiceId}.");
        }

        if (!string.Equals(StringHelper.Normalize(submission.InvoiceNumber ?? string.Empty), StringHelper.Normalize(message.InvoiceNumber), StringComparison.Ordinal))
        {
            throw new ImportFailure(ImportStage.Validate, $"The invoice number in the file is '{submission.InvoiceNumber}', and the one given with it is '{message.InvoiceNumber}'.");
        }

        List<ValidationResult> problems = [];
        if (!Validator.TryValidateObject(submission, new ValidationContext(submission), problems, validateAllProperties: true))
        {
            throw new ImportFailure(ImportStage.Validate, string.Join(" ", problems.Select(problem => problem.ErrorMessage).Take(5)));
        }

        return submission;
    }

    // one file in the zip, not bigger than it may be unpacked
    private (string Name, byte[] Content) Unzip(byte[] zip)
    {
        try
        {
            using MemoryStream stream = new(zip, writable: false);
            using ZipArchive archive = new(stream, ZipArchiveMode.Read);
            ZipArchiveEntry[] files = [.. archive.Entries.Where(entry => !entry.FullName.EndsWith('/'))];
            if (files.Length != 1)
            {
                throw new ImportFailure(ImportStage.Extract, $"The zip has {files.Length} files; it must have exactly one.");
            }

            ZipArchiveEntry entry = files[0];
            long limit = import.Value.MaxUnpackedBytes;
            if (entry.Length > limit)
            {
                throw new ImportFailure(ImportStage.Extract, $"The file in the zip is {entry.Length} bytes unpacked, and {limit} is the most.");
            }

            using Stream entryStream = entry.Open();
            using MemoryStream unpacked = new();

            // the size in the header is the zip's word; what is really read is counted
            byte[] buffer = new byte[81920];
            int read;
            while ((read = entryStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (unpacked.Length + read > limit)
                {
                    throw new ImportFailure(ImportStage.Extract, $"The file in the zip is bigger than {limit} bytes unpacked.");
                }

                unpacked.Write(buffer, 0, read);
            }

            return (FileName(entry.Name), unpacked.ToArray());
        }
        catch (InvalidDataException exception)
        {
            throw new ImportFailure(ImportStage.Extract, $"The zip cannot be opened: {exception.Message}");
        }
    }

    // an invoice that is there already: the new one is refused, and told why (nothing was changed)
    private Task RefuseAsync(InvoiceQueuedEto message, Invoice? live, CancellationToken cancellationToken)
    {
        string reason = live switch
        {
            null => $"The invoice number '{message.InvoiceNumber}' belongs to another invoice; send this one with a number of its own.",
            { Status: InvoiceStatus.Processing } => "This invoice is being processed: wait for it to finish.",
            { Status: InvoiceStatus.DraftCreated } => "This invoice has a draft that waits for approval. To send it again, reject the draft first, or use another invoice number.",
            { Status: InvoiceStatus.Approved } => "This invoice was approved and is being signed; it cannot be sent again. Use another invoice number.",
            _ => "This invoice was approved and signed; it cannot be sent again. Use another invoice number.",
        };
        logger.LogInformation("Invoice {InvoiceId} ({QueueItemId}) was refused: {Reason}", message.InvoiceId, message.QueueId, reason);
        return eventBus.PublishAsync(new InvoiceImportFailedEto(message.QueueId, message.InvoiceId, ImportStage.Claim, reason), cancellationToken: cancellationToken);
    }

    // the record failed: its objects are named with error_ in front, the reason is kept, and the intake service is told
    private async Task FailAsync(Invoice record, InvoiceQueuedEto message, string stage, string reason, CancellationToken cancellationToken)
    {
        try
        {
            record.MovedObjects(await RenameAsync(record.UploadPath, cancellationToken).ConfigureAwait(false), await RenameAsync(record.ExtractedPath, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // the names are a convenience: the failure is still told
            logger.LogWarning(exception, "The objects of failed invoice {InvoiceId} could not be renamed", message.InvoiceId);
        }

        record.Fail(stage, reason, time.GetUtcNow());
        await invoices.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
        await eventBus.PublishAsync(new InvoiceImportFailedEto(message.QueueId, message.InvoiceId, stage, reason), cancellationToken: cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Invoice {InvoiceId} ({QueueItemId}) failed at {ImportStage}: {Reason}", message.InvoiceId, message.QueueId, stage, reason);
    }

    // the object is read and written under its new name and the old one is taken away (the store has no move)
    private async Task<string?> RenameAsync(string? path, CancellationToken cancellationToken)
    {
        if (path is null)
        {
            return null;
        }

        int slash = path.LastIndexOf('/');
        string renamed = $"{path[..(slash + 1)]}{ErrorPrefix}{path[(slash + 1)..]}";
        byte[]? bytes = await ReadAsync(storage.Value.Container, path, cancellationToken).ConfigureAwait(false);
        if (bytes is null)
        {
            return path;
        }

        await SaveAsync(storage.Value.Container, renamed, bytes, XmlContentType, cancellationToken).ConfigureAwait(false);
        await blobs.DeleteAsync(storage.Value.Container, path, cancellationToken).ConfigureAwait(false);
        return renamed;
    }

    private async Task<byte[]?> ReadAsync(string container, string path, CancellationToken cancellationToken)
    {
        Stream? stream = await blobs.OpenReadAsync(container, path, cancellationToken).ConfigureAwait(false);
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

    private async Task SaveAsync(string container, string path, byte[] bytes, string contentType, CancellationToken cancellationToken)
    {
        using MemoryStream stream = new(bytes, writable: false);
        await blobs.SaveAsync(container, path, stream, contentType, cancellationToken).ConfigureAwait(false);
    }

    // a name without a path in it and without what a store may not take
    private static string FileName(string? name)
    {
        string file = Path.GetFileName((name ?? string.Empty).Replace('\\', '/'));
        char[] invalid = [.. Path.GetInvalidFileNameChars(), ' '];
        string cleaned = string.Concat(file.Select(character => invalid.Contains(character) || char.IsControl(character) ? '_' : character)).Trim('.', '_');
        return cleaned.Length == 0 ? "file" : cleaned.Length > 120 ? cleaned[^120..] : cleaned;
    }

    /// <summary>The invoice cannot be taken in, at this stage, for this reason (the reason is for the sender).</summary>
    private sealed class ImportFailure(string stage, string reason) : Exception(reason)
    {
        public string Stage { get; } = stage;
    }
}
