using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Guids;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Users;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Application.Services;

public abstract class BaseApplicationService :
    IApplicationService,
    IAuditingEnabled,
    ITransientDependency
{
    protected IBaseLazyServiceProvider LazyServiceProvider { get; set; }

    protected ICurrentUser CurrentUser => this.LazyServiceProvider.LazyGetRequiredService<ICurrentUser>();

    protected ICurrentTenant CurrentTenant => this.LazyServiceProvider.LazyGetRequiredService<ICurrentTenant>();

    protected IDataFilter DataFilter => this.LazyServiceProvider.LazyGetRequiredService<IDataFilter>();

    protected IGuidGenerator GuidGenerator => this.LazyServiceProvider.LazyGetService<IGuidGenerator>((IGuidGenerator)SimpleGuidGenerator.Instance);

    protected IStringLocalizerFactory StringLocalizerFactory => this.LazyServiceProvider.LazyGetRequiredService<IStringLocalizerFactory>();
}