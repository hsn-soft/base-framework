using System;
using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Data;

public class DefaultConnectionStringResolver : IConnectionStringResolver, ITransientDependency
{
    public DefaultConnectionStringResolver(
        IOptionsMonitor<BaseDbConnectionOptions> options)
    {
        Options = options.CurrentValue;
    }

    protected BaseDbConnectionOptions Options { get; }

    [Obsolete("Use ResolveAsync method.")]
    public virtual string Resolve(string connectionStringName = null)
    {
        return ResolveInternal(connectionStringName);
    }

    public virtual Task<string> ResolveAsync(string connectionStringName = null)
    {
        return Task.FromResult(ResolveInternal(connectionStringName));
    }

    private string ResolveInternal(string connectionStringName)
    {
        if (connectionStringName == null)
        {
            return Options.ConnectionStrings.Default;
        }

        var connectionString = Options.GetConnectionStringOrNull(connectionStringName);

        if (!connectionString.IsNullOrEmpty())
        {
            return connectionString;
        }

        return null;
    }
}