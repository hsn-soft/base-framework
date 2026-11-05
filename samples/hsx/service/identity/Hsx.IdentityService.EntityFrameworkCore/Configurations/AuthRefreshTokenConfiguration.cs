using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Consts;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class AuthRefreshTokenConfiguration
{
    public static void ConfigureAuthRefreshToken(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AuthRefreshToken>(entity =>
        {
            entity.ToTable(EfCoreDbProperties.DbTablePrefix + AuthRefreshTokenConsts.TableName, EfCoreDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TokenHash).HasMaxLength(AuthRefreshTokenConsts.TokenHashMaxLength).IsRequired();
            entity.Property(e => e.ReplacedByTokenHash).HasMaxLength(AuthRefreshTokenConsts.TokenHashMaxLength);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasOne<AppUser>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
