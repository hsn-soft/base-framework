using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.AspNetCore.Hosting;

public interface IBasicLoader
{
    Task LoadAsync(CancellationToken cancellationToken);
}