using HsnSoft.Base.Test.Api.Domain.Consts;
using HsnSoft.Base.Test.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Test.Api.EfCore.Configurations;

public static class UserConfiguration
{
    public static void ConfigureUserEntity(this ModelBuilder builder)
    {
        builder.Entity<User>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + UserConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.Email).HasColumnName(nameof(User.Email)).IsRequired().HasMaxLength(UserConsts.EmailMaxLength);

            // b.HasIndex(x => new { x.IsDeleted });
            b.HasIndex(m => m.Email).IsUnique();
        });
    }
}