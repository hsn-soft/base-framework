using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.Clients.Middlewares;

public class HttpClientAuthorizationDelegatingHandler : DelegatingHandler
{
    const string ACCESS_TOKEN = "access_token";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpClientAuthorizationDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Request != null)
        {
            var authorizationHeader = _httpContextAccessor.HttpContext
                .Request.Headers["Authorization"];

            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                request.Headers.Add("Authorization", new List<string> { authorizationHeader });
            }

            var token = await GetToken();
            if (token == null)
            {
                token = GetTokenFromClaims();
            }
            if (token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetToken()
    {
        return await _httpContextAccessor.HttpContext.GetTokenAsync(ACCESS_TOKEN);
    }

    private string GetTokenFromClaims()
    {
        return _httpContextAccessor.HttpContext.User.FindFirst(x => x.Type == ACCESS_TOKEN)?.Value;
    }
}