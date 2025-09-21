using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.EntityFrameworkCore;

public abstract class BaseEfCoreDbContext<TDbContext>(DbContextOptions<TDbContext> options) : DbContext(options) where TDbContext : DbContext;