using HsnSoft.Base.Application;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Core.Tests;

public class ApplicationServiceTests
{
    private sealed class Sample(IServiceProvider services) : ApplicationService(services)
    {
        public ILogger TheLogger => Logger;

        public ICurrentUser TheUser => CurrentUser;

        public ICurrentTenant TheTenant => CurrentTenant;

        public IServiceProvider TheServices => ServiceProvider;
    }

    private sealed class FixedUser : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public Guid? Id => null;

        public string? UserName => "alice";

        public Guid? TenantId => null;

        public bool IsSystemTenant => false;

        public IReadOnlyList<Guid> AllowedTenantIds => [];
    }

    [Fact]
    public void TheLoggerTheUserAndTheTenant_AreFoundFromTheServices_AndKept()
    {
        using ServiceProvider provider = new ServiceCollection().AddLogging().AddSingleton<ICurrentUser, FixedUser>().AddSingleton<ICurrentTenant, CurrentTenant>().BuildServiceProvider();
        Sample service = new(provider);

        Assert.Equal("alice", service.TheUser.UserName);
        Assert.Same(service.TheUser, service.TheUser);
        Assert.Same(service.TheLogger, service.TheLogger);
        Assert.Same(service.TheTenant, service.TheTenant);
        Assert.False(service.TheTenant.IsAvailable);
        Assert.Same(provider, service.TheServices);
    }

    [Fact]
    public void TheServicesAreRequired_AndWhatIsNotRegisteredIsAnErrorWhenAsked()
    {
        Assert.Throws<ArgumentNullException>(() => new Sample(null!));

        using ServiceProvider empty = new ServiceCollection().BuildServiceProvider();
        Sample service = new(empty);
        Assert.Throws<InvalidOperationException>(() => service.TheUser);
        Assert.Throws<InvalidOperationException>(() => service.TheTenant);
        Assert.Throws<InvalidOperationException>(() => service.TheLogger);
    }
}
