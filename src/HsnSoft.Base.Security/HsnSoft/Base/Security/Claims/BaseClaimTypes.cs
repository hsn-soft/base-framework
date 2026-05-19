using System.Security.Claims;

namespace HsnSoft.Base.Security.Claims;

public static class BaseClaimTypes
{
    public static string UserName { get; set; } = ClaimTypes.Name;

    public static string Name { get; set; } = ClaimTypes.GivenName;

    public static string SurName { get; set; } = ClaimTypes.Surname;

    public static string UserId { get; set; } = ClaimTypes.NameIdentifier;

    public static string Role { get; set; } = ClaimTypes.Role;

    public static string Email { get; set; } = ClaimTypes.Email;

    public static string EmailVerified { get; set; } = "email_verified";

    public static string PhoneNumber { get; set; } = "phone_number";

    public static string PhoneNumberVerified { get; set; } = "phone_number_verified";
    public static string SecurityStamp { get; set; } = "security_stamp";

    public static string TenantId { get; set; } = "tenant_id";
    public static string TenantNormalized { get; set; } = "tenant_normalized";
    public static string IsSystemTenant { get; set; } = "is_system_tenant";
    public static string AllowedTenantId { get; set; } = "allowed_tenant_id";

    public static string ClientId { get; set; } = "client_id";
}