using System.Globalization;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Submits;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Services;
using Edx.IntakeService.Domain.StoredFileDomain.Entities;
using Edx.IntakeService.Http.Host.Validation;
using Edx.Shared.Helper;
using Grpc.Core;
using HsnSoft.Base;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using ApplicationModelFormat = Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Submits.ModelFormat;

namespace Edx.IntakeService.Http.Host.GrpcApi;

/// <summary>The gRPC entry point: it turns what came on the wire into what the application service takes, calls it, and turns the record back. The rules are the application service's, the same as for REST and WCF.</summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class IntakeGrpcService(IIntakeAppService intake) : Intake.IntakeBase
{
    [PermissionAuthorize(EdxEndpointPermissions.Intake.Archives.UploadXml)]
    public override Task<StoredFileReply> UploadXmlArchive(IAsyncStreamReader<UploadChunk> requestStream, ServerCallContext context)
        => UploadAsync(requestStream, context, intake.ReceiveXmlArchiveAsync);

    [PermissionAuthorize(EdxEndpointPermissions.Intake.Archives.UploadTabular)]
    public override Task<StoredFileReply> UploadTabularArchive(IAsyncStreamReader<UploadChunk> requestStream, ServerCallContext context)
        => UploadAsync(requestStream, context, intake.ReceiveTabularArchiveAsync);

    [PermissionAuthorize(EdxEndpointPermissions.Intake.Models.SubmitInvoice)]
    public override async Task<StoredFileReply> SubmitInvoice(SubmitInvoiceRequest request, ServerCallContext context)
    {
        InvoiceSubmissionDto model = ToModel(request.Invoice ?? throw new BusinessException("invoice_required", "The invoice is required."));
        ModelValidation.Ensure(model);

        ApplicationModelFormat format = request.Format == ModelFormat.Xml ? ApplicationModelFormat.Xml : ApplicationModelFormat.Json;
        return ToReply(await intake.ReceiveInvoiceModelAsync(model, format, IntakeChannel.Grpc, context.CancellationToken).ConfigureAwait(false));
    }

    private static async Task<StoredFileReply> UploadAsync(
        IAsyncStreamReader<UploadChunk> requestStream,
        ServerCallContext context,
        Func<string, Stream, IntakeChannel, CancellationToken, Task<StoredFileDto>> receive)
    {
        if (!await requestStream.MoveNext(context.CancellationToken).ConfigureAwait(false) || requestStream.Current.PayloadCase != UploadChunk.PayloadOneofCase.FileName)
        {
            throw new BusinessException("upload_first_message_names_the_file", "The first message of an upload names the file.");
        }

        string fileName = requestStream.Current.FileName;
        await using ChunkedReadStream content = new(requestStream);
        return ToReply(await receive(fileName, content, IntakeChannel.Grpc, context.CancellationToken).ConfigureAwait(false));
    }

    private static InvoiceSubmissionDto ToModel(InvoiceSubmission message) => new()
    {
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

    private static StoredFileReply ToReply(StoredFileDto file) => new()
    {
        Id = file.Id.ToString(),
        Kind = file.Kind,
        Channel = file.Channel,
        FileName = file.FileName,
        ContentType = file.ContentType,
        SizeBytes = file.SizeBytes,
        FolderPath = file.FolderPath,
        Container = file.Container,
        ObjectName = file.ObjectName,
        EntryCount = file.Entries.Count,
        TenantId = file.TenantId.ToString(),
    };
}
