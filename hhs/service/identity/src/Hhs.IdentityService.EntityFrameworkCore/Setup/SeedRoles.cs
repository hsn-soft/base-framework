// using Hhs.Shared.Helper.Consts;
//
// namespace Hhs.IdentityService.EntityFrameworkCore.Setup;
//
// internal static class SeedRoles
// {
//     public static List<SeedRole> Roles => new()
//     {
//         new SeedRole { RoleId = Guid.Parse("E2F3F225-A555-49D9-8E8B-CDE113C18C4C"), Name = DefaultRoleNames.SystemAdmin, IsPublic = false, TenantDomain = DomainNames.System, TenantId = default },
//         new SeedRole { RoleId = Guid.Parse("8E19FFE7-5671-44A6-84B3-BD447A75FAEC"), Name = DefaultRoleNames.SystemUser, TenantDomain = DomainNames.System, TenantId = default },
//
//         new SeedRole { RoleId = Guid.Parse("ABCDEF6D-370F-4FDC-9BCA-0330FF0DFABC"), Name = DefaultRoleNames.AppUser, IsPublic = false, IsDefault = true, TenantDomain = DomainNames.Registered, TenantId = default },
//
//         new SeedRole { RoleId = Guid.Parse("19466B3F-3E8B-4034-84CC-EFCC572C2FFE"), Name = $"{NameConsts.Admin}#techsummus", TenantDomain = "techsummus", TenantId = Guid.Parse("97422b81-74da-4532-a230-9e4fb0c0dede") },
//         new SeedRole { RoleId = Guid.Parse("1CB1D2B5-928A-42F8-B005-78EE076B499C"), Name = $"{NameConsts.Admin}#dunya", TenantDomain = "dunya", TenantId = Guid.Parse("8c2420c0-3cef-43fc-a8bf-71e22afee1f5") },
//         new SeedRole { RoleId = Guid.Parse("29466B3F-3E8B-4034-84CC-EFCC572C2FFE"), Name = $"{NameConsts.Admin}#kisadalga", TenantDomain = "kisadalga", TenantId = Guid.Parse("54a50c2d-3ad0-41f2-99a2-3df95f508395") },
//         new SeedRole { RoleId = Guid.Parse("2CB1D2B5-928A-42F8-B005-78EE076B499C"), Name = $"{NameConsts.Admin}#tamindir", TenantDomain = "tamindir", TenantId = Guid.Parse("23389012-8c70-4d64-9d7f-7a0420b00c17") },
//         new SeedRole { RoleId = Guid.Parse("2D085C6D-370F-4FDC-9BCA-0330FF0DFA2E"), Name = $"{NameConsts.Admin}#technotoday", TenantDomain = "technotoday", TenantId = Guid.Parse("f25a1a33-8fd9-4652-98e1-c354a4201717") },
//         new SeedRole { RoleId = Guid.Parse("f6b8b776-9f17-4f73-ac2b-17d7a153e4fb"), Name = $"{NameConsts.Admin}#sondakika", TenantDomain = "sondakika", TenantId = Guid.Parse("59e368a4-7d4a-419b-9e74-2f517ed65af7") },
//         new SeedRole { RoleId = Guid.Parse("46f859a8-0737-452a-8fd6-258156fdf901"), Name = $"{NameConsts.Admin}#t24", TenantDomain = "t24", TenantId = Guid.Parse("f05ad9c0-52c7-4ad8-8349-1f4bcc1b9bf9") },
//         new SeedRole { RoleId = Guid.Parse("2901aeaf-b3cc-41c9-936e-897a5502879e"), Name = $"{NameConsts.Admin}#cnbce", TenantDomain = "cnbce", TenantId = Guid.Parse("fe3078f2-ab3a-49a4-96b1-8eb9069afe4a") },
//         new SeedRole { RoleId = Guid.Parse("d09e458b-9b42-4894-9bc2-349ad54cbf2a"), Name = $"{NameConsts.Admin}#boxofficeturkiye", TenantDomain = "boxofficeturkiye", TenantId = Guid.Parse("6f3727b8-019d-4de0-8c0a-00758e04ae1c") },
//     };
// }
//
// internal sealed class SeedRole
// {
//     public Guid RoleId { get; set; }
//     public string Name { get; set; }
//
//     public bool IsStatic { get; set; } = true; // can't delete
//     public bool IsPublic { get; set; } = true; // view commercial role list
//     public bool IsDefault { get; set; } = false; // register screen user
//
//     public Guid TenantId { get; set; } = default;
//     public string TenantDomain { get; set; } = null;
// }