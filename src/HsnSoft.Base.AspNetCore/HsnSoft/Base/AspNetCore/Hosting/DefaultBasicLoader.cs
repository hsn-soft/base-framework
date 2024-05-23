using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.AspNetCore.Hosting;

public sealed class DefaultBasicLoader : IBasicLoader
{
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
    }
}