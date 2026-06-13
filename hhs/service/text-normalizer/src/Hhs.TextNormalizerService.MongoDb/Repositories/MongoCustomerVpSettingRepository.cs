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
}