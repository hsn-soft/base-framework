using System;
using HsnSoft.Base.Data;
using HsnSoft.Base.Guids;
using HsnSoft.Base.Linq;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Timing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HsnSoft.Base.Domain.Services;

public abstract class DomainService : IDomainService
{
    protected IServiceProvider ServiceProvider { get; set; }

    protected IClock Clock => ServiceProvider.GetRequiredService<IClock>();

    protected IDataFilter DataFilter => ServiceProvider.GetRequiredService<IDataFilter>();

    protected IGuidGenerator GuidGenerator => SimpleGuidGenerator.Instance;

    protected ILoggerFactory LoggerFactory => ServiceProvider.GetRequiredService<ILoggerFactory>();

    protected ICurrentTenant CurrentTenant => ServiceProvider.GetRequiredService<ICurrentTenant>();

    protected IAsyncQueryableExecuter AsyncExecuter => ServiceProvider.GetRequiredService<IAsyncQueryableExecuter>();

    protected IStringLocalizerFactory StringLocalizerFactory => ServiceProvider.GetRequiredService<IStringLocalizerFactory>();

    protected ILogger Logger => LoggerFactory?.CreateLogger(GetType().FullName) ?? NullLogger.Instance;
}