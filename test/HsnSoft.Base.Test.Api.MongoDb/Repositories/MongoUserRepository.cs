using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Test.Api.Domain.Entities;
using HsnSoft.Base.Test.Api.Domain.Repositories;
using HsnSoft.Base.Test.Api.MongoDb.Context;

namespace HsnSoft.Base.Test.Api.MongoDb.Repositories;

public sealed class MongoUserRepository(AppMongoDbContext context) : MongoGenericRepository<User, Guid>(context), IMongoUserRepository;