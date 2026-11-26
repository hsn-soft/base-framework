using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace HsnSoft.Base.MongoDB;

/// <summary>
/// The <c>tlsCAFile</c> option of a MongoDB connection string. The .NET driver (3.x) reads the option but does not apply it, and ignores it without an error, so a server whose certificate is signed by a private CA (the CA of a cluster, not one of the system) is
/// refused with <c>UntrustedRoot</c> even though the file is given. <see cref="MongoOptions.Apply"/> reads the file and installs <see cref="Validate"/> as the certificate check of the connection: the certificate of the server must chain to a CA of the file
/// and its name must match the host (the system store is not used for it); <c>tlsInsecure</c> in the connection string still turns the whole check off, as it does for the driver.
/// </summary>
internal static class MongoTlsCa
{
    private const string Option = "tlsCAFile";

    /// <summary>The path in the <c>tlsCAFile</c> option of <paramref name="connectionString"/> (decoded), or null when the connection string does not have the option or its value is empty.</summary>
    internal static string? FileOf(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        int query = connectionString.IndexOf('?', StringComparison.Ordinal);
        if (query < 0)
        {
            return null;
        }

        foreach (string pair in connectionString[(query + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int equals = pair.IndexOf('=', StringComparison.Ordinal);
            if (equals > 0 && string.Equals(pair[..equals], Option, StringComparison.OrdinalIgnoreCase))
            {
                string value = Uri.UnescapeDataString(pair[(equals + 1)..]);
                return value.Length == 0 ? null : value;
            }
        }

        return null;
    }

    /// <summary>The certificates (PEM) of <paramref name="path"/>. A file that is not there, or has no certificate, is an error at the start of the service, not a refused connection later.</summary>
    internal static X509Certificate2Collection Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The tlsCAFile of the MongoDB connection string is not a file: '{path}'.", path);
        }

        X509Certificate2Collection roots = [];
        roots.ImportFromPemFile(path);
        if (roots.Count == 0)
        {
            throw new InvalidOperationException($"The tlsCAFile of the MongoDB connection string has no certificate (PEM): '{path}'.");
        }

        return roots;
    }

    /// <summary>
    /// Whether the certificate of the server is trusted: it has a valid chain that ends in one of <paramref name="roots"/>, and its name matches the host. A certificate that is missing, a name that does not match, an expired or revoked-by-date certificate
    /// or a chain of another CA is refused. Revocation is not checked (a private CA has no list to ask).
    /// </summary>
    internal static bool Validate(X509Certificate2Collection roots, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors)
    {
        ArgumentNullException.ThrowIfNull(roots);

        if (certificate is null || (errors & (SslPolicyErrors.RemoteCertificateNotAvailable | SslPolicyErrors.RemoteCertificateNameMismatch)) != 0)
        {
            return false;
        }

        using X509Certificate2 leaf = new(certificate);
        using X509Chain own = new();
        own.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        own.ChainPolicy.CustomTrustStore.AddRange(roots);
        own.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        if (chain is not null)
        {
            foreach (X509ChainElement element in chain.ChainElements)
            {
                own.ChainPolicy.ExtraStore.Add(element.Certificate);
            }
        }

        return own.Build(leaf);
    }
}
