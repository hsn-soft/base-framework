using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using Edx.InvoiceService.Application.Importing;
using Edx.InvoiceService.Application.Storage;
using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using Edx.InvoiceService.Domain.InvoiceDomain.Exceptions;
using Edx.InvoiceService.Domain.InvoiceDomain.Repositories;
using Edx.Shared.Contracts.Events.Integration;
using Edx.Shared.Contracts.Events.Invoicing;
using Edx.Shared.Contracts.Invoicing;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Envelope;
using Edx.Shared.Efatura.Ubl;
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
/// What the invoice service does with the events of the pipeline, in the order the invoice goes: <see cref="OnQueuedAsync"/> takes an invoice from the queue of the firm invoice integration (claims it, copies it, unpacks it, reads it by its type, checks it against its schema and against what the
/// sender may make, writes its draft XML), and <see cref="OnSignedAsync"/> keeps the invoice the signer service signed (the approval itself is in <c>InvoiceAppService</c>, the signing in the signer service). Both run as the user the event came from, in the tenant of the event, so the limits are the sender's. What goes wrong with an invoice is
/// the invoice's failure, told to the firm invoice integration (<see cref="InvoiceImportFailedEto"/>) and kept on the record, and is not retried: the sender has to correct the file. There is no transaction over the store, the database and the broker; a record that was claimed and whose
/// work did not end (the service was stopped) stays <see cref="SalesInvoiceStatus.Processing"/> and blocks the invoice until somebody looks at it, which the sample does not automate.
/// </summary>
public sealed class InvoiceImportService(
    ISalesInvoiceRepository invoices,
    IBlobStore blobs,
    IEventBus eventBus,
    IPermissionChecker permissions,
    TimeProvider time,
    IOptions<InvoiceStorageOptions> storage,
    IOptions<InvoiceImportOptions> import,
    ILogger<InvoiceImportService> logger)
{
    private const string ErrorPrefix = "error_";
    private const string TemporaryFolder = "firm-invoice-tmp";
    private const string ZipContentType = "application/zip";
    private const string XmlContentType = "application/xml";
    private const int MaxProblemsInReason = 3;

    public async Task OnQueuedAsync(InvoiceQueuedEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // the claim: an invoice is at one place at a time. What is already there decides (the store decides when two come at once).
        SalesInvoice? live = await invoices.FindLiveAsync(message.InvoiceId, cancellationToken).ConfigureAwait(false);
        if (live is not null)
        {
            await RefuseAsync(message, live, cancellationToken).ConfigureAwait(false);
            return;
        }

        SalesInvoice record = SalesInvoice.Begin(message.InvoiceId, message.InvoiceNumber, message.QueueId, message.SourceType, message.ChannelType, time.GetUtcNow());
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
        foreach (SalesInvoice old in await invoices.FindReplaceableAsync(message.InvoiceId, record.Id, cancellationToken).ConfigureAwait(false))
        {
            await invoices.DeleteAsync(old, cancellationToken).ConfigureAwait(false);
            await eventBus.PublishAsync(new InvoiceSupersededEto(old.QueueId, old.InvoiceId), cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        await eventBus.PublishAsync(new InvoiceDraftCreatedEto(message.QueueId, message.InvoiceId), cancellationToken: cancellationToken).ConfigureAwait(false);
        logger.LogInformation("The draft of invoice {InvoiceId} ({InvoiceNumber}) was made: {InvoiceTotal}", record.InvoiceId, record.Number, record.Total);
    }

    /// <summary>The signer service signed the invoice of an approved draft and wrote it in the signed place: the record moves to signed and the draft is deleted. (The GİB invoice integration takes the same event and sends the invoice.)</summary>
    public async Task OnSignedAsync(InvoiceSignedEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        SalesInvoice? invoice = await invoices.FindAsync(message.RecordId, cancellationToken).ConfigureAwait(false);
        if (invoice is not { Status: SalesInvoiceStatus.Approved, DraftPath: { } draftPath })
        {
            // the event came twice, or the invoice is not there: nothing to do
            logger.LogWarning("Invoice record {InvoiceRecordId} was said signed and is {InvoiceStatus}: nothing to do", message.RecordId, invoice?.Status);
            return;
        }

        invoice.Sign(message.ObjectName, time.GetUtcNow());
        await invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
        await blobs.DeleteAsync(storage.Value.Container, draftPath, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Invoice {InvoiceId} ({InvoiceNumber}) was signed", invoice.InvoiceId, invoice.Number);
    }

    /// <summary>The GİB invoice integration made the envelope of the invoice and the center took it. Only the envelope side of the invoice changes: its own status ended when it was signed.</summary>
    public async Task OnSentAsync(InvoiceSentEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        SalesInvoice? invoice = await invoices.FindAsync(message.RecordId, cancellationToken).ConfigureAwait(false);
        if (invoice is null || !invoice.ApplyEnvelope(message.EnvelopeId, InvoiceEnvelopeState.Sent, EfaturaStatus.Queued, EfaturaStatus.NameOf(EfaturaStatus.Queued), null, time.GetUtcNow()))
        {
            logger.LogInformation("Invoice record {InvoiceRecordId} was said sent and its envelope is {EnvelopeState}: the news is left", message.RecordId, invoice?.EnvelopeState);
            return;
        }

        await invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("The envelope of invoice {InvoiceId} ({InvoiceNumber}) was taken by the center", invoice.InvoiceId, invoice.Number);
    }

    /// <summary>The envelope was placed at the receiver's system: the queue item of the invoice is completed.</summary>
    public async Task OnDeliveredAsync(InvoiceDeliveredEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        SalesInvoice? invoice = await invoices.FindAsync(message.RecordId, cancellationToken).ConfigureAwait(false);
        if (invoice is null || !invoice.ApplyEnvelope(message.EnvelopeId, InvoiceEnvelopeState.Delivered, message.StatusCode, EfaturaStatus.NameOf(message.StatusCode), null, time.GetUtcNow()))
        {
            logger.LogInformation("Invoice record {InvoiceRecordId} was said delivered and its envelope is {EnvelopeState}: the news is left", message.RecordId, invoice?.EnvelopeState);
            return;
        }

        await invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
        await eventBus.PublishAsync(new InvoiceCompletedEto(invoice.QueueId, invoice.InvoiceId), cancellationToken: cancellationToken).ConfigureAwait(false);
        logger.LogInformation("The envelope of invoice {InvoiceId} ({InvoiceNumber}) was delivered to its receiver", invoice.InvoiceId, invoice.Number);
    }

    /// <summary>The envelope did not reach the receiver (the center refused it, or could not deliver it): the reason stays on the invoice, and the queue item of the invoice is told.</summary>
    public async Task OnDeliveryFailedAsync(InvoiceDeliveryFailedEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        SalesInvoice? invoice = await invoices.FindAsync(message.RecordId, cancellationToken).ConfigureAwait(false);
        InvoiceEnvelopeState state = message.StatusCode is >= EfaturaStatus.Processing and < EfaturaStatus.Processed ? InvoiceEnvelopeState.Refused : InvoiceEnvelopeState.Failed;
        string text = message.StatusCode == 0 ? string.Empty : EfaturaStatus.NameOf(message.StatusCode);
        if (invoice is null || !invoice.ApplyEnvelope(message.EnvelopeId ?? invoice.EnvelopeId ?? Guid.Empty, state, message.StatusCode, text, $"{message.Stage}: {message.Reason}", time.GetUtcNow()))
        {
            logger.LogInformation("Invoice record {InvoiceRecordId} was said undelivered and its envelope is {EnvelopeState}: the news is left", message.RecordId, invoice?.EnvelopeState);
            return;
        }

        await invoices.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
        await eventBus.PublishAsync(new InvoiceImportFailedEto(invoice.QueueId, invoice.InvoiceId, ImportStage.Delivery, message.Reason), cancellationToken: cancellationToken).ConfigureAwait(false);
        logger.LogInformation("The envelope of invoice {InvoiceId} ({InvoiceNumber}) did not reach its receiver at {DeliveryStage}: {Reason}", invoice.InvoiceId, invoice.Number, message.Stage, message.Reason);
    }

    // the whole import of a claimed invoice; returns the stage it reached (for the log of an unexpected error)
    private async Task<string> ImportAsync(SalesInvoice record, InvoiceQueuedEto message, CancellationToken cancellationToken)
    {
        string container = storage.Value.Container;
        string tenant = record.TenantId.ToString("N");
        string name = FileName(message.FileName);

        // fetch: the bytes the firm invoice integration kept, checked against the hash it said, and copied into the container of this service
        byte[] bytes = await ReadAsync(message.Container, message.ObjectName, cancellationToken).ConfigureAwait(false) ?? throw new ImportFailure(ImportStage.Fetch, "The file of the invoice is not in the store of the firm invoice integration.");
        if (!string.Equals(Convert.ToHexStringLower(SHA256.HashData(bytes)), message.FileHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new ImportFailure(ImportStage.Fetch, "The file in the store is not the one that was received (its hash is not the one that was checked).");
        }

        // the work place: the file as it came (the zip) goes to firm-invoice-tmp/{queueId}_{name}; the queue id is unique, so there is no folder of the tenant
        string workPath = $"{TemporaryFolder}/{message.QueueId:N}_{name}";
        await SaveAsync(container, workPath, bytes, message.Zipped ? ZipContentType : XmlContentType, cancellationToken).ConfigureAwait(false);
        record.SetUploadPath(workPath);

        // extract: the one file of the zip is unpacked in memory and kept beside it, and the zip is taken away once the file is there
        byte[] content = bytes;
        string contentName = name;
        if (message.Zipped)
        {
            (contentName, content) = Unzip(bytes);
            string extractedPath = $"{TemporaryFolder}/{message.QueueId:N}_{contentName}";
            await SaveAsync(container, extractedPath, content, XmlContentType, cancellationToken).ConfigureAwait(false);
            await blobs.DeleteAsync(container, workPath, cancellationToken).ConfigureAwait(false);
            workPath = extractedPath;
            record.MovedObjects(null, extractedPath);
        }

        // validate: the file is read by its type into the UBL-TR invoice (an XML file is one already, the other forms are made into one with the tenant as the supplier), which follows the schema and says what the sender said
        EInvoiceProfile profile = EdxEInvoiceProfiles.ForTenant(record.TenantId) ?? throw new ImportFailure(ImportStage.Validate, "Your tenant has no e-invoice account (VKN, aliases): invoices cannot be made for it.");
        XElement ubl = ReadInvoice(message, content, contentName, profile);
        UblInvoiceSummary summary = Summarize(ubl, message, profile);

        // limit: what the sender may make, as the sender; it is checked on the draft, which has the total (nothing is written until it passes)
        string draftPath = $"{tenant}/sales/draft_{record.InvoiceId:N}.xml";
        try
        {
            record.MarkDrafted(
                new InvoiceDraftInfo(
                    summary.IssueDate,
                    summary.Currency,
                    summary.SupplierName,
                    summary.SupplierRegisterNumber,
                    summary.CustomerName,
                    summary.CustomerRegisterNumber,
                    [.. summary.Lines.Select(line => new InvoiceDraftLine(line.Description, line.Quantity, line.UnitPrice, line.VatRate, line.LineAmount + line.TaxAmount))],
                    summary.PayableAmount,
                    summary.ProfileId),
                draftPath,
                time.GetUtcNow());
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
        await SaveAsync(container, draftPath, UblXml.ToBytes(ubl), XmlContentType, cancellationToken).ConfigureAwait(false);

        // the draft is kept: the file in the work place is not needed any more
        await blobs.DeleteAsync(container, workPath, cancellationToken).ConfigureAwait(false);
        record.MovedObjects(null, null);
        await invoices.UpdateAsync(record, cancellationToken).ConfigureAwait(false);
        return ImportStage.Convert;
    }

    // the UBL-TR invoice of the file: an XML file is one (checked against the schema); a table, a text file or a model is read into the model of an invoice and made into one
    private static XElement ReadInvoice(InvoiceQueuedEto message, byte[] content, string fileName, EInvoiceProfile profile)
    {
        if (message.SourceType == InvoiceSourceType.Xml)
        {
            XElement document;
            try
            {
                document = InvoiceFileParser.ParseUbl(content);
            }
            catch (InvoiceFileException exception)
            {
                throw new ImportFailure(ImportStage.Validate, exception.Message);
            }

            IReadOnlyList<string> problems = UblSchemas.Default.Validate(document);
            return problems.Count == 0
                ? document
                : throw new ImportFailure(ImportStage.Validate, $"The XML does not follow the UBL-TR invoice schema: {string.Join(" | ", problems.Take(MaxProblemsInReason))}");
        }

        InvoiceSubmissionDto submission = Read(message, content, fileName);
        UblParty supplier = new(profile.RegisterNumber, profile.Title, profile.TaxOffice, profile.District, profile.City, profile.Country);
        UblParty customer = new(submission.CustomerTaxId, submission.CustomerName, submission.CustomerTaxOffice, submission.CustomerDistrict, submission.CustomerCity, string.IsNullOrWhiteSpace(submission.CustomerCountry) ? "Türkiye" : submission.CustomerCountry);
        XElement ubl = UblInvoiceBuilder.Build(new UblInvoice(
            submission.InvoiceNumber,
            submission.InvoiceId,
            submission.IssueDate,
            submission.Currency,
            supplier,
            customer,
            [.. submission.Lines.Select(line => new UblLine(line.Description, line.Quantity, string.IsNullOrWhiteSpace(line.UnitCode) ? "C62" : line.UnitCode, line.UnitPrice, line.VatRate))],
            string.IsNullOrWhiteSpace(submission.InvoiceProfile) ? UblInvoiceBuilder.ProfileId : submission.InvoiceProfile.ToUpperInvariant()));

        // what was made follows the schema, or the model allowed something the invoice cannot say
        IReadOnlyList<string> madeProblems = UblSchemas.Default.Validate(ubl);
        return madeProblems.Count == 0
            ? ubl
            : throw new ImportFailure(ImportStage.Convert, $"The invoice made of the file does not follow the UBL-TR invoice schema: {string.Join(" | ", madeProblems.Take(MaxProblemsInReason))}");
    }

    // what the invoice says of itself is what the sender said, and is an invoice this system makes (the basic scenario, a sale, in liras, from the tenant)
    private static UblInvoiceSummary Summarize(XElement ubl, InvoiceQueuedEto message, EInvoiceProfile profile)
    {
        UblInvoiceSummary summary;
        try
        {
            summary = UblInvoiceReader.Read(ubl);
        }
        catch (UblInvoiceException exception)
        {
            throw new ImportFailure(ImportStage.Validate, exception.Message);
        }

        if (summary.Uuid != message.InvoiceId)
        {
            throw new ImportFailure(ImportStage.Validate, $"The invoice id (UUID) in the file is {summary.Uuid}, and the one given with it is {message.InvoiceId}.");
        }

        if (!string.Equals(StringHelper.Normalize(summary.Number), StringHelper.Normalize(message.InvoiceNumber), StringComparison.Ordinal))
        {
            throw new ImportFailure(ImportStage.Validate, $"The invoice number (ID) in the file is '{summary.Number}', and the one given with it is '{message.InvoiceNumber}'.");
        }

        if (summary.SupplierRegisterNumber != profile.RegisterNumber)
        {
            throw new ImportFailure(ImportStage.Validate, $"The supplier of the invoice is {summary.SupplierRegisterNumber}, and an invoice sent by {profile.TenantCode} is issued by {profile.RegisterNumber}.");
        }

        if (!UblInvoiceBuilder.IsKnownProfile(summary.ProfileId) || summary.InvoiceTypeCode != UblInvoiceBuilder.InvoiceTypeCode)
        {
            throw new ImportFailure(ImportStage.Validate, $"Only the basic ({UblInvoiceBuilder.ProfileId}) and the commercial ({UblInvoiceBuilder.CommercialProfileId}) scenarios and the type {UblInvoiceBuilder.InvoiceTypeCode} are taken; the file says {summary.ProfileId} and {summary.InvoiceTypeCode}.");
        }

        return !string.Equals(summary.Currency, "TRY", StringComparison.Ordinal)
            ? throw new ImportFailure(ImportStage.Validate, $"Only invoices in TRY are taken; the file says {summary.Currency}.")
            : summary;
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

        if (!string.Equals(StringHelper.Normalize(submission.InvoiceNumber), StringHelper.Normalize(message.InvoiceNumber), StringComparison.Ordinal))
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
    private Task RefuseAsync(InvoiceQueuedEto message, SalesInvoice? live, CancellationToken cancellationToken)
    {
        string reason = live switch
        {
            null => $"The invoice number '{message.InvoiceNumber}' belongs to another invoice; send this one with a number of its own.",
            { Status: SalesInvoiceStatus.Processing } => "This invoice is being processed: wait for it to finish.",
            { Status: SalesInvoiceStatus.DraftCreated } => "This invoice has a draft that waits for approval. To send it again, reject the draft first, or use another invoice number.",
            { Status: SalesInvoiceStatus.Approved } => "This invoice was approved and is being signed; it cannot be sent again. Use another invoice number.",
            _ => "This invoice was approved and signed; it cannot be sent again. Use another invoice number.",
        };
        logger.LogInformation("Invoice {InvoiceId} ({QueueItemId}) was refused: {Reason}", message.InvoiceId, message.QueueId, reason);
        return eventBus.PublishAsync(new InvoiceImportFailedEto(message.QueueId, message.InvoiceId, ImportStage.Claim, reason), cancellationToken: cancellationToken);
    }

    // the record failed: its objects are named with error_ in front, the reason is kept, and the firm invoice integration is told
    private async Task FailAsync(SalesInvoice record, InvoiceQueuedEto message, string stage, string reason, CancellationToken cancellationToken)
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
