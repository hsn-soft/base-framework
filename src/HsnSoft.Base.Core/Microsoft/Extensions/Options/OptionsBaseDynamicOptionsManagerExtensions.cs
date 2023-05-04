using System.Threading.Tasks;
using HsnSoft.Base;
using HsnSoft.Base.Options;

namespace Microsoft.Extensions.Options;

public static class OptionsBaseDynamicOptionsManagerExtensions
{
    public static Task SetAsync<T>(this IOptions<T> options)
        where T : class
    {
        return options.ToDynamicOptions().SetAsync();
    }

    public static Task SetAsync<T>(this IOptions<T> options, string name)
        where T : class
    {
        return options.ToDynamicOptions().SetAsync(name);
    }

    private static BaseDynamicOptionsManager<T> ToDynamicOptions<T>(this IOptions<T> options)
        where T : class
    {
        if (options is BaseDynamicOptionsManager<T> dynamicOptionsManager)
        {
            return dynamicOptionsManager;
        }

        throw new BaseException($"Options must be derived from the {typeof(BaseDynamicOptionsManager<>).FullName}!");
    }
}