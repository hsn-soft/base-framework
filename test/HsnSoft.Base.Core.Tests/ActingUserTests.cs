using HsnSoft.Base.Security;

namespace HsnSoft.Base.Core.Tests;

public class ActingUserTests
{
    private static UserContext User(string name) => new(Guid.NewGuid(), name, []);

    [Fact]
    public void NobodyActs_UntilAScopeIsOpen_AndTheBeforeComesBack()
    {
        UserContext alice = User("alice");
        UserContext bob = User("bob");
        Assert.Null(ActingUser.Current);

        using (ActingUser.Use(alice))
        {
            Assert.Same(alice, ActingUser.Current);
            using (ActingUser.Use(bob))
            {
                Assert.Same(bob, ActingUser.Current);
            }

            Assert.Same(alice, ActingUser.Current);
            using (ActingUser.Use(null))
            {
                Assert.Null(ActingUser.Current);
            }

            Assert.Same(alice, ActingUser.Current);
        }

        Assert.Null(ActingUser.Current);
    }

    [Fact]
    public async Task AScope_FollowsTheAsyncCalls_ButIsNotSeenByAnotherFlow()
    {
        UserContext alice = User("alice");
        Task<UserContext?> other;
        using (ActingUser.Use(alice))
        {
            other = Task.Run(async () =>
            {
                await Task.Yield();
                return ActingUser.Current;
            });
            await Task.Delay(1);
            Assert.Same(alice, ActingUser.Current);
        }

        Assert.Same(alice, await other);
        Assert.Null(await Task.Run(() => ActingUser.Current));
    }

    private sealed class ActingCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => ActingUser.Current?.Id is not null;

        public Guid? Id => ActingUser.Current?.Id;

        public string? UserName => ActingUser.Current?.UserName;
    }

    [Fact]
    public void ACurrentUser_HasNoRolesUnlessItSaysSo()
        => Assert.Empty(((ICurrentUser)new ActingCurrentUser()).Roles);
}
