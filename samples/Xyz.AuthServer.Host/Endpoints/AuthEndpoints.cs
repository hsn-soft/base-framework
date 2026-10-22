using System.Security.Claims;
using Xyz.AuthServer.Host.Services;
using Xyz.Shared.Hosting;

namespace Xyz.AuthServer.Host.Endpoints;

public sealed record LoginRequest(string Username, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn, string RefreshToken);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/logout", LogoutAsync);
        group.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            Subject = user.FindFirstValue("sub"),
            Roles = user.FindAll("role").Select(x => x.Value),
        })).RequireAuthorization(ServiceRegister.AuthenticatedPolicy);
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, TokenService tokens, CancellationToken cancellationToken)
    {
        var account = tokens.ValidateCredentials(request.Username, request.Password);
        if (account is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials");
        }

        return Results.Ok(ToResponse(await tokens.IssueAsync(account, cancellationToken)));
    }

    private static async Task<IResult> RefreshAsync(RefreshRequest request, TokenService tokens, CancellationToken cancellationToken)
    {
        var pair = await tokens.RefreshAsync(request.RefreshToken, cancellationToken);
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
