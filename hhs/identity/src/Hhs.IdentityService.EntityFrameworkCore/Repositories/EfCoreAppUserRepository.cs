using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Repositories;
using Hhs.IdentityService.Domain.Localization;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public class EfCoreAppUserRepository : EfCoreGenericRepository<AppUser, Guid>, IAppUserRepository
{
    [NotNull] protected IStringLocalizer L { get; }

    public EfCoreAppUserRepository(
        IServiceProvider provider,
        IStringLocalizerFactory stringLocalizerFactory,
        IdentityServiceDbContext dbContext
    ) : base(provider, dbContext)
    {
        // DefaultPropertySelector = new List<Expression<Func<AppContent, object>>> { x => x.Client };

        L = stringLocalizerFactory.CreateMultiple([typeof(IdentityServiceResource), typeof(ValidationResource), typeof(SharedResource)]);
    }

    public Task<AppUser> CreateAsync(Guid tenantId, string tenantDomain, string userName, string email, string phone, string name, string surname, string defaultLanguage, string avatarSuffixUrl, ICollection<string> roles = null, string plainPassword = null) => throw new NotImplementedException();

    public Task<AppUser> CreateAsync(Guid id, Guid tenantId, string tenantDomain, string userName, string email, string phone, string name, string surname, string defaultLanguage, string avatarSuffixUrl, ICollection<string> roles = null, string plainPassword = null) => throw new NotImplementedException();

    public Task<AppUser> UpdateAsync(Guid id, string userName, string email, string phone, string name, string surname, string defaultLanguage, ICollection<string> roles = null) => throw new NotImplementedException();

    public Task DeleteAsync(Guid id) => throw new NotImplementedException();
}