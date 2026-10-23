using System.Security.Claims;
using HsnSoft.Base.Authentication;
using HsnSoft.Base.Security;
using Xyz.AuthServer.Host.Services;

namespace Xyz.AuthServer.Host.Endpoints;

public sealed record LoginRequest(string Username, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn, string RefreshToken);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/auth");

        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/logout", LogoutAsync);
        group.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            Subject = user.GetUserId(),
            Roles = user.GetRoles(),
        })).RequireAuthorization(BasePolicies.Authenticated);
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, TokenService tokens, CancellationToken cancellationToken)
    {
        UserAccount? account = tokens.ValidateCredentials(request.Username, request.Password);
        if (account is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials");
        }

        return Results.Ok(ToResponse(await tokens.IssueAsync(account, cancellationToken)));
    }

    private static async Task<IResult> RefreshAsync(RefreshRequest request, TokenService tokens, CancellationToken cancellationToken)
    {
        TokenPair? pair = await tokens.RefreshAsync(request.RefreshToken, cancellationToken);
        return pair is null
            ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid or expired refresh token")
            : Results.Ok(ToResponse(pair));
    }

    private static async Task<IResult> LogoutAsync(RefreshRequest request, TokenService tokens, CancellationToken cancellationToken)
    {
        await tokens.RevokeAsync(request.RefreshToken, cancellationToken);
        return Results.NoContent();
    }

    private static TokenResponse ToResponse(TokenPair pair) => new(pair.AccessToken, "Bearer", pair.ExpiresInSeconds, pair.RefreshToken);
}
