using System.Threading.Tasks;

namespace HsnSoft.Base.Data;

public interface IDataSeedContributor
{
    Task SeedAsync(DataSeedContext context);
}
