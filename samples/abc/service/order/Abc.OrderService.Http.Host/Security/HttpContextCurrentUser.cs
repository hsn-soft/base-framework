using Abc.OrderService.Application.Security;
using HsnSoft.Base.Security;

namespace Abc.OrderService.Http.Host.Security;

/// <summary>The current user is the one the token of the request names (<c>sub</c>).</summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserName => accessor.HttpContext?.User.GetUserId();
}
