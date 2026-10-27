using System;
using System.IO;
using System.Security.Cryptography;

namespace HsnSoft.Base.Authentication;

/// <summary>
/// Reads an RSA key from a file, for tokens that are signed with a private key and checked with the public one (the auth server holds the private key,
/// every other host only the public one). The file is either the XML of <c>RSA.ToXmlString</c> (<c>&lt;RSAKeyValue&gt;</c>) or a PEM (<c>-----BEGIN ...</c>).
/// </summary>
public static class RsaKeys
{
    /// <summary>The key of a file the validating hosts have: a public key (a file with a private key in it works too, but is not what a validating host should have).</summary>
    public static RSA LoadPublic(string path) => Load(path);

    /// <summary>The key of a file only the one that signs has: it must contain the private key.</summary>
    public static RSA LoadPrivate(string path)
    {
        RSA rsa = Load(path);
        try
        {
            // a public key can verify but not sign: say it now, not at the first token
            rsa.SignData([], HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            return rsa;
        }
        catch (CryptographicException exception)
        {
            rsa.Dispose();
            throw new InvalidOperationException($"The RSA key file '{path}' has no private key, so it cannot sign.", exception);
        }
    }

    private static RSA Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // a relative path is relative to where the application runs (the key file is copied next to the assemblies)
        string full = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
        if (!File.Exists(full))
        {
            throw new InvalidOperationException($"The RSA key file '{full}' was not found.");
        }

        string text = File.ReadAllText(full);
        RSA rsa = RSA.Create();
        try
        {
            if (text.Contains("-----BEGIN", StringComparison.Ordinal))
            {
                rsa.ImportFromPem(text);
            }
            else
            {
                rsa.FromXmlString(text);
            }

            return rsa;
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException or FormatException)
        {
            rsa.Dispose();
            throw new InvalidOperationException($"The file '{full}' is not an RSA key (XML <RSAKeyValue> or PEM).", exception);
        }
    }
}
