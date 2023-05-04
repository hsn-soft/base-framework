using HsnSoft.Base.Auditing;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Guids;

namespace HsnSoft.Base.Application.Services;

public abstract class ApplicationService :
    IApplicationService,
    IAuditingEnabled,
    ITransientDependency
{
    protected IGuidGenerator GuidGenerator => SimpleGuidGenerator.Instance;
}