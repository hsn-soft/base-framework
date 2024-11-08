using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base;

public interface IBasicLoader
{
    Task LoadAsync(CancellationToken cancellationToken);
}