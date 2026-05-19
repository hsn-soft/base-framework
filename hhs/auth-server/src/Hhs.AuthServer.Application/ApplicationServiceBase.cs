using Hhs.AuthServer.Application.Localization;
using Hhs.IdentityService.Domain.Localization;
using Hhs.Shared.Localization;
using HsnSoft.Base.Application.Services;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.AuthServer.Application;

public abstract class ApplicationServiceBase : ApplicationService
{
    [NotNull]
    protected IStringLocalizer L { get; }

    protected ApplicationServiceBase(IServiceProvider provider) : base(provider)
    {
        L = StringLocalizerFactory.CreateMultiple(new List<Type>
        {
            typeof(AuthServerResource),
            typeof(IdentityServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        });
    }
}