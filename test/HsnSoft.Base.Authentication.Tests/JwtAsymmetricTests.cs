using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HsnSoft.Base.Authentication.Tests;

/// <summary>Tokens signed with a private key (RS256) and checked with the public key: the hosts that check cannot make a token.</summary>
public sealed class JwtAsymmetricTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("jwt-rsa-").FullName;
    private readonly RSA _signer = RSA.Create(2048);
    private readonly string _publicFile;

    public JwtAsymmetricTests()
    {
        _publicFile = Path.Combine(_directory, "public_key.xml");
        File.WriteAllText(_publicFile, _signer.ToXmlString(includePrivateParameters: false));
    }

    public void Dispose()
    {
        _signer.Dispose();
        Directory.Delete(_directory, recursive: true);
    }

    private static ServiceProvider BuildProvider(string? publicKeyFile, string? key = null)
    {
        Dictionary<string, string?> settings = new() { ["Jwt:Issuer"] = "test-issuer", ["Jwt:Audience"] = "test-audience" };
        if (publicKeyFile is not null)
        {
            settings["Jwt:PublicKeyFile"] = publicKeyFile;
        }

        if (key is not null)
        {
            settings["Jwt:Key"] = key;
        }

        return new ServiceCollection()
            .AddLogging()
            .AddBaseJwtAuthentication(new ConfigurationBuilder().AddInMemoryCollection(settings).Build())
            .BuildServiceProvider();
    }

    private static string RsaToken(RSA signer, string issuer = "test-issuer", string audience = "test-audience")
        => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity([new Claim("sub", "alice"), new Claim("role", "admin")]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(signer), SecurityAlgorithms.RsaSha256),
        });

    private static Task<TokenValidationResult> ValidateAsync(ServiceProvider provider, string token)
        => new JsonWebTokenHandler().ValidateTokenAsync(
            token,
            provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters);

    [Fact]
    public async Task ATokenSignedWithThePrivateKey_IsValidWithThePublicKeyFile()
    {
        using ServiceProvider provider = BuildProvider(_publicFile);

        TokenValidationResult result = await ValidateAsync(provider, RsaToken(_signer));

        Assert.True(result.IsValid);
        Assert.Equal("alice", result.ClaimsIdentity.Name);
        Assert.True(new ClaimsPrincipal(result.ClaimsIdentity).IsInRole("admin"));
    }

    [Fact]
    public async Task ATokenSignedWithAnotherPrivateKey_IsRejected()
    {
        using RSA stranger = RSA.Create(2048);
        using ServiceProvider provider = BuildProvider(_publicFile);

        Assert.False((await ValidateAsync(provider, RsaToken(stranger))).IsValid);
    }

    [Theory]
    [InlineData("someone-else", "test-audience")]
    [InlineData("test-issuer", "another-audience")]
    public async Task ATokenOfAnotherIssuerOrAudience_IsRejected(string issuer, string audience)
    {
        using ServiceProvider provider = BuildProvider(_publicFile);

        Assert.False((await ValidateAsync(provider, RsaToken(_signer, issuer, audience))).IsValid);
    }

    [Fact]
    public async Task APublicKey_CannotBeTurnedIntoAnHmacSecret()
    {
        // the classic attack: sign with HS256, using the bytes of the public key as the secret; the host must not accept the algorithm
        byte[] secret = Encoding.UTF8.GetBytes(File.ReadAllText(_publicFile));
        string forged = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Subject = new ClaimsIdentity([new Claim("sub", "mallory"), new Claim("role", "admin")]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256),
        });
        using ServiceProvider provider = BuildProvider(_publicFile);

        Assert.False((await ValidateAsync(provider, forged)).IsValid);
    }

    [Fact]
    public async Task WithAPublicKeyFile_TheSharedKeyIsNotUsed()
    {
        string hmac = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            Subject = new ClaimsIdentity([new Claim("sub", "alice")]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes("unit-test-signing-key-0123456789-abcdef")), SecurityAlgorithms.HmacSha256),
        });
        using ServiceProvider provider = BuildProvider(_publicFile, key: "unit-test-signing-key-0123456789-abcdef");

        Assert.False((await ValidateAsync(provider, hmac)).IsValid);
    }

    [Fact]
    public void APublicKeyFileThatIsNotThere_StopsTheApplicationAtStartup()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => BuildProvider(Path.Combine(_directory, "missing.xml")));

        Assert.Contains("was not found", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheKeyFilesOfTheOptions_AreNullByDefault()
    {
        JwtOptions options = new();

        Assert.Null(options.PublicKeyFile);
        Assert.Null(options.PrivateKeyFile);
    }
}
