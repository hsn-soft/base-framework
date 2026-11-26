using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace HsnSoft.Base.MongoDB.Tests;

/// <summary>A private CA and the certificates it signs, made in memory for the tests of the CA file of the connection string.</summary>
internal sealed class TestCa : IDisposable
{
    private readonly X509Certificate2 _certificate;

    public TestCa(string name = "Test CA")
    {
        using RSA key = RSA.Create(2048);
        CertificateRequest request = new($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        using X509Certificate2 self = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(30));
        _certificate = X509CertificateLoader.LoadPkcs12(self.Export(X509ContentType.Pfx), null, X509KeyStorageFlags.Exportable);
    }

    /// <summary>The CA certificate as PEM text (with the closing line break, so texts of several CAs can follow each other).</summary>
    public string PemText => _certificate.ExportCertificatePem() + "\n";

    public X509Certificate2 PublicCertificate => X509CertificateLoader.LoadCertificate(_certificate.RawData);

    /// <summary>A server certificate for <paramref name="dnsName"/> (and 127.0.0.1) that this CA signed.</summary>
    public X509Certificate2 IssueServer(string dnsName = "localhost", DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null)
    {
        using RSA key = RSA.Create(2048);
        CertificateRequest request = new($"CN={dnsName}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        SubjectAlternativeNameBuilder names = new();
        names.AddDnsName(dnsName);
        names.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(names.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], false));
        byte[] serial = RandomNumberGenerator.GetBytes(8);
        serial[0] &= 0x7F;
        using X509Certificate2 signed = request.Create(_certificate, notBefore ?? DateTimeOffset.UtcNow.AddHours(-1), notAfter ?? DateTimeOffset.UtcNow.AddDays(10), serial);
        return X509CertificateLoader.LoadPkcs12(signed.CopyWithPrivateKey(key).Export(X509ContentType.Pfx), null, X509KeyStorageFlags.Exportable);
    }

    public void Dispose() => _certificate.Dispose();
}

/// <summary>A folder in the temp directory that is removed with the object.</summary>
internal sealed class TempFolder : IDisposable
{
    public TempFolder() => Path = Directory.CreateTempSubdirectory("mongo-ca-").FullName;

    public string Path { get; }

    public string Write(string name, string text)
    {
        string file = System.IO.Path.Combine(Path, name);
        File.WriteAllText(file, text);
        return file;
    }

    public void Dispose() => Directory.Delete(Path, true);
}
