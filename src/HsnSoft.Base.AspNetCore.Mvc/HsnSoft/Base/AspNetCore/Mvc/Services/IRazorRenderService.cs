using System.Threading.Tasks;

namespace HsnSoft.Base.AspNetCore.Mvc.Services;

public interface IRazorRenderService
{
    Task<string> ToStringAsync<T>(string viewName, T model);
}