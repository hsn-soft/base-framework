using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Localization;

public sealed class CacheStringLocalizerFactory : IStringLocalizerFactory, IStringLocalizerFactoryWithMultiple
{
    private readonly IMemoryCache _cache;

    public CacheStringLocalizerFactory(IMemoryCache cache)
    {
        _cache = cache;
    }

    public IStringLocalizer Create(Type resourceType) => new CacheStringLocalizer(resourceType, _cache);
    public IStringLocalizer CreateMultiple(List<Type> resourceTypes) => new CacheStringLocalizer(resourceTypes, _cache);

    public IStringLocalizer Create(string baseName, string location) => new CacheStringLocalizer(typeof(DefaultResource), _cache);
}