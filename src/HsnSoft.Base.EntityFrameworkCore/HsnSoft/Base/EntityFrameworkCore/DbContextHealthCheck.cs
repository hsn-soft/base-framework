using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HsnSoft.Base.EntityFrameworkCore;

internal sealed class DbContextHealthCheck<TContext>(TContext context) : IHealthCheck
    where TContext : DbContext
{
    private readonly TContext _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => await _context.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("cannot connect");
}
