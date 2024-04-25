using System;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Guids;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Application.Services;

public abstract class BaseApplicationService :
    IApplicationService,
    IAuditingEnabled,
    ITransientDependency
{
    protected IServiceProvider ServiceProvider { get; set; }

    protected IDataFilter DataFilter => this.ServiceProvider.GetRequiredService<IDataFilter>();

    protected IGuidGenerator GuidGenerator => SimpleGuidGenerator.Instance;

    protected ICurrentUser CurrentUser => this.ServiceProvider.GetRequiredService<ICurrentUser>();

    protected ICurrentTenant CurrentTenant => this.ServiceProvider.GetRequiredService<ICurrentTenant>();

    protected IStringLocalizerFactory StringLocalizerFactory => this.ServiceProvider.GetRequiredService<IStringLocalizerFactory>();
}