using System.Globalization;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Submits;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Services;
using Edx.Shared.Contracts.Invoicing;
using Edx.Shared.Helper;
using Grpc.Core;
using HsnSoft.Base;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using ApplicationModelFormat = Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Submits.ModelFormat;

namespace Edx.IntakeService.Grpc.Host;

/// <summary>The gRPC entry point: it turns what came on the wire into what the application service takes, calls it, and turns the queue item back. The rules are the application service's, the same as for REST and WCF.</summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class IntakeGrpcService(IIntakeAppService intake) : Intake.IntakeBase
{
    [PermissionAuthorize(EdxEndpointPermissions.Intake.Upload)]
    public override async Task<QueueReply> UploadInvoiceFile(IAsyncStreamReader<UploadChunk> requestStream, ServerCallContext context)
    {
        if (!await requestStream.MoveNext(context.CancellationToken).ConfigureAwait(false) || requestStream.Current.PayloadCase != UploadChunk.PayloadOneofCase.Header)
        {
            throw new BusinessException("upload_first_message_is_the_header", "The first message of an upload is its header.");
        }

        UploadHeader header = requestStream.Current.Header;
        InvoiceUploadDto input = new()
        {
            InvoiceId = Guid.TryParse(header.InvoiceId, out Guid invoiceId) ? invoiceId : null,
            InvoiceNumber = header.InvoiceNumber,
            FileHash = header.FileHash,
            SourceType = header.SourceType == Edx.IntakeService.Grpc.Host.SourceType.Unspecified ? null : (InvoiceSourceType)(int)header.SourceType,
            FileName = header.FileName,
        };
        await using ChunkedReadStream content = new(requestStream);
        return ToReply(await intake.QueueFileAsync(input, content, InvoiceChannelType.ServiceUploadFile, context.CancellationToken).ConfigureAwait(false));
    }

    [PermissionAuthorize(EdxEndpointPermissions.Intake.SubmitModel)]
    public override async Task<QueueReply> SubmitInvoice(SubmitInvoiceRequest request, ServerCallContext context)
    {
        InvoiceSubmissionDto model = ToModel(request.Invoice ?? throw new BusinessException("invoice_required", "The invoice is required."));
        ApplicationModelFormat format = request.Format == ModelFormat.Xml ? ApplicationModelFormat.Xml : ApplicationModelFormat.Json;
        return ToReply(await intake.QueueModelAsync(model, format, InvoiceChannelType.ServiceDto, context.CancellationToken).ConfigureAwait(false));
    }

    private static InvoiceSubmissionDto ToModel(InvoiceSubmission message) => new()
    {
        InvoiceId = Guid.TryParse(message.InvoiceId, out Guid invoiceId) ? invoiceId : Guid.Empty,
        InvoiceNumber = message.InvoiceNumber,
        IssueDate = Date(message.IssueDate, "issue_date"),
        Currency = message.Currency,
        SupplierName = message.SupplierName,
        SupplierTaxId = Optional(message.SupplierTaxId),
        CustomerName = message.CustomerName,
        CustomerTaxId = Optional(message.CustomerTaxId),
        Lines = [.. message.Lines.Select(line => new InvoiceLineDto
        {
            Description = line.Description,
            Quantity = Number(line.Quantity, "quantity"),
            UnitPrice = Number(line.UnitPrice, "unit_price"),
            VatRate = Number(line.VatRate, "vat_rate"),
        })],
    };

    private static string? Optional(string text) => string.IsNullOrWhiteSpace(text) ? null : text;

    private static DateTime Date(string text, string field)
        => DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)
            ? date
            : throw new BusinessException("date_invalid", $"{field} must be a date as yyyy-MM-dd.");

    private static decimal Number(string text, string field)
        => decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number)
            ? number
            : throw new BusinessException("number_invalid", $"{field} must be a number with a dot for decimals.");

    private static QueueReply ToReply(QueueItemDto item) => new()
    {
        QueueId = item.Id.ToString(),
        InvoiceId = item.InvoiceId.ToString(),
        InvoiceNumber = item.InvoiceNumber,
        SourceType = item.SourceType,
        ChannelType = item.ChannelType,
        Status = item.Status,
        FileName = item.FileName,
        FileHash = item.FileHash,
        SizeBytes = item.SizeBytes,
        TenantId = item.TenantId.ToString(),
    };
}
