using System.Threading.Tasks;
using HsnSoft.Base.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Data;

//TODO: Create a HsnSoft.Base.Data.Seeding namespace?
public class DataSeeder : IDataSeeder, ITransientDependency
{
    public DataSeeder(
        IOptions<BaseDataSeedOptions> options,
        IServiceScopeFactory serviceScopeFactory)
    {
        ServiceScopeFactory = serviceScopeFactory;
        Options = options.Value;
    }

    protected IServiceScopeFactory ServiceScopeFactory { get; }
    protected BaseDataSeedOptions Options { get; }

    // [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            foreach (var contributorType in Options.Contributors)
            {
                var contributor = (IDataSeedContributor)scope
                    .ServiceProvider
                    .GetRequiredService(contributorType);

                await contributor.SeedAsync(context);
            }
        }
    }
}