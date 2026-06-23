using Hhs.VideoGeneratorService.Domain.SettingDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.VideoGeneratorService.Domain.SettingDomain.Repositories;

public interface ICustomerVpSettingRepository : IReadOnlyGenericRepository<CustomerVpSetting, Guid>
{
}