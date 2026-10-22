using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Sample.Api.Domain.Entities;
using HsnSoft.Base.Sample.Api.Domain.Repositories;
using HsnSoft.Base.Sample.Api.MongoDb.Context;

namespace HsnSoft.Base.Sample.Api.MongoDb.Repositories;

public sealed class MongoUserRepository(AppMongoDbContext context, IServiceProvider provider) : MongoGenericRepository<User, Guid>(provider, context), IMongoUserRepository;