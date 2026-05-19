using System.Net;
using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Users;
using HsnSoft.Base.Validation.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace Hhs.Shared.Hosting.Microservices.Middlewares;

public sealed class UserTenantCheckerMiddleware : IMiddleware
{
    private readonly ICurrentUser _currentUser;
    private readonly IDataFilter _dataFilter;
    private readonly IStringLocalizer _localizer;

    public UserTenantCheckerMiddleware(IDataFilter dataFilter, ICurrentUser currentUser, IStringLocalizerFactory factory)
    {
        _dataFilter = dataFilter;
        _currentUser = currentUser;
        _localizer = factory.CreateMultiple([typeof(ValidationResource), typeof(SharedResource)]);
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (_currentUser is null) throw new BaseHttpException((int)HttpStatusCode.Forbidden);
        // if (_currentUser.IsAuthenticated)
        // {
        //     if (string.IsNullOrWhiteSpace(_currentUser.TenantDomain))
        //     {
        //         throw new BaseHttpException((int)HttpStatusCode.Forbidden, string.Format(_localizer?["Error:UnknownField"].ToString() ?? "", nameof(_currentUser.TenantDomain)));
        //     }
        //
        //     if (!_currentUser.TenantDomain.Equals(NameConsts.System) && _currentUser.TenantId is null)
        //     {
        //         throw new BaseHttpException((int)HttpStatusCode.Forbidden, string.Format(_localizer?["Error:UnknownField"].ToString() ?? "", nameof(_currentUser.TenantId)));
        //     }
        //
        //     if (_currentUser.TenantDomain.Equals(NameConsts.System))
        //     {
        //         using (_dataFilter.Disable<IMultiTenant>())
        //         {
        //             await next(context);
        //             return;
        //         }
        //     }
        // }

        await next(context);
    }
}