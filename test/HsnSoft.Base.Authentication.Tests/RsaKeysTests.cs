using System.Security.Cryptography;

namespace HsnSoft.Base.Authentication.Tests;

public sealed class RsaKeysTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("rsa-keys-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private string Write(string name, string content)
    {
        string path = Path.Combine(_directory, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void ThePublicAndThePrivateKey_AreReadFromXml()
    {
        using RSA made = RSA.Create(2048);
        string publicFile = Write("public.xml", made.ToXmlString(includePrivateParameters: false));
        string privateFile = Write("private.xml", made.ToXmlString(includePrivateParameters: true));

        using RSA publicKey = RsaKeys.LoadPublic(publicFile);
        using RSA privateKey = RsaKeys.LoadPrivate(privateFile);

        byte[] signature = privateKey.SignData("hello"u8.ToArray(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        Assert.True(publicKey.VerifyData("hello"u8.ToArray(), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [Fact]
    public void APemIsReadToo()
    {
        using RSA made = RSA.Create(2048);
        string publicFile = Write("public.pem", made.ExportSubjectPublicKeyInfoPem());
        string privateFile = Write("private.pem", made.ExportPkcs8PrivateKeyPem());

        using RSA publicKey = RsaKeys.LoadPublic(publicFile);
        using RSA privateKey = RsaKeys.LoadPrivate(privateFile);

        byte[] signature = privateKey.SignData("hello"u8.ToArray(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        Assert.True(publicKey.VerifyData("hello"u8.ToArray(), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [Fact]
    public void ARelativePath_IsRelativeToWhereTheApplicationRuns()
    {
        using RSA made = RSA.Create(2048);
        string name = $"relative-{Guid.NewGuid():N}.xml";
        string inBase = Path.Combine(AppContext.BaseDirectory, name);
        File.WriteAllText(inBase, made.ToXmlString(includePrivateParameters: false));
        try
        {
            using RSA key = RsaKeys.LoadPublic(name);

            Assert.Equal(made.ExportParameters(false).Modulus, key.ExportParameters(false).Modulus);
        }
        finally
        {
            File.Delete(inBase);
        }
    }

    [Fact]
    public void AKeyFileThatIsNotThere_SaysWhereItLooked()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => RsaKeys.LoadPublic(Path.Combine(_directory, "missing.xml")));

        Assert.Contains("missing.xml", error.Message, StringComparison.Ordinal);
        Assert.Contains("was not found", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("this is not a key")]
    [InlineData("<RSAKeyValue><Modulus>AAAA</Modulus></RSAKeyValue>")]
    [InlineData("-----BEGIN PUBLIC KEY-----\nAAAA\n-----END PUBLIC KEY-----")]
    public void AFileThatIsNotAnRsaKey_IsRefusedWithItsName(string content)
    {
        string file = Write("bad.key", content);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => RsaKeys.LoadPublic(file));

        Assert.Contains("bad.key", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void APublicKey_CannotBeUsedToSign()
    {
        using RSA made = RSA.Create(2048);
        string publicFile = Write("public-only.xml", made.ToXmlString(includePrivateParameters: false));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => RsaKeys.LoadPrivate(publicFile));

        Assert.Contains("no private key", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ABlankPath_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => RsaKeys.LoadPublic(" "));
        Assert.Throws<ArgumentNullException>(() => RsaKeys.LoadPrivate(null!));
    }
}
