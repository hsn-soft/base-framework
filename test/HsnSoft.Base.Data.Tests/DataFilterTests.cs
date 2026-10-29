using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Data.Tests;

public class DataFilterTests
{
    private interface IOtherFilter;

    [Fact]
    public void FiltersAreOn_UntilAScopeTurnsThemOff_AndTheScopeRestoresThem()
    {
        DataFilter filter = new();
        Assert.True(filter.IsEnabled<ISoftDelete>());

        using (filter.Disable<ISoftDelete>())
        {
            Assert.False(filter.IsEnabled<ISoftDelete>());
            Assert.True(filter.IsEnabled<IOtherFilter>());

            using (filter.Enable<ISoftDelete>())
            {
                Assert.True(filter.IsEnabled<ISoftDelete>());
            }

            Assert.False(filter.IsEnabled<ISoftDelete>());
        }

        Assert.True(filter.IsEnabled<ISoftDelete>());
    }

    [Fact]
    public async Task AFilterSwitch_FollowsTheFlowOfTheCode_NotAnotherFlow()
    {
        DataFilter filter = new();
        using IDisposable scope = filter.Disable<ISoftDelete>();

        bool inside = await Task.Run(async () =>
        {
            await Task.Yield();
            return filter.IsEnabled<ISoftDelete>();
        });
        bool otherFlow = await Task.Run(() => { using IDisposable own = filter.Enable<ISoftDelete>(); return filter.IsEnabled<ISoftDelete>(); });

        Assert.False(inside);
        Assert.True(otherFlow);
        Assert.False(filter.IsEnabled<ISoftDelete>());
    }

    [Fact]
    public void TheRegistration_AddsTheSwitch_AndKeepsWhatTheHostRegistered()
    {
        ServiceCollection services = new();
        DataFilter own = new();
        services.AddSingleton<IDataFilter>(own);

        using ServiceProvider provider = services.AddBaseDataFilter().BuildServiceProvider();
        using ServiceProvider fresh = new ServiceCollection().AddBaseDataFilter().BuildServiceProvider();

        Assert.Same(own, provider.GetRequiredService<IDataFilter>());
        Assert.True(fresh.GetRequiredService<IDataFilter>().IsEnabled<ISoftDelete>());
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBaseDataFilter());
    }
}
