using HsnSoft.Base.Application;
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

        public IServiceProvider TheServices => ServiceProvider;
    }

    private sealed class FixedUser : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public Guid? Id => null;

        public string? UserName => "alice";
    }

    [Fact]
    public void TheLoggerAndTheUser_AreFoundFromTheServices_AndKept()
    {
        using ServiceProvider provider = new ServiceCollection().AddLogging().AddSingleton<ICurrentUser, FixedUser>().BuildServiceProvider();
        Sample service = new(provider);

        Assert.Equal("alice", service.TheUser.UserName);
        Assert.Same(service.TheUser, service.TheUser);
        Assert.Same(service.TheLogger, service.TheLogger);
        Assert.Same(provider, service.TheServices);
    }

    [Fact]
    public void TheServicesAreRequired_AndWhatIsNotRegisteredIsAnErrorWhenAsked()
    {
        Assert.Throws<ArgumentNullException>(() => new Sample(null!));

        using ServiceProvider empty = new ServiceCollection().BuildServiceProvider();
        Sample service = new(empty);
        Assert.Throws<InvalidOperationException>(() => service.TheUser);
        Assert.Throws<InvalidOperationException>(() => service.TheLogger);
    }
}
