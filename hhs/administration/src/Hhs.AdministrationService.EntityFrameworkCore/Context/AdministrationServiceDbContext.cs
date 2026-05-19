using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using Hhs.AdministrationService.EntityFrameworkCore.Configurations;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hhs.AdministrationService.EntityFrameworkCore.Context;

public sealed class AdministrationServiceDbContext : BaseEfCoreDbContext<AdministrationServiceDbContext>
{
    public DbSet<PermissionGrant> PermissionGrants { get; set; }

    public AdministrationServiceDbContext(IServiceProvider provider, DbContextOptions<AdministrationServiceDbContext> options) : base(options, provider)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigurePermissionGrantEntity();
    }
}