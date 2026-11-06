using Hsx.IdentityService.Domain.AppUserDomain.Consts;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class AppUserConfiguration
{
    public static void ConfigureAppUser(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable(EfCoreDbProperties.DbTablePrefix + AppUserConsts.TableName, EfCoreDbProperties.DbSchema, table =>
            {
                table.HasCheckConstraint("CK_AppUser_NormalizedUserName_NotEmpty", "\"NormalizedUserName\" <> ''");
                table.HasCheckConstraint("CK_AppUser_NormalizedEmail_NotEmpty", "\"NormalizedEmail\" <> ''");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.UserName).HasMaxLength(AppUserConsts.UserNameMaxLength).IsRequired();
            entity.Property(e => e.NormalizedUserName).HasMaxLength(AppUserConsts.UserNameMaxLength).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(AppUserConsts.EmailMaxLength).IsRequired();
            entity.Property(e => e.NormalizedEmail).HasMaxLength(AppUserConsts.EmailMaxLength).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(AppUserConsts.DisplayNameMaxLength).IsRequired();
            entity.Property(e => e.LanguageCode).HasMaxLength(AppUserConsts.LanguageCodeMaxLength);
            entity.Property(e => e.PhoneNumber).HasMaxLength(AppUserConsts.PhoneNumberMaxLength);
            entity.Property(e => e.AvatarSuffixUrl).HasMaxLength(AppUserConsts.AvatarSuffixUrlMaxLength);
            entity.Property(e => e.TemporaryPasswordExpiresAt);
            entity.Property(e => e.PasswordHash).HasMaxLength(AppUserConsts.PasswordHashMaxLength).IsRequired();
            // the email and the user name are unique in the tenant (a deleted user keeps both): a sign-in names the tenant
            entity.HasIndex(e => new { e.TenantId, e.NormalizedEmail }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.NormalizedUserName }).IsUnique();
            entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
