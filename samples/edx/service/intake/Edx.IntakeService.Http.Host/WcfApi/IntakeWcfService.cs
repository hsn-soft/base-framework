using CoreWCF;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Submits;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Services;
using Edx.IntakeService.Domain.StoredFileDomain.Entities;
using Edx.IntakeService.Http.Host.Validation;
using Edx.Shared.Helper;
using HsnSoft.Base;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Edx.IntakeService.Http.Host.WcfApi;

/// <summary>
/// The WCF entry point. WCF has no part in the authentication of the host, so each call asks the JWT scheme who the caller is (the token is in the HTTP header, as for every other entry point) before anything else; what it
/// needs of the application it takes from the request's own scope, so the user and the tenant of the token are the ones the application service sees.
/// </summary>
public sealed class IntakeWcfService(IHttpContextAccessor accessor, ILogger<IntakeWcfService> logger) : IIntakeWcfService
{
    public Task<StoredFileResult> UploadXmlArchiveAsync(ArchiveUploadRequest request)
        => RunAsync(EdxEndpointPermissions.Intake.Archives.UploadXml, async (intake, cancellationToken) =>
        {
            using MemoryStream content = new(Required(request).Content, writable: false);
            return await intake.ReceiveXmlArchiveAsync(request.FileName, content, IntakeChannel.Wcf, cancellationToken).ConfigureAwait(false);
        });

    public Task<StoredFileResult> UploadTabularArchiveAsync(ArchiveUploadRequest request)
        => RunAsync(EdxEndpointPermissions.Intake.Archives.UploadTabular, async (intake, cancellationToken) =>
        {
            using MemoryStream content = new(Required(request).Content, writable: false);
            return await intake.ReceiveTabularArchiveAsync(request.FileName, content, IntakeChannel.Wcf, cancellationToken).ConfigureAwait(false);
        });

    public Task<StoredFileResult> SubmitInvoiceAsync(InvoiceSubmitRequest request)
        => RunAsync(EdxEndpointPermissions.Intake.Models.SubmitInvoice, async (intake, cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            ModelValidation.Ensure(request.Invoice);
            ModelFormat format = Enum.TryParse(request.Format, ignoreCase: true, out ModelFormat parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new BusinessException("model_format_invalid", $"The format '{request.Format}' is not Json or Xml.");
            return await intake.ReceiveInvoiceModelAsync(request.Invoice, format, IntakeChannel.Wcf, cancellationToken).ConfigureAwait(false);
        });

    private static ArchiveUploadRequest Required(ArchiveUploadRequest? request)
        => request is { Content.Length: > 0 } ? request : throw new BusinessException("upload_empty", "The request carries no content.");

    private async Task<StoredFileResult> RunAsync(string permission, Func<IIntakeAppService, CancellationToken, Task<StoredFileDto>> call)
    {
        HttpContext http = accessor.HttpContext ?? throw Fault("internal_error", "The call has no HTTP context.");

        AuthenticateResult authentication = await http.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme).ConfigureAwait(false);
        if (!authentication.Succeeded)
        {
            logger.LogWarning("WCF call refused: the token is missing or not valid");
            throw Fault("unauthorized", "A valid bearer token is required in the Authorization header.");
        }

        http.User = authentication.Principal;
        try
        {
            // the same permissions as the other entry points ask: the attribute does not reach a WCF operation, so it is asked here
            if (!await http.RequestServices.GetRequiredService<IPermissionChecker>().IsGrantedAsync(permission, http.RequestAborted).ConfigureAwait(false))
            {
                logger.LogWarning("WCF call refused: the permission {Permission} was not granted", permission);
                throw Fault("forbidden", $"The permission '{permission}' is required.");
            }

            StoredFileDto file = await call(http.RequestServices.GetRequiredService<IIntakeAppService>(), http.RequestAborted).ConfigureAwait(false);
            return ToResult(file);
        }
        catch (BusinessException business)
        {
            logger.LogWarning("WCF call refused: {ErrorCode} {ErrorMessage}", business.ErrorCode, business.Message);
            throw Fault(business.ErrorCode ?? "business_rule", business.Message);
        }
        catch (BusinessRuleException rule)
        {
            logger.LogWarning("WCF call refused: {ErrorMessage}", rule.Message);
            throw Fault(rule.MessageKey ?? "business_rule", rule.Message);
        }
        catch (NotFoundException notFound)
        {
            logger.LogWarning("WCF call found nothing: {ErrorMessage}", notFound.Message);
            throw Fault(notFound.MessageKey ?? "not_found", notFound.Message);
        }
        catch (Exception exception) when (exception is not FaultException)
        {
            logger.LogError(exception, "WCF call failed ({TraceId})", http.TraceIdentifier);
            throw Fault("internal_error", $"The call failed. Trace id: {http.TraceIdentifier}");
        }
    }

    private static FaultException<IntakeFault> Fault(string code, string message)
        => new(new IntakeFault { Code = code, Message = message }, new FaultReason(message), new FaultCode("Sender"));

    private static StoredFileResult ToResult(StoredFileDto file) => new()
    {
        Id = file.Id,
        Kind = file.Kind,
        Channel = file.Channel,
        FileName = file.FileName,
        ContentType = file.ContentType,
        SizeBytes = file.SizeBytes,
        FolderPath = file.FolderPath,
        Container = file.Container,
        ObjectName = file.ObjectName,
        EntryCount = file.Entries.Count,
        TenantId = file.TenantId,
    };
}
