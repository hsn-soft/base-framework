using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;

namespace HsnSoft.Base.Core.Tests;

public class ActingUserTests
{
    private static UserContext User(string name) => new(Guid.NewGuid(), name, [], Guid.NewGuid(), false);

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

    [Fact]
    public void TheCurrentTenant_FollowsTheCurrentUser_WhichAnActingUserAnswers()
    {
        Guid tenant = Guid.NewGuid();
        ActingCurrentUser user = new();
        CurrentTenant currentTenant = new(user);
        Assert.False(currentTenant.IsAvailable);

        using (ActingUser.Use(new UserContext(Guid.NewGuid(), "alice", [], tenant, false)))
        {
            Assert.Equal(tenant, currentTenant.Id);
            Assert.Equal([tenant], currentTenant.AllowedTenantIds);
        }

        Assert.False(currentTenant.IsAvailable);
    }

    private sealed class ActingCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => ActingUser.Current?.Id is not null;

        public Guid? Id => ActingUser.Current?.Id;

        public string? UserName => ActingUser.Current?.UserName;

        public Guid? TenantId => ActingUser.Current?.TenantId;

        public bool IsSystemTenant => ActingUser.Current?.IsSystemTenant ?? false;

        public IReadOnlyList<Guid> AllowedTenantIds => [];
    }

    [Fact]
    public void ACurrentUser_HasNoRolesUnlessItSaysSo()
        => Assert.Empty(((ICurrentUser)new ActingCurrentUser()).Roles);
}
