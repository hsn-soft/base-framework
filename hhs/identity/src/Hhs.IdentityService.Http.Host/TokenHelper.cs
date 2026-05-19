using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Hhs.IdentityService;

public static class TokenHelper
{
    public static string CreateRawToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(bytes);
    }
}


//
// public interface ICurrentUser
// {
//     Guid? UserId { get; }
//     Guid? TenantId { get; }
//     bool IsSystemTenant { get; }
// }
//
// public sealed class CurrentUser : ICurrentUser
// {
//     private readonly IHttpContextAccessor _httpContextAccessor;
//
//     public CurrentUser(IHttpContextAccessor httpContextAccessor)
//     {
//         _httpContextAccessor = httpContextAccessor;
//     }
//
//     public Guid? UserId =>
//         Guid.TryParse(_httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
//             ? id
//             : null;
//
//     public Guid? TenantId =>
//         Guid.TryParse(_httpContextAccessor.HttpContext?.User.FindFirstValue("tenant_id"), out var id)
//             ? id
//             : null;
//
//     public bool IsSystemTenant =>
//         string.Equals(
//             _httpContextAccessor.HttpContext?.User.FindFirstValue("is_system_tenant"),
//             "true",
//             StringComparison.OrdinalIgnoreCase);
// }