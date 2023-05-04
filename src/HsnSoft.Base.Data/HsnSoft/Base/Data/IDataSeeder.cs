using System.Threading.Tasks;

namespace HsnSoft.Base.Data;

public interface IDataSeeder
{
    Task SeedAsync(DataSeedContext context);
}
