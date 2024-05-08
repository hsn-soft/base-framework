using System;
using HsnSoft.Base.Data;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Timing;
using HsnSoft.Base.Users;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Application.Services;

public abstract class ApplicationService : IApplicationService, IScopedDependency
{
    [CanBeNull]
    protected IClock Clock { get; }

    [CanBeNull]
    protected IDataFilter DataFilter { get; }

    [CanBeNull]
    protected ICurrentUser CurrentUser { get; }

    [CanBeNull]
    protected ICurrentTenant CurrentTenant { get; }

    [CanBeNull]
    protected IStringLocalizerFactory StringLocalizerFactory { get; }

    [CanBeNull]
    protected ILoggerFactory LoggerFactory { get; }

    protected ApplicationService(IServiceProvider provider = null)
    {
        Clock = provider?.GetService<IClock>();
        DataFilter = provider?.GetService<IDataFilter>();
        CurrentUser = provider?.GetService<ICurrentUser>();
        CurrentTenant = provider?.GetService<ICurrentTenant>();
        StringLocalizerFactory = provider?.GetService<IStringLocalizerFactory>();
        LoggerFactory = provider?.GetService<ILoggerFactory>();
    }
}