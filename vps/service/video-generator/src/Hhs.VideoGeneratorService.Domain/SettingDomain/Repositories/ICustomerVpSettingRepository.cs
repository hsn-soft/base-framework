using Hhs.VideoGeneratorService.Domain.SettingDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.VideoGeneratorService.Domain.SettingDomain.Repositories;

public interface ICustomerVpSettingRepository : IMongoGenericRepository<CustomerVpSetting, Guid>
{
    Task<KeyValuePair<bool,string>> GetAudioProviderKeyByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default);

    Task<KeyValuePair<bool,string>> GetVideoProviderKeyByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default);

}