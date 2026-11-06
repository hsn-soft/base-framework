using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace HsnSoft.Base.Authentication;

/// <summary>
/// What a host with a wrong <c>Jwt</c> section would otherwise find out at the first request, or never (a short key that works): it is checked when the host starts, and a section that is wrong stops the host with all the reasons.
/// </summary>
internal static class JwtSettingsCheck
{
    /// <summary>The shortest HMAC key: 32 bytes, the size of the hash (SHA-256) it signs with; a shorter one is weaker than the signature that it makes.</summary>
    public const int MinimumKeyBytes = 32;

    /// <summary>The smallest RSA key (bits) that is accepted for a token.</summary>
    public const int MinimumRsaBits = 2048;

    public static void Ensure(JwtOptions settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        List<string> problems = [];
        if (string.IsNullOrWhiteSpace(settings.Issuer))
        {
            problems.Add("Jwt:Issuer is empty: no token would be accepted");
        }

        if (string.IsNullOrWhiteSpace(settings.Audience))
        {
            problems.Add("Jwt:Audience is empty: no token would be accepted");
        }

        if (settings.ClockSkewSeconds is < 0 or > 300)
        {
            problems.Add($"Jwt:ClockSkewSeconds is {settings.ClockSkewSeconds}: it must be between 0 and 300");
        }

        if (!string.IsNullOrWhiteSpace(settings.PublicKeyFile))
        {
            using RSA rsa = RsaKeys.LoadPublic(settings.PublicKeyFile);
            if (rsa.KeySize < MinimumRsaBits)
            {
                problems.Add($"the RSA key of Jwt:PublicKeyFile has {rsa.KeySize} bits: at least {MinimumRsaBits} are needed");
            }
        }
        else if (Encoding.UTF8.GetByteCount(settings.Key ?? string.Empty) < MinimumKeyBytes)
        {
            problems.Add($"Jwt:Key is {Encoding.UTF8.GetByteCount(settings.Key ?? string.Empty)} bytes long: at least {MinimumKeyBytes} are needed (or give Jwt:PublicKeyFile, the RSA public key of the auth server)");
        }

        if (settings.AccessTokenMinutes < 1)
        {
            problems.Add($"Jwt:AccessTokenMinutes is {settings.AccessTokenMinutes}: it must be at least 1");
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException("The Jwt section is not usable: " + string.Join("; ", problems) + ".");
        }
    }
}
