using Hhs.TextNormalizerService.Domain.SettingDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.Domain.SettingDomain.Repositories;

public interface ICustomerVpSettingRepository : IMongoGenericRepository<CustomerVpSetting, Guid>
{
}