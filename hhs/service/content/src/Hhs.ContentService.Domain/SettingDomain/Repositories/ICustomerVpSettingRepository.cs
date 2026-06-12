using Hhs.ContentService.Domain.SettingDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.ContentService.Domain.SettingDomain.Repositories;

public interface ICustomerVpSettingRepository : IReadOnlyGenericRepository<CustomerVpSetting, Guid>
{
}