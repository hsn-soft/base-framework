using System.Threading;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Threading;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.AspNetCore.Threading;

public class HttpContextCancellationTokenProvider : CancellationTokenProviderBase, ITransientDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCancellationTokenProvider(
        IAmbientScopeProvider<CancellationTokenOverride> cancellationTokenOverrideScopeProvider,
        IHttpContextAccessor httpContextAccessor)
        : base(cancellationTokenOverrideScopeProvider)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override CancellationToken Token
    {
        get
        {
            if (OverrideValue != null)
            {
                return OverrideValue.CancellationToken;
            }

            return _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        }
    }
}