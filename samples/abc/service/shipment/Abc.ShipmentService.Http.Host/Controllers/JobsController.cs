using Abc.ShipmentService.Application.Contracts.PaymentDomain.Dtos;
using Abc.ShipmentService.Application.Contracts.PaymentDomain.Services;
using Abc.ShipmentService.Controllers.Base;
using HsnSoft.Base.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abc.ShipmentService.Controllers;

/// <summary>
/// The jobs of the shipment service, called by the scheduler of the system (<c>Abc.Scheduler.Host</c>, by the list of jobs in its settings). The caller is not a user, so there is no token: the key of the scheduler
/// (<c>Scheduler:ApiKey</c>, the header <c>SchedulerApiKey</c>) is the authentication of the job, and the service does not start without it.
/// </summary>
[AllowAnonymous]
[SchedulerAuth]
[Route("api/shipment-service/v1/commercial/jobs")]
public sealed class JobsController(IPaymentAppService paymentAppService) : BaseServiceController
{
    /// <summary>Counts the payments of each status and writes it to the log (a job that only looks, so it is safe to run as often as the scheduler likes).</summary>
    [HttpPost("payment-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<PaymentSummaryDto> PaymentSummaryAsync(CancellationToken cancellationToken = default)
    {
        PaymentSummaryDto summary = await paymentAppService.GetSummaryAsync(cancellationToken);
        Logger.LogInformation("Payments: {Pending} pending, {Succeeded} succeeded, {Failed} failed, {Refunded} refunded", summary.Pending, summary.Succeeded, summary.Failed, summary.Refunded);
        return summary;
    }
}
