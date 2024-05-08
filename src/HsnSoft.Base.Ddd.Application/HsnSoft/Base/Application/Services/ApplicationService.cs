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
    [NotNull]
    protected IClock Clock { get; }

    [NotNull]
    protected IDataFilter DataFilter { get; }

    [NotNull]
    protected ICurrentUser CurrentUser { get; }

    [NotNull]
    protected ICurrentTenant CurrentTenant { get; }

    [NotNull]
    protected IStringLocalizerFactory StringLocalizerFactory { get; }

    [NotNull]
    protected ILoggerFactory LoggerFactory { get; }

    protected ApplicationService(IServiceProvider provider)
    {
        var serviceProvider = provider ?? throw new ArgumentNullException(nameof(provider), "ApplicationService IServiceProvider is null");
        Clock = serviceProvider.GetRequiredService<IClock>();
        DataFilter = serviceProvider.GetRequiredService<IDataFilter>();
        CurrentUser = serviceProvider.GetRequiredService<ICurrentUser>();
        CurrentTenant = serviceProvider.GetRequiredService<ICurrentTenant>();
        StringLocalizerFactory = serviceProvider.GetRequiredService<IStringLocalizerFactory>();
        LoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    }
}