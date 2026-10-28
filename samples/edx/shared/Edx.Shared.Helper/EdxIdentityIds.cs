using System.Security.Cryptography;
using System.Text;

namespace Edx.Shared.Helper;

/// <summary>
/// The ids of the roles and of the users of the sample: the same for the same tenant and name, wherever and whenever they are made, so the seed adds what is missing without asking and a change of a name or an id is a visible change of the code. The id is a uuid of version 8 made from a hash (SHA-256) of the tenant and
/// the name: not a secret and not a promise for other systems.
/// </summary>
public static class EdxIdentityIds
{
    public static Guid RoleId(Guid tenantId, string roleName) => From($"role:{tenantId:N}:{roleName.Trim().ToLowerInvariant()}");

    public static Guid UserId(Guid tenantId, string userName) => From($"user:{tenantId:N}:{userName.Trim().ToLowerInvariant()}");

    private static Guid From(string text)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        byte[] bytes = hash[..16];

        // version 8 (a uuid made by the application) and the variant of RFC 9562
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes, bigEndian: true);
    }
}
