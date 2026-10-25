using Microsoft.AspNetCore.Http;

namespace HsnSoft.Base.Security;

/// <summary>The current user of the request being served: what the token of its <c>HttpContext</c> says.</summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId => accessor.HttpContext?.User.GetUserId();

    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
