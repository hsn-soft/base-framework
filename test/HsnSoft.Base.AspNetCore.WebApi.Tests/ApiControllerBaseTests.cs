using System.Net;
using System.Text.Json;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.AspNetCore.WebApi.Tests;

[ApiController]
[Route("api/base-sample")]
public sealed class BaseSampleController : ApiControllerBase
{
    [HttpGet("who")]
    public IActionResult Who() => Ok(new
    {
        user = CurrentUser.UserName,
        tenant = CurrentTenant.Id,
        sameUser = ReferenceEquals(CurrentUser, CurrentUser),
        sameTenant = ReferenceEquals(CurrentTenant, CurrentTenant),
        sameLogger = ReferenceEquals(Logger, Logger),
        loggerEnabled = Logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Critical),
    });
}

public sealed class ApiControllerBaseTests
{
    private sealed class FixedUser : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public Guid? Id => null;

        public string? UserName => "alice";

        public Guid? TenantId => new("11111111-1111-1111-1111-111111111111");

        public bool IsSystemTenant => false;

        public IReadOnlyList<Guid> AllowedTenantIds => [];
    }

    [Fact]
    public async Task ABaseController_HasTheLoggerTheUserAndTheTenant_FromTheServicesOfTheRequest()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Services.AddBaseWebApi().AddSingleton<ICurrentUser, FixedUser>().AddBaseEntityConcepts();
        builder.Services.AddControllers().AddApplicationPart(typeof(BaseSampleController).Assembly);
        await using WebApplication app = builder.Build();
        app.UseBaseExceptionHandling();
        app.MapControllers();
        await app.StartAsync();

        HttpResponseMessage response = await app.GetTestClient().GetAsync("/api/base-sample/who");
        JsonElement payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("payload");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("alice", payload.GetProperty("user").GetString());
        Assert.Equal("11111111-1111-1111-1111-111111111111", payload.GetProperty("tenant").GetString());
        Assert.True(payload.GetProperty("sameUser").GetBoolean());
        Assert.True(payload.GetProperty("sameTenant").GetBoolean());
        Assert.True(payload.GetProperty("sameLogger").GetBoolean());
        Assert.True(payload.GetProperty("loggerEnabled").GetBoolean());
        Assert.IsAssignableFrom<ICurrentTenant>(app.Services.GetRequiredService<ICurrentTenant>());
    }
}
