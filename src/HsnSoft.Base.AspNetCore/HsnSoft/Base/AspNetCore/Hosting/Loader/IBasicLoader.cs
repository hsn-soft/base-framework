using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.AspNetCore.Hosting.Loader;

public interface IBasicLoader
{
    Task LoadAsync(CancellationToken cancellationToken);
}