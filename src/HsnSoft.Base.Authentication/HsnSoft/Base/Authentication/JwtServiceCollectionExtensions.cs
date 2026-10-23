using System;
using System.Text;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HsnSoft.Base.Authentication;

public static class JwtServiceCollectionExtensions
{
    /// <summary>
    /// JWT bearer validation (issuer, audience, signature, lifetime) and the <see cref="BasePolicies.Authenticated"/> policy.
    /// Claims keep their token names: the user name is <c>sub</c>, roles are <c>role</c>. Add your own policies with
    /// <c>services.AddAuthorizationBuilder()</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Holds the <c>Jwt</c> section (<see cref="JwtOptions"/>).</param>
    public static IServiceCollection AddBaseJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                JwtOptions settings = jwt.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
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
