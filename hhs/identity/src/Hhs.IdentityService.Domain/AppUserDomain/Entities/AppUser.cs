using System.Globalization;
using System.Text;
using Hhs.IdentityService.Domain.AppUserDomain.Consts;
using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;

namespace Hhs.IdentityService.Domain.AppUserDomain.Entities;

public sealed class AppUser : IdentityUser<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; internal set; }

    public Guid TenantId { get; private set; }

    [NotNull]
    public string TenantDomain { get; private set; }

    public string Name { get; private set; }

    public string Surname { get; private set; }

    public string DefaultLanguage { get; internal set; }

    public string AvatarSuffixUrl { get; internal set; }

    // public user => IsSystemUser : false, IsTenantUser: false
    public bool IsSystemUser => TenantId == default && (TenantDomain ?? string.Empty).Equals(DefaultDomainNames.System);
    public bool IsTenantUser => TenantId != default && !IsSystemUser;

    private AppUser()
    {
        TenantDomain = string.Empty;
        UserName = string.Empty;
        Email = string.Empty;
    }

    internal AppUser(Guid id, Guid tenantId, string tenantDomain,
        string userName, string email, string phone,
        string name,
        string surname,
        string defaultLanguage,
        string avatarSuffixUrl
    ) : this()
    {
        Id = id;
        SetTenant(tenantId, tenantDomain);
        SetDefaultLanguage(defaultLanguage);

        SetUserName(userName);
        SetEmail(email);
        SetPhone(phone);
        SetName(name);
        SetSurname(surname);
        AvatarSuffixUrl = avatarSuffixUrl;
    }

    internal void SetTenant(Guid tenantId, string tenantDomain)
    {
        TenantId = tenantId;
        TenantDomain = LocalizedModelValidator.NotNullOrWhiteSpace(tenantDomain, $"{nameof(TenantDomain)}", AppUserConsts.TenantDomainMaxLength);
    }

    internal void SetDefaultLanguage(string defaultLanguage)
    {
        DefaultLanguage = LocalizedModelValidator.Length(defaultLanguage, $"{nameof(AppUser)}:{nameof(DefaultLanguage)}", AppUserConsts.DefaultLanguageMaxLength);

        string[] acceptableLanguages = new[] { "en", "ru", "tr" };
        if (!acceptableLanguages.Contains((DefaultLanguage ?? "").ToLower(new CultureInfo("en-US"))))
        {
            DefaultLanguage = "en";
        }
    }

    internal void SetUserName(string username)
    {
        string checkUserName = LocalizedModelValidator.NotNullOrWhiteSpace(username, $"{nameof(AppUser)}:{nameof(UserName)}", AppUserConsts.UserNameMaxLength);

        UserName = string.Join("", checkUserName.ToLower(new CultureInfo("en-US")).Normalize(NormalizationForm.FormD)
            .Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));

        NormalizedUserName = string.Join("", UserName.ToUpper().Normalize(NormalizationForm.FormD)
            .Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
    }

    internal void SetEmail(string email)
    {
        string checkEmail = LocalizedModelValidator.NotNullOrWhiteSpace(email, $"{nameof(AppUser)}:{nameof(Email)}", AppUserConsts.EmailMaxLength);

        Email = string.Join("", checkEmail.ToLower(new CultureInfo("en-US")).Normalize(NormalizationForm.FormD)
            .Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));

        NormalizedEmail = string.Join("", Email.ToUpper().Normalize(NormalizationForm.FormD)
            .Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
    }

    internal void SetPhone(string phone)
    {
        PhoneNumber = LocalizedModelValidator.Length(phone, $"{nameof(AppUser)}:{nameof(PhoneNumber)}", AppUserConsts.PhoneNumberMaxLength);
    }

    internal void SetName(string name)
    {
        Name = LocalizedModelValidator.Length(name, $"{nameof(AppUser)}:{nameof(Name)}", AppUserConsts.NameMaxLength);
    }

    internal void SetSurname(string surname)
    {
        Surname = LocalizedModelValidator.Length(surname, $"{nameof(AppUser)}:{nameof(Surname)}", AppUserConsts.SurnameMaxLength);
    }
}