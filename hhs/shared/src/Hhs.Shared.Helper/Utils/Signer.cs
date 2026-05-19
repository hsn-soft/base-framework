using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Hhs.Shared.Helper.Utils;

public class Signer
{
    private readonly HMAC hmac;

    public Signer(string key)
    {
        hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
    }

    public string Sign(string data)
    {
        string encryptedData = Cryptography.Encrypt(data);

        string timestampedData = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "." + encryptedData;

        byte[] signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(timestampedData));

        return WebUtility.UrlEncode(Convert.ToBase64String(signature) + "." + timestampedData);
    }

    public bool Verify(string tokenData, int maxAgeSeconds)
    {
        string[] parts = tokenData.Split('.');
        if (parts.Length != 3) return false;

        string encodedSignature = parts[0];
        long timestamp = long.Parse(parts[1]);
        string originalData = parts[2];

        if ((DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp) > maxAgeSeconds)
        {
            return false;
        }

        string timestampedData = timestamp + "." + originalData;
        byte[] signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(timestampedData));

        return encodedSignature == Convert.ToBase64String(signature);
    }
}

public class PasswordResetLinkGenerator
{
    private readonly Signer signer;
    private readonly string baseUrl;
    private readonly int maxAgeSeconds;
    private readonly PasswordResetParameters _passwordResetParameters;
    private readonly IConfiguration _configuration;
    private readonly string _parcelShopBaseUrl;

    public PasswordResetLinkGenerator(IConfiguration configuration, bool isNewUser = false)
    {
        _configuration = configuration;
        _passwordResetParameters = _configuration.GetSection("PasswordResetParameters").Get<PasswordResetParameters>();

        signer = new Signer(_passwordResetParameters.SecretKey);
        this.baseUrl = _passwordResetParameters.BaseClientUrl;
        this.maxAgeSeconds = isNewUser
            ? _passwordResetParameters.MaxAgeSecondsForNewUser
            : _passwordResetParameters.MaxAgeSeconds;
        _parcelShopBaseUrl = _passwordResetParameters.BaseParcelShopClientUrl;
    }

    public string GenerateResetLink(string username, string role)
    {
        string token = signer.Sign(username);
        if (role == "parcel-shop-user" || role == "parcel-shop-admin" || role == "parcel-shop-manager")
        {
            return $"{_parcelShopBaseUrl}/reset-password/{token}";
        }

        return $"{baseUrl}/forgot-password/{token}";
    }

    public bool VerifyResetLink(string signedData)
    {
        return signer.Verify(signedData, this.maxAgeSeconds);
    }
}

public class PasswordResetParameters
{
    public string BaseClientUrl { get; set; }
    public string BaseParcelShopClientUrl { get; set; }
    public string SecretKey { get; set; }
    public int MaxAgeSeconds { get; set; }
    public int MaxAgeSecondsForNewUser { get; set; }
}