using System;
using HsnSoft.Base.Guids;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HsnSoft.Base.AspNetCore.Mvc;

public abstract class ApiControllerBase : ControllerBase
{
    protected IServiceProvider ServiceProvider { get; set; }

    protected IGuidGenerator GuidGenerator => SimpleGuidGenerator.Instance;

    protected ILoggerFactory LoggerFactory => ServiceProvider.GetRequiredService<ILoggerFactory>();

    protected ILogger Logger => LoggerFactory?.CreateLogger(GetType().FullName) ?? NullLogger.Instance;

    protected IAuthorizationService AuthorizationService => ServiceProvider.GetRequiredService<IAuthorizationService>();
    protected ICurrentUser CurrentUser => this.ServiceProvider.GetRequiredService<ICurrentUser>();

    protected ICurrentTenant CurrentTenant => this.ServiceProvider.GetRequiredService<ICurrentTenant>();

    protected IStringLocalizerFactory StringLocalizerFactory => ServiceProvider.GetRequiredService<IStringLocalizerFactory>();
}