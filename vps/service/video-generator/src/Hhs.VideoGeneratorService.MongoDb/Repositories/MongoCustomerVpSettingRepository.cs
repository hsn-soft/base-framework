using Hhs.VideoGeneratorService.Domain.SettingDomain.Entities;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Repositories;
using Hhs.VideoGeneratorService.MongoDb.Context;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.VideoGeneratorService.MongoDb.Repositories;

public sealed class MongoCustomerVpSettingRepository(
    IServiceProvider provider,
    VideoGeneratorServiceDbContext dbContext
) : MongoGenericRepository<CustomerVpSetting, Guid>(provider, dbContext), ICustomerVpSettingRepository
{
    public async Task<KeyValuePair<bool, string>> GetAudioProviderKeyByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default)
    {
        string result = await GetSingleOrDefaultAsync(
            predicate: x => x.ScopeKey == scopeKey,
            selector: x => x.AudioProviderKey, cancellationToken: cancellationToken);

        return string.IsNullOrWhiteSpace(result)
            ? new KeyValuePair<bool, string>(false, null)
            : new KeyValuePair<bool, string>(true, result);
    }

    public async Task<KeyValuePair<bool, string>> GetVideoProviderKeyByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default)
    {
        string result = await GetSingleOrDefaultAsync(
            predicate: x => x.ScopeKey == scopeKey,
            selector: x => x.VideoProviderKey, cancellationToken: cancellationToken);

        return string.IsNullOrWhiteSpace(result)
            ? new KeyValuePair<bool, string>(false, null)
            : new KeyValuePair<bool, string>(true, result);
    }
}