using HsnSoft.Base;
using HsnSoft.Base.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.Shared.Hosting.Workers;

public sealed class AppBasicLoader : IBasicLoader
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public AppBasicLoader(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);

        using var scope = _serviceScopeFactory.CreateScope();

        // LOAD SOME SETTINGS

        // INITIALIZE DATA
        var seeder = scope.ServiceProvider.GetRequiredService<IBasicDataSeeder>();
        await seeder.EnsureSeedDataAsync(cancellationToken);
    }
}