using Abc.IdentityService.Domain.UserDomain.Consts;
using Abc.IdentityService.Domain.UserDomain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Abc.IdentityService.EntityFrameworkCore.Configurations;

public static class UserDomainConfiguration
{
    public static void ConfigureUserDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<User>(user =>
        {
            user.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + UserConsts.TableName, EfCoreIdentityDbProperties.DbSchema);
            user.HasKey(entity => entity.Id);
            user.Property(entity => entity.Id).ValueGeneratedNever();
            user.Property(entity => entity.UserName).HasMaxLength(UserConsts.UserNameMaxLength).IsRequired();
            user.Property(entity => entity.NormalizedUserName).HasMaxLength(UserConsts.UserNameMaxLength).IsRequired();
            user.Property(entity => entity.Email).HasMaxLength(UserConsts.EmailMaxLength).IsRequired();
            user.Property(entity => entity.NormalizedEmail).HasMaxLength(UserConsts.EmailMaxLength).IsRequired();
            user.Property(entity => entity.DisplayName).HasMaxLength(UserConsts.DisplayNameMaxLength).IsRequired();
            user.Property(entity => entity.Language).HasMaxLength(UserConsts.LanguageMaxLength).IsRequired();
            user.Property(entity => entity.PasswordHash).HasMaxLength(UserConsts.PasswordHashMaxLength).IsRequired();

            // the roles are one column of names with a comma between them (a role cannot have a comma, the user checks it)
            user.Property(entity => entity.Roles)
                .HasConversion(
                    roles => string.Join(',', roles),
                    text => text.Split(',', StringSplitOptions.RemoveEmptyEntries),
                    new ValueComparer<IReadOnlyList<string>>((left, right) => left!.SequenceEqual(right!), roles => string.Join(',', roles).GetHashCode(StringComparison.Ordinal), roles => roles.ToArray()))
                .HasMaxLength(256)
                .IsRequired();
            user.HasIndex(entity => entity.NormalizedUserName).IsUnique();
            user.HasIndex(entity => entity.TenantId);
        });
    }
}
