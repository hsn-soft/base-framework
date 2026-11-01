using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Authentication.Tests;

public sealed class JwtSettingsCheckTests : IDisposable
{
    private readonly List<string> _files = [];

    public void Dispose()
    {
        foreach (string file in _files)
        {
            File.Delete(file);
        }
    }

    private string PublicKeyOf(int bits)
    {
        using RSA rsa = RSA.Create(bits);
        string path = Path.Combine(Path.GetTempPath(), $"jwt-{Guid.NewGuid():N}.xml");
        File.WriteAllText(path, rsa.ToXmlString(includePrivateParameters: false));
        _files.Add(path);
        return path;
    }

    private static JwtOptions Valid() => new() { Issuer = "issuer", Audience = "audience", Key = new string('k', 32) };

    private static string Reasons(JwtOptions options) => Assert.Throws<InvalidOperationException>(() => JwtSettingsCheck.Ensure(options)).Message;

    [Fact]
    public void AUsableSection_Passes_WithAnHmacKeyOrAnRsaKey()
    {
        JwtSettingsCheck.Ensure(Valid());
        JwtSettingsCheck.Ensure(new JwtOptions { Issuer = "i", Audience = "a", PublicKeyFile = PublicKeyOf(2048) });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NoIssuerOrNoAudience_StopsTheHost_BecauseNoTokenWouldBeAccepted(string blank)
    {
        Assert.Contains("Jwt:Issuer", Reasons(new JwtOptions { Issuer = blank, Audience = "a", Key = new string('k', 32) }), StringComparison.Ordinal);
        Assert.Contains("Jwt:Audience", Reasons(new JwtOptions { Issuer = "i", Audience = blank, Key = new string('k', 32) }), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void AKeyShorterThan32Bytes_StopsTheHost(int length)
        => Assert.Contains($"{length} bytes", Reasons(new JwtOptions { Issuer = "i", Audience = "a", Key = new string('k', length) }), StringComparison.Ordinal);

    [Fact]
    public void TheLengthOfTheKey_IsInBytes_NotInCharacters()
    {
        // sixteen characters of two bytes each are 32 bytes
        JwtSettingsCheck.Ensure(new JwtOptions { Issuer = "i", Audience = "a", Key = new string('ö', 16) });
        Assert.Contains("30 bytes", Reasons(new JwtOptions { Issuer = "i", Audience = "a", Key = new string('ö', 15) }), StringComparison.Ordinal);
    }

    [Fact]
    public void AnRsaKeyUnder2048Bits_StopsTheHost()
        => Assert.Contains("1024 bits", Reasons(new JwtOptions { Issuer = "i", Audience = "a", PublicKeyFile = PublicKeyOf(1024) }), StringComparison.Ordinal);

    [Fact]
    public void AKeyFileThatIsNotThere_StopsTheHostWithItsOwnMessage()
        => Assert.Contains("was not found", Assert.Throws<InvalidOperationException>(() => JwtSettingsCheck.Ensure(new JwtOptions { Issuer = "i", Audience = "a", PublicKeyFile = "/nowhere/public.xml" })).Message, StringComparison.Ordinal);

    [Fact]
    public void ASectionWithSeveralProblems_SaysThemAll()
    {
        string message = Reasons(new JwtOptions { Issuer = string.Empty, Audience = string.Empty, Key = "short", AccessTokenMinutes = 0 });

        Assert.Contains("Jwt:Issuer", message, StringComparison.Ordinal);
        Assert.Contains("Jwt:Audience", message, StringComparison.Ordinal);
        Assert.Contains("Jwt:Key", message, StringComparison.Ordinal);
        Assert.Contains("Jwt:AccessTokenMinutes", message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRegistration_StopsTheHostWhenItStarts_NotAtTheFirstRequest()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Issuer"] = "i", ["Jwt:Audience"] = "a", ["Jwt:Key"] = "too short" }).Build();

        Assert.Contains("Jwt:Key", Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseJwtAuthentication(configuration)).Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() => JwtSettingsCheck.Ensure(null!));
    }
}
