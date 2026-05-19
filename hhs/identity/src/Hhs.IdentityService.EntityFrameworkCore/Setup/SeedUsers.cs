using Hhs.Shared.Helper.Consts;

namespace Hhs.IdentityService.EntityFrameworkCore.Setup;

internal static class SeedUsers
{
    private const string DefaultPlainPassword = "Passw0rd!";

    public static List<SeedUser> Users => new()
    {
        new SeedUser
        {
            UserId = Guid.Parse("4A670F80-5592-44D3-AFD5-C8DFCA3679E4"),
            TenantId = default,
            TenantDomain = DefaultDomainNames.System,
            Username = DefaultRoleNames.SystemAdmin,
            PlainPassword = DefaultPlainPassword,
            GivenName = DefaultDomainNames.System,
            FamilyName = IdentityConsts.Admin,
            Email = $"{IdentityConsts.Admin}@{IdentityConsts.SolutionName}.com#{DefaultDomainNames.System}",
            Roles = { DefaultRoleNames.SystemAdmin },
            AvatarUrl = "/demo-techsummus-48x48.png"
        },
        new SeedUser
        {
            UserId = Guid.Parse("BA271C49-4FCD-4143-8A17-5B6B7452AF66"),
            TenantId = default,
            TenantDomain = DefaultDomainNames.System,
            Username = DefaultRoleNames.SystemUser,
            PlainPassword = DefaultPlainPassword,
            GivenName = DefaultDomainNames.System,
            FamilyName = IdentityConsts.User,
            Email = $"{IdentityConsts.User}@{IdentityConsts.SolutionName}.com#{DefaultDomainNames.System}",
            Roles = { DefaultRoleNames.SystemUser },
            AvatarUrl = "/demo-techsummus-48x48.png"
        },
        new SeedUser
        {
            UserId = Guid.Parse("ABCD12BF-09B6-4DA9-AB62-D5F1DDD5FABC"),
            TenantId = default,
            TenantDomain = DefaultDomainNames.PublicApp,
            Username = "hsnsh",
            PlainPassword = DefaultPlainPassword,
            GivenName = "Hasan",
            FamilyName = "SAHIN",
            Email = "hsnsh@outlook.com",
            Phone = "905335551122",
            Roles = { DefaultRoleNames.AppUser },
            AvatarUrl = "/demo-techsummus-48x48.png"
        },
        new SeedUser
        {
            UserId = Guid.Parse("21DD12BF-09B6-4DA9-AB62-D5F1DDD5F48A"),
            TenantId = Guid.Parse("97422b81-74da-4532-a230-9e4fb0c0dede"),
            TenantDomain = "techsummus",
            Username = $"{IdentityConsts.Admin}#techsummus",
            PlainPassword = DefaultPlainPassword,
            GivenName = "TechSummus",
            FamilyName = "Admin",
            Email = $"{IdentityConsts.Admin}@techsummus.com#techsummus",
            Roles = { $"{IdentityConsts.Admin}#techsummus" },
            AvatarUrl = "/demo-techsummus-48x48.png"
        },
        new SeedUser
        {
            UserId = Guid.Parse("E1EA940A-76FD-40F6-AE45-68EC7680E1C2"),
            TenantId = Guid.Parse("8c2420c0-3cef-43fc-a8bf-71e22afee1f5"),
            TenantDomain = "dunya",
            Username = $"{IdentityConsts.Admin}#dunya",
            PlainPassword = DefaultPlainPassword,
            GivenName = "Dunya",
            FamilyName = "Admin",
            Email = $"{IdentityConsts.Admin}@dunya.com#dunya",
            Roles = { $"{IdentityConsts.Admin}#dunya" },
            AvatarUrl = "/dunya-video-logo-48x48.png"
        },
        new SeedUser
        {
            UserId = Guid.Parse("4E0E6A32-06EB-4B78-9625-69A20F950836"),
            TenantId = Guid.Parse("54a50c2d-3ad0-41f2-99a2-3df95f508395"),
            TenantDomain = "kisadalga",
            Username = $"{IdentityConsts.Admin}#kisadalga",
            PlainPassword = DefaultPlainPassword,
            GivenName = "KisaDalga",
            FamilyName = "Admin",
            Email = $"{IdentityConsts.Admin}@kisadalga.net#kisadalga",
            Roles = { $"{IdentityConsts.Admin}#kisadalga" },
            AvatarUrl = "/KisaDalgaLogoDark.png"
        },
        new SeedUser
        {
            UserId = Guid.Parse("31EA940A-76FD-40F6-AE45-68EC7680E1C2"),
            TenantId = Guid.Parse("23389012-8c70-4d64-9d7f-7a0420b00c17"),
            TenantDomain = "tamindir",
            Username = $"{IdentityConsts.Admin}#tamindir",
            PlainPassword = DefaultPlainPassword,
            GivenName = "TamIndir",
            FamilyName = "Admin",
            Email = $"{IdentityConsts.Admin}@tamindir.com#tamindir",
            Roles = { $"{IdentityConsts.Admin}#tamindir" },
            AvatarUrl = "/tamindir-video-logo-48x48.png"
        },
        new SeedUser
        {
            UserId = Guid.Parse("3E0E6A32-06EB-4B78-9625-69A20F950836"),
            TenantId = Guid.Parse("f25a1a33-8fd9-4652-98e1-c354a4201717"),
            TenantDomain = "technotoday",
            Username = $"{IdentityConsts.Admin}#technotoday",
            PlainPassword = DefaultPlainPassword,
            GivenName = "TechnoToday",
            FamilyName = "Admin",
            Email = $"{IdentityConsts.Admin}@technotoday.com.tr#technotoday",
            Roles = { $"{IdentityConsts.Admin}#technotoday" },
            AvatarUrl = "/technotoday-footer-logo-48x48.png"
        },
        new SeedUser
        {
            UserId = Guid.Parse("f6b8b776-9f17-4f73-ac2b-17d7a153e4fb"),
            TenantId = Guid.Parse("59e368a4-7d4a-419b-9e74-2f517ed65af7"),
            TenantDomain = "sondakika",
            Username = $"{IdentityConsts.Admin}#sondakika",
            PlainPassword = DefaultPlainPassword,
            GivenName = "SonDakika",
            FamilyName = "Admin",
            Email = $"{IdentityConsts.Admin}@sondakika.com#sondakika",
            Roles = { $"{IdentityConsts.Admin}#sondakika" },
            AvatarUrl = "/sondakika-video-logo-48x48.png"
        },
        new SeedUser
        {
            UserId = Guid.Parse("f05ad9c0-52c7-4ad8-8349-1f4bcc1b9bf9"),
            TenantId = Guid.Parse("46f859a8-0737-452a-8fd6-258156fdf901"),
            TenantDomain = "t24",
            Username = $"{IdentityConsts.Admin}#t24",
            PlainPassword = DefaultPlainPassword,
            GivenName = "T24",
            FamilyName = "Admin",
            Email = $"{IdentityConsts.Admin}@t24.com.tr#t24",
            Roles = { $"{IdentityConsts.Admin}#t24" },
            AvatarUrl = "/t24-video-logo-48x48.png"
        },
        new SeedUser
        {
            UserId = Guid.Parse("2901aeaf-b3cc-41c9-936e-897a5502879e"),
            TenantId = Guid.Parse("fe3078f2-ab3a-49a4-96b1-8eb9069afe4a"),
            TenantDomain = "cnbce",
            Username = $"{IdentityConsts.Admin}#cnbce",
            PlainPassword = DefaultPlainPassword,
            GivenName = "Cnbce",
            FamilyName = "Admin",
            Email = $"{IdentityConsts.Admin}@cnbce.com#cnbce",
            Roles = { $"{IdentityConsts.Admin}#cnbce" },
            AvatarUrl = "/cnbce-video-logo-48x48.png"
        },
        new SeedUser
        {
            UserId = Guid.Parse("d09e458b-9b42-4894-9bc2-349ad54cbf2a"),
            TenantId = Guid.Parse("6f3727b8-019d-4de0-8c0a-00758e04ae1c"),
            TenantDomain = "boxofficeturkiye",
            Username = $"{IdentityConsts.Admin}#boxofficeturkiye",
            PlainPassword = DefaultPlainPassword,
            GivenName = "BoxOfficeTurkiye",
            FamilyName = "Admin",
            Email = $"{IdentityConsts.Admin}@boxofficeturkiye.com#boxofficeturkiye",
            Roles = { $"{IdentityConsts.Admin}#boxofficeturkiye" },
            AvatarUrl = "/boxofficeturkiye-video-logo-48x48.png"
        }
    };
}

internal sealed class SeedUser
{
    public Guid UserId { get; set; }

    public string Username { get; set; }
    public string PlainPassword { get; set; }

    public Guid TenantId { get; set; } = default;
    public string TenantDomain { get; set; } = null;

    public string GivenName { get; set; }
    public string FamilyName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public List<string> Roles { get; set; } = new();

    public string Lang { get; set; }
    public string AvatarUrl { get; set; }
}