using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Caching;

public class DistributedCacheKeyNormalizer : IDistributedCacheKeyNormalizer, ITransientDependency
{
    protected ICurrentTenant CurrentTenant { get; }

    protected BaseDistributedCacheOptions DistributedCacheOptions { get; }

    public DistributedCacheKeyNormalizer(
        ICurrentTenant currentTenant,
        IOptions<BaseDistributedCacheOptions> distributedCacheOptions)
    {
        CurrentTenant = currentTenant;
        DistributedCacheOptions = distributedCacheOptions.Value;
    }

    public virtual string NormalizeKey(DistributedCacheKeyNormalizeArgs args)
    {
        var normalizedKey = $"c:{args.CacheName},k:{DistributedCacheOptions.KeyPrefix}{args.Key}";

        if (!args.IgnoreMultiTenancy && CurrentTenant.Id.HasValue)
        {
            normalizedKey = $"t:{CurrentTenant.Id.Value},{normalizedKey}";
        }

        return normalizedKey;
    }
}
