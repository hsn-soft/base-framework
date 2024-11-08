using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.Data;

public sealed class DefaultBasicDataSeeder : IBasicDataSeeder
{
    public async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
    }
}