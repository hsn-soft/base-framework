using System;
using System.Text;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HsnSoft.Base.Authentication;

public static class JwtServiceCollectionExtensions
{
    /// <summary>
    /// JWT bearer validation (issuer, audience, signature, lifetime) and the <see cref="BasePolicies.Authenticated"/> policy, and <see cref="ICurrentUser"/> (the user of the request).
    /// Claims keep their token names: the user name is <c>sub</c>, roles are <c>role</c>. Add your own policies with
    /// <c>services.AddAuthorizationBuilder()</c>. Tokens are checked with the shared <c>Jwt:Key</c> (HMAC) or, when <c>Jwt:PublicKeyFile</c> is given, with that RSA public key (RS256 only):
    /// a section that is not usable (an empty issuer or audience, an HMAC key shorter than 32 bytes, a missing, unreadable or too small (under 2048 bits) RSA key file) stops the application when it starts, with the reasons.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the <c>Jwt</c> section (<see cref="JwtOptions"/>).</param>
    public static IServiceCollection AddBaseJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        // who is making the request, for the code that does not know HTTP
        services.AddHttpContextAccessor();
        services.TryAddSingleton<ICurrentUser, HttpContextCurrentUser>();

        // a section that is wrong (no issuer or audience, a key that is too short, a key file that is not there or is too weak) is found now, not at the first request, or never
        JwtSettingsCheck.Ensure(configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions());

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                JwtOptions settings = jwt.Value;
                bool asymmetric = !string.IsNullOrWhiteSpace(settings.PublicKeyFile);
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = asymmetric
                        ? new RsaSecurityKey(RsaKeys.LoadPublic(settings.PublicKeyFile!))
                        : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                    // only RS256 with a public key: a token that says it is signed another way (HMAC with the public key as the secret) is not accepted
                    ValidAlgorithms = asymmetric ? [SecurityAlgorithms.RsaSha256] : null,
                    NameClaimType = BaseClaimTypes.UserId,
                    RoleClaimType = BaseClaimTypes.Role,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(BasePolicies.Authenticated, policy => policy.RequireAuthenticatedUser());

        return services;
    }

    /// <summary>Reads the bearer token and builds the user. Put it before <see cref="UseBaseAuthorization"/>.</summary>
    public static IApplicationBuilder UseBaseAuthentication(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseAuthentication();
    }

    /// <summary>Enforces the policies of the endpoints. Put it after <see cref="UseBaseAuthentication"/>.</summary>
    public static IApplicationBuilder UseBaseAuthorization(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseAuthorization();
    }
}
