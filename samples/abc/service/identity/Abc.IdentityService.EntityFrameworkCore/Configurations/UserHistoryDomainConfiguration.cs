using Abc.IdentityService.Domain.UserHistoryDomain.Consts;
using Abc.IdentityService.Domain.UserHistoryDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Abc.IdentityService.EntityFrameworkCore.Configurations;

public static class UserHistoryDomainConfiguration
{
    public static void ConfigureUserHistoryDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<UserHistory>(history =>
        {
            history.ToTable(EfCoreHistoryDbProperties.DbTablePrefix + UserHistoryConsts.TableName, EfCoreHistoryDbProperties.DbSchema);
            history.HasKey(entity => entity.Id);
            history.Property(entity => entity.Id).ValueGeneratedNever();
            history.Property(entity => entity.UserName).HasMaxLength(UserHistoryConsts.UserNameMaxLength).IsRequired();
            history.Property(entity => entity.Action).HasMaxLength(UserHistoryConsts.ActionMaxLength).IsRequired();
            history.HasIndex(entity => entity.UserId);
            history.HasIndex(entity => entity.TenantId);
        });
    }
}
