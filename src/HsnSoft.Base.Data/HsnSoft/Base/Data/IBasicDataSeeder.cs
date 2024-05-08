using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.Data;

public interface IBasicDataSeeder
{
    Task EnsureSeedDataAsync(CancellationToken cancellationToken);
}