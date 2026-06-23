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
}