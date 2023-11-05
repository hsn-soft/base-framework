namespace HsnSoft.Base.Caching;

public interface IDistributedCacheKeyNormalizer
{
    string NormalizeKey(DistributedCacheKeyNormalizeArgs args);
}
