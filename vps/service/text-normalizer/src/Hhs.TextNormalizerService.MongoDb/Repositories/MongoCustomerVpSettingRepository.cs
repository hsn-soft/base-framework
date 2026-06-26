using Hhs.TextNormalizerService.Domain.SettingDomain.Entities;
using Hhs.TextNormalizerService.Domain.SettingDomain.Repositories;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.MongoDb.Repositories;

public sealed class MongoCustomerVpSettingRepository(
    IServiceProvider provider,
    TextNormalizerServiceDbContext dbContext
) : MongoGenericRepository<CustomerVpSetting, Guid>(provider, dbContext), ICustomerVpSettingRepository
{
    public async Task<KeyValuePair<bool, string>> GetOutlineProviderKeyByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default)
    {
        string result = await GetSingleOrDefaultAsync(
            predicate: x => x.ScopeKey == scopeKey,
            selector: x => x.OutlineProviderKey, cancellationToken: cancellationToken);

        return string.IsNullOrWhiteSpace(result)
            ? new KeyValuePair<bool, string>(false, null)
            : new KeyValuePair<bool, string>(true, result);
    }
}