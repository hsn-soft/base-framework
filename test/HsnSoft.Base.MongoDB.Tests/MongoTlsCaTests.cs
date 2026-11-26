using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB.Tests;

public class MongoTlsCaTests
{
    [Theory]
    [InlineData("mongodb://h:27017/db?tls=true&tlsCAFile=/etc/ca.crt", "/etc/ca.crt")]
    [InlineData("mongodb://h:27017/db?tlsCAFile=/etc/ca.crt&tls=true", "/etc/ca.crt")]
    [InlineData("mongodb://h:27017/db?TLSCAFILE=/etc/ca.crt", "/etc/ca.crt")]
    [InlineData("mongodb://h:27017/db?tlsCAFile=%2Fetc%2Fmy%20ca.crt", "/etc/my ca.crt")]
    [InlineData("mongodb://h:27017/db?tlsCAFile=", null)]
    [InlineData("mongodb://h:27017/db?tls=true", null)]
    [InlineData("mongodb://h:27017/db", null)]
    [InlineData("mongodb://h:27017/db?tlsCAFileX=/etc/ca.crt", null)]
    public void FileOf_ReadsTheOptionOfTheConnectionString(string connectionString, string? expected)
        => Assert.Equal(expected, MongoTlsCa.FileOf(connectionString));

    [Fact]
    public void FileOf_RejectsNull() => Assert.Throws<ArgumentNullException>(() => MongoTlsCa.FileOf(null!));

    [Fact]
    public void Load_ReadsEveryCertificateOfTheFile()
    {
        using TempFolder folder = new();
        using TestCa first = new("First");
        using TestCa second = new("Second");
        string file = folder.Write("ca.pem", first.PemText + second.PemText);

        X509Certificate2Collection roots = MongoTlsCa.Load(file);

        Assert.Equal(2, roots.Count);
    }

    [Fact]
    public void Load_MissingFile_IsAnErrorThatNamesTheFile()
    {
        using TempFolder folder = new();
        string file = Path.Combine(folder.Path, "missing.pem");

        FileNotFoundException error = Assert.Throws<FileNotFoundException>(() => MongoTlsCa.Load(file));

        Assert.Contains(file, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_FileWithoutACertificate_IsAnError()
    {
        using TempFolder folder = new();
        string file = folder.Write("ca.pem", "not a certificate");

        Assert.Throws<InvalidOperationException>(() => MongoTlsCa.Load(file));
    }

    [Fact]
    public void Load_BlankPath_IsRejected() => Assert.Throws<ArgumentException>(() => MongoTlsCa.Load(" "));

    [Fact]
    public void Validate_ACertificateOfTheCa_IsTrusted()
    {
        using TestCa ca = new();
        using X509Certificate2 server = ca.IssueServer();
        X509Certificate2Collection roots = [ca.PublicCertificate];

        Assert.True(MongoTlsCa.Validate(roots, server, null, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Fact]
    public void Validate_TheSystemSaysChainErrors_ButTheCaFileDecides()
    {
        // an unknown (private) CA is a chain error for the system; the CA file is what the option is for
        using TestCa ca = new();
        using X509Certificate2 server = ca.IssueServer();

        Assert.True(MongoTlsCa.Validate([ca.PublicCertificate], server, null, SslPolicyErrors.RemoteCertificateChainErrors));
        Assert.True(MongoTlsCa.Validate([ca.PublicCertificate], server, null, SslPolicyErrors.None));
    }

    [Fact]
    public void Validate_ACertificateOfAnotherCa_IsRefused()
    {
        using TestCa ca = new("Trusted");
        using TestCa other = new("Other");
        using X509Certificate2 server = other.IssueServer();

        Assert.False(MongoTlsCa.Validate([ca.PublicCertificate], server, null, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Fact]
    public void Validate_ANameThatDoesNotMatch_IsRefusedEvenForTheRightCa()
    {
        using TestCa ca = new();
        using X509Certificate2 server = ca.IssueServer();

        Assert.False(MongoTlsCa.Validate([ca.PublicCertificate], server, null, SslPolicyErrors.RemoteCertificateNameMismatch));
        Assert.False(MongoTlsCa.Validate([ca.PublicCertificate], server, null, SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch));
    }

    [Fact]
    public void Validate_NoCertificate_IsRefused()
    {
        using TestCa ca = new();

        Assert.False(MongoTlsCa.Validate([ca.PublicCertificate], null, null, SslPolicyErrors.None));
        Assert.False(MongoTlsCa.Validate([ca.PublicCertificate], null, null, SslPolicyErrors.RemoteCertificateNotAvailable));
    }

    [Fact]
    public void Validate_AnExpiredCertificate_IsRefused()
    {
        using TestCa ca = new();
        using X509Certificate2 expired = ca.IssueServer(notBefore: DateTimeOffset.UtcNow.AddDays(-5), notAfter: DateTimeOffset.UtcNow.AddDays(-1));

        Assert.False(MongoTlsCa.Validate([ca.PublicCertificate], expired, null, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Fact]
    public void Validate_TheCertificateOfTheCaItselfIsNotAServerCertificateOfAnotherCa()
    {
        using TestCa ca = new("Trusted");
        using TestCa other = new("Other");

        Assert.False(MongoTlsCa.Validate([ca.PublicCertificate], other.PublicCertificate, null, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Fact]
    public void Validate_RejectsNoRoots() => Assert.Throws<ArgumentNullException>(() => MongoTlsCa.Validate(null!, null, null, SslPolicyErrors.None));

    [Fact]
    public void Apply_TheCaFileOfTheConnectionString_InstallsTheCheckOfTheServerCertificate()
    {
        using TempFolder folder = new();
        using TestCa ca = new();
        string file = folder.Write("ca.pem", ca.PemText);
        using X509Certificate2 server = ca.IssueServer();

        MongoClientSettings settings = new MongoOptions().Apply($"mongodb://mongo:27017/orders?tls=true&tlsCAFile={Uri.EscapeDataString(file)}");

        Assert.NotNull(settings.SslSettings?.ServerCertificateValidationCallback);
        Assert.True(settings.SslSettings!.ServerCertificateValidationCallback(this, server, null, SslPolicyErrors.RemoteCertificateChainErrors));
        using TestCa other = new("Other");
        using X509Certificate2 foreign = other.IssueServer();
        Assert.False(settings.SslSettings.ServerCertificateValidationCallback(this, foreign, null, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Fact]
    public void Apply_WithoutTheOption_LeavesTheTlsSettingsToTheDriver()
        => Assert.Null(new MongoOptions().Apply("mongodb://mongo:27017/orders?tls=true").SslSettings?.ServerCertificateValidationCallback);

    [Fact]
    public void Apply_TlsOff_DoesNotReadTheFile()
        => Assert.Null(new MongoOptions().Apply("mongodb://mongo:27017/orders?tlsCAFile=/no/such/file.pem").SslSettings?.ServerCertificateValidationCallback);

    [Fact]
    public void Apply_TlsInsecure_WinsAndDoesNotReadTheFile()
    {
        MongoClientSettings settings = new MongoOptions().Apply("mongodb://mongo:27017/orders?tls=true&tlsInsecure=true&tlsCAFile=/no/such/file.pem");

        Assert.True(settings.AllowInsecureTls);
        Assert.Null(settings.SslSettings?.ServerCertificateValidationCallback);
    }

    [Fact]
    public void Apply_ACaFileThatIsNotThere_FailsAtOnce()
        => Assert.Throws<FileNotFoundException>(() => new MongoOptions().Apply("mongodb://mongo:27017/orders?tls=true&tlsCAFile=/no/such/file.pem"));
}
