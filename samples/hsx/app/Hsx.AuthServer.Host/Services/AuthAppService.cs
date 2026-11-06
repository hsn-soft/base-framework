using HsnSoft.Base;
using HsnSoft.Base.Security;
using Hsx.AuthServer.Host.Contracts.AuthDomain.Dtos;
using Hsx.AuthServer.Host.Contracts.AuthDomain.Dtos.Submits;
using Hsx.AuthServer.Host.Contracts.AuthDomain.Services;
using Hsx.AuthServer.Host.Security;
using Hsx.AuthServer.Host.Security.Clients;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Repositories;
using Microsoft.Extensions.Options;

namespace Hsx.AuthServer.Host.Services;

public sealed partial class AuthAppService(
    UserAuthenticator authenticator,
    TokenService tokens,
    ClientRegistry clients,
    IAuthRefreshTokenRepository refreshTokens,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContext,
    IOptions<AuthServerOptions> options,
    ILogger<AuthAppService> logger) : IAuthAppService
{
    private readonly AuthServerOptions _options = options.Value;

    private HttpContext Http => httpContext.HttpContext ?? throw new InvalidOperationException("There is no request: the auth server is called through its controller.");

    private TimeSpan Lifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    public async Task<TokenDto> LoginAsync(LoginDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // the client is asked first: an application that is not known or cannot prove itself reaches neither the users nor the audit
        ClientOptions client = Authenticate(input);
        if (!client.AllowUserSignIn)
        {
            throw GrantNotAllowed(client);
        }

        if (input.OfflineAccess && !client.AllowOfflineAccess)
        {
            throw new BaseHttpException(StatusCodes.Status400BadRequest, "This client may not ask for a refresh token").WithKey("offline_access_not_allowed");
        }

        List<string> audiences = AudiencesOf(client, input.Audiences);
        SignInOrigin origin = new(Http.Connection.RemoteIpAddress?.ToString(), Http.Request.Headers.UserAgent.ToString());
        AuthenticatedUser user = await authenticator.AuthenticateAsync(input.Tenant, input.Account, input.Password, origin, cancellationToken).ConfigureAwait(false)
                                 ?? throw new BaseHttpException(StatusCodes.Status401Unauthorized, "Invalid credentials").WithKey("invalid_credentials");

        if (input.OfflineAccess)
        {
            SetRefreshCookie(await refreshTokens.IssueAsync(user.Id, user.TenantId, Lifetime, cancellationToken).ConfigureAwait(false));
        }

        // a sign-in is written to the audit by the authenticator (who, from where, and why it failed), so it is not logged again
        return ToDto(tokens.Issue(user, audiences, client.AccessTokenMinutes), user);
    }

    public async Task<TokenDto> RefreshAsync(RefreshDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ClientOptions client = Authenticate(input);
        if (!client.AllowUserSignIn || !client.AllowOfflineAccess)
        {
            throw GrantNotAllowed(client);
        }

        List<string> audiences = AudiencesOf(client, input.Audiences);
        string? token = Http.Request.Cookies[_options.RefreshCookieName];
        (Guid UserId, string NewToken)? rotated = string.IsNullOrEmpty(token) ? null : await refreshTokens.RotateAsync(token, Lifetime, cancellationToken).ConfigureAwait(false);
        AuthenticatedUser? user = rotated is { } found ? await authenticator.FindForRefreshAsync(found.UserId, cancellationToken).ConfigureAwait(false) : null;
        if (rotated is null || user is null)
        {
            RemoveRefreshCookieWhenAnswered();
            throw new BaseHttpException(StatusCodes.Status401Unauthorized, "Invalid refresh token").WithKey("invalid_refresh_token");
        }

        SetRefreshCookie(rotated.Value.NewToken);
        return ToDto(tokens.Issue(user, audiences, client.AccessTokenMinutes), user);
    }

    public Task<TokenDto> ClientTokenAsync(ClientTokenRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        ClientOptions client = Authenticate(input);
        if (!client.AllowClientCredentials)
        {
            throw GrantNotAllowed(client);
        }

        AccessToken token = tokens.IssueForClient(client.ClientId, AudiencesOf(client, input.Audiences), client.AccessTokenMinutes);
        return Task.FromResult(new TokenDto { AccessToken = token.Value, ExpiresIn = token.ExpiresInSeconds });
    }

    public async Task<LogoutResultDto> LogoutAsync(CancellationToken cancellationToken = default)
    {
        string? token = Http.Request.Cookies[_options.RefreshCookieName];
        bool hadToken = !string.IsNullOrEmpty(token);
        if (hadToken)
        {
            await refreshTokens.RevokeAsync(token!, cancellationToken).ConfigureAwait(false);
        }

        Http.Response.Cookies.Delete(_options.RefreshCookieName, CookieOptions());
        return new LogoutResultDto { RefreshTokenRevoked = hadToken };
    }

    public Task<MeDto> GetMeAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new MeDto { UserId = currentUser.Id, UserName = currentUser.UserName, Roles = [.. currentUser.Roles] });

    [LoggerMessage(Level = LogLevel.Warning, Message = "A client was refused: {ClientId} ({Reason})")]
    private static partial void LogClientRefused(ILogger logger, string clientId, string reason);

    private static BaseHttpException GrantNotAllowed(ClientOptions client)
        => new BaseHttpException(StatusCodes.Status403Forbidden, $"The client '{client.ClientId}' may not ask for this.").WithKey("grant_not_allowed");

    // the client the request names, when it is known, enabled and proves itself; the answer for an id that is not known and for a wrong secret is the same
    private ClientOptions Authenticate(ClientRequestDto input)
    {
        ClientOptions? client = clients.Find(input.ClientId);
        if (!clients.Proves(client, input.ClientSecret) || client is null)
        {
            LogClientRefused(logger, Cut(input.ClientId), client is null ? "unknown" : "secret");
            throw new BaseHttpException(StatusCodes.Status401Unauthorized, "Invalid client").WithKey("invalid_client");
        }

        return client;
    }

    // what is typed is cut before it is written to a log
    private static string Cut(string text) => text.Length <= 100 ? text : text[..100];

    // the audiences asked for, when the client has them all; the ones of the client when none is asked for
    private static List<string> AudiencesOf(ClientOptions client, List<string>? asked)
    {
        if (asked is not { Count: > 0 })
        {
            return client.Audiences;
        }

        if (asked.FirstOrDefault(audience => !client.Audiences.Contains(audience)) is { } notAllowed)
        {
            throw new BaseHttpException(StatusCodes.Status400BadRequest, $"The client may not ask for the audience '{notAllowed}'.").WithKey("audience_not_allowed", notAllowed);
        }

        return [.. asked.Distinct()];
    }

    private static TokenDto ToDto(AccessToken token, AuthenticatedUser user)
        => new() { AccessToken = token.Value, ExpiresIn = token.ExpiresInSeconds, PasswordChangeRequired = user.PasswordChangeRequired };

    // an error answer is written from scratch (the response is cleared, the headers set so far are lost), so the cookie of a refresh token that is not valid any more is removed when the answer starts
    private void RemoveRefreshCookieWhenAnswered()
        => Http.Response.OnStarting(
            static state =>
            {
                (HttpResponse response, string name, CookieOptions options) = ((HttpResponse, string, CookieOptions))state;
                response.Cookies.Delete(name, options);
                return Task.CompletedTask;
            },
            (Http.Response, _options.RefreshCookieName, CookieOptions()));

    private CookieOptions CookieOptions() => new() { HttpOnly = true, Secure = Http.Request.IsHttps, SameSite = SameSiteMode.Strict, Path = _options.RefreshCookiePath };

    private void SetRefreshCookie(string token)
    {
        CookieOptions cookie = CookieOptions();
        cookie.Expires = DateTimeOffset.UtcNow.Add(Lifetime);
        Http.Response.Cookies.Append(_options.RefreshCookieName, token, cookie);
    }
}
