using CoreWCF;
using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Dtos;
using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Dtos.Submits;
using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Services;
using Edx.Shared.Contracts.Invoicing;
using Edx.Shared.Helper;
using HsnSoft.Base;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Edx.Integration.FirmInvoice.Wcf.Host;

/// <summary>
/// The WCF entry point. WCF has no part in the authentication of the host, so each call asks the JWT scheme who the caller is (the token is in the HTTP header, as for every other entry point) before anything else; what it
/// needs of the application it takes from the request's own scope, so the user and the tenant of the token are the ones the application service sees.
/// </summary>
public sealed class FirmInvoiceWcfService(IHttpContextAccessor accessor, ILogger<FirmInvoiceWcfService> logger) : IFirmInvoiceWcfService
{
    public Task<QueueItemResult> UploadInvoiceFileAsync(InvoiceFileUploadRequest request)
        => RunAsync(EdxEndpointPermissions.FirmInvoice.Upload, async (firmInvoice, cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            using MemoryStream content = new(request.Content, writable: false);
            InvoiceUploadDto input = new() { InvoiceId = request.InvoiceId, InvoiceNumber = request.InvoiceNumber, FileHash = request.FileHash, SourceType = request.SourceType, FileName = request.FileName };
            return await firmInvoice.QueueFileAsync(input, content, InvoiceChannelType.ServiceUploadFile, cancellationToken).ConfigureAwait(false);
        });

    public Task<QueueItemResult> SubmitInvoiceAsync(InvoiceSubmitRequest request)
        => RunAsync(EdxEndpointPermissions.FirmInvoice.SubmitModel, async (firmInvoice, cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            ModelFormat format = Enum.TryParse(request.Format, ignoreCase: true, out ModelFormat parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new BusinessException("model_format_invalid", $"The format '{request.Format}' is not Json or Xml.");
            return await firmInvoice.QueueModelAsync(request.Invoice, format, InvoiceChannelType.ServiceDto, cancellationToken).ConfigureAwait(false);
        });

    private async Task<QueueItemResult> RunAsync(string permission, Func<IFirmInvoiceAppService, CancellationToken, Task<QueueItemDto>> call)
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

            QueueItemDto queued = await call(http.RequestServices.GetRequiredService<IFirmInvoiceAppService>(), http.RequestAborted).ConfigureAwait(false);
            return ToResult(queued);
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
        catch (BaseHttpException refused) when (refused.HttpStatusCode == StatusCodes.Status403Forbidden)
        {
            logger.LogWarning("WCF call refused: {ErrorMessage}", refused.Message);
            throw Fault(refused.MessageKey ?? "forbidden", refused.Message);
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

    private static FaultException<FirmInvoiceFault> Fault(string code, string message)
        => new(new FirmInvoiceFault { Code = code, Message = message }, new FaultReason(message), new FaultCode("Sender"));

    private static QueueItemResult ToResult(QueueItemDto item) => new()
    {
        QueueId = item.Id,
        InvoiceId = item.InvoiceId,
        InvoiceNumber = item.InvoiceNumber,
        SourceType = item.SourceType,
        ChannelType = item.ChannelType,
        Status = item.Status,
        FileName = item.FileName,
        FileHash = item.FileHash,
        SizeBytes = item.SizeBytes,
        TenantId = item.TenantId,
    };
}
