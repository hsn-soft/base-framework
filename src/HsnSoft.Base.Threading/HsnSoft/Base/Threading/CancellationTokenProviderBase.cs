using System;
using System.Threading;

namespace HsnSoft.Base.Threading;

public abstract class CancellationTokenProviderBase : ICancellationTokenProvider
{
    public const string CancellationTokenOverrideContextKey = "HsnSoft.Base.Threading.CancellationToken.Override";

    protected CancellationTokenProviderBase(IAmbientScopeProvider<CancellationTokenOverride> cancellationTokenOverrideScopeProvider)
    {
        CancellationTokenOverrideScopeProvider = cancellationTokenOverrideScopeProvider;
    }

    protected IAmbientScopeProvider<CancellationTokenOverride> CancellationTokenOverrideScopeProvider { get; }

    protected CancellationTokenOverride OverrideValue => CancellationTokenOverrideScopeProvider.GetValue(CancellationTokenOverrideContextKey);

    public abstract CancellationToken Token { get; }

    public IDisposable Use(CancellationToken cancellationToken)
    {
        return CancellationTokenOverrideScopeProvider.BeginScope(CancellationTokenOverrideContextKey, new CancellationTokenOverride(cancellationToken));
    }
}