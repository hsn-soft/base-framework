using System;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Guids;
using HsnSoft.Base.Linq;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Timing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HsnSoft.Base.Domain.Services;

public abstract class DomainService : IDomainService
{
    public IBaseLazyServiceProvider LazyServiceProvider { get; set; }

    [Obsolete("Use LazyServiceProvider instead.")]
    public IServiceProvider ServiceProvider { get; set; }

    protected IClock Clock => LazyServiceProvider.LazyGetRequiredService<IClock>();

    protected IGuidGenerator GuidGenerator => LazyServiceProvider.LazyGetService<IGuidGenerator>(SimpleGuidGenerator.Instance);

    protected ILoggerFactory LoggerFactory => LazyServiceProvider.LazyGetRequiredService<ILoggerFactory>();

    protected ICurrentTenant CurrentTenant => LazyServiceProvider.LazyGetRequiredService<ICurrentTenant>();

    protected IAsyncQueryableExecuter AsyncExecuter => LazyServiceProvider.LazyGetRequiredService<IAsyncQueryableExecuter>();

    protected ILogger Logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName) ?? NullLogger.Instance);
}