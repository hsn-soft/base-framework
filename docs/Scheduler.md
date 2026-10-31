# The endpoints a scheduler calls

A service that does something on a schedule (look at its payments every minute, close what is overdue) does not run the schedule itself: a **scheduler** of the system calls an endpoint of the service on a cron schedule, and the service does the work.
This is the part of the service ([ADR-0126](adr/0126-the-endpoints-a-scheduler-calls-are-authenticated-by-a-key-the-host-requires.md)); a scheduler is a product's own host (the sample has one, `Abc.Scheduler.Host`, on Quartz).

```csharp
builder.Services.AddBaseSchedulerAuth(builder.Configuration);   // reads Scheduler:ApiKey and REQUIRES it: no key, no start
```

```csharp
[AllowAnonymous]                       // a scheduler has no token: the key is the authentication of the job
[SchedulerAuth]
[Route("api/shipment-service/v1/commercial/jobs")]
public sealed class JobsController(IPaymentAppService payments) : BaseServiceController
{
    [HttpPost("payment-summary")]
    public Task<PaymentSummaryDto> PaymentSummaryAsync(CancellationToken cancellationToken) => payments.GetSummaryAsync(cancellationToken);
}
```

- The scheduler sends the key in the header `SchedulerApiKey`; the service compares it with `Scheduler:ApiKey` **in constant time**. A missing, empty or wrong key is a 401 (the answer never has the key or what was sent). A user's token is not enough, and the key is not a user.
- `Scheduler:ApiKey` is a secret: not in a file of the repository. It comes from the environment (`Scheduler__ApiKey`), a `.env` file (`AddBaseHosting` reads it) or the secret of the platform; the same key is in the scheduler and in every service it calls.
- The scheduler also sends `SchedulerJobId` (the run, also the correlation id: the logs of the job and of what it causes are one chain) and `SchedulerJobName`.
- A job is written to be **called again** (the scheduler does not retry, a run that fails is logged and the next one goes on) and to be safe when two runs meet (a service with two pods gets a run on one of them; a job that must not overlap says so in its own lock).
- **Run one scheduler.** Two would call every job twice.
