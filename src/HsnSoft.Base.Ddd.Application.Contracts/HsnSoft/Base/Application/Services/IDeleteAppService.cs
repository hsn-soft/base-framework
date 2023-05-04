using System.Threading.Tasks;

namespace HsnSoft.Base.Application.Services;

public interface IDeleteAppService<in TKey> : IApplicationService
{
    Task DeleteAsync(TKey id);
}