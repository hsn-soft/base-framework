using System.Security.Claims;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.Test.Api;

public class FakeUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        string fakeUserId = Guid.NewGuid().ToString();
        const string fakeUserName = "TestUser";

        var claims = new List<Claim>
        {
            new(BaseClaimTypes.TenantId,Guid.Empty.ToString()),
            new(ClaimTypes.NameIdentifier, fakeUserId), new(ClaimTypes.Name, fakeUserName), new(ClaimTypes.Role, "Tester") // istersen rol ekle
        };

        var identity = new ClaimsIdentity(claims, "FakeAuth");
        context.User = new ClaimsPrincipal(identity);

        await next(context);
    }
}