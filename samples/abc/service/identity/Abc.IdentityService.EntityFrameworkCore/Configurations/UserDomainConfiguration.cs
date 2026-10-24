using Abc.IdentityService.Domain.UserDomain.Consts;
using Abc.IdentityService.Domain.UserDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Abc.IdentityService.EntityFrameworkCore.Configurations;

public static class UserDomainConfiguration
{
    public static void ConfigureUserDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<User>(user =>
        {
            user.ToTable(UserConsts.TableName);
            user.HasKey(entity => entity.Id);
            user.Property(entity => entity.Id).ValueGeneratedNever();
            user.Property(entity => entity.UserName).HasMaxLength(UserConsts.UserNameMaxLength).IsRequired();
            user.Property(entity => entity.NormalizedUserName).HasMaxLength(UserConsts.UserNameMaxLength).IsRequired();
            user.Property(entity => entity.Email).HasMaxLength(UserConsts.EmailMaxLength).IsRequired();
            user.Property(entity => entity.NormalizedEmail).HasMaxLength(UserConsts.EmailMaxLength).IsRequired();
            user.Property(entity => entity.DisplayName).HasMaxLength(UserConsts.DisplayNameMaxLength).IsRequired();
            user.HasIndex(entity => entity.NormalizedUserName).IsUnique();
        });
    }
}
