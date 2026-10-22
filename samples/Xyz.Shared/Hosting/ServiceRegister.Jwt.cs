using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xyz.Shared.Security;

namespace Xyz.Shared.Hosting;

public static partial class ServiceRegister
{
    public const string AdminPolicy = "admin";
    public const string AuthenticatedPolicy = "authenticated";

    /// <summary>JWT bearer validation plus the "authenticated" and "admin" policies. Settings: <c>Jwt</c> section.</summary>
    public static IServiceCollection AddSharedJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                var settings = jwt.Value;
                bearer.MapInboundClaims = false; // keep "sub" and "role" as they are in the token
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthenticatedPolicy, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(AdminPolicy, policy => policy.RequireRole(Roles.Admin));

        return services;
    }
}
