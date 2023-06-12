using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Localization;

public sealed class CacheStringLocalizer : IStringLocalizer
{
    private readonly List<Type> _resourceTypes;
    private readonly IMemoryCache _cache;

    public CacheStringLocalizer(Type resourceType, IMemoryCache cache)
        : this(new List<Type> { resourceType ?? typeof(DefaultResource) }, cache)
    {
    }

    public CacheStringLocalizer(List<Type> resourceTypes, IMemoryCache cache)
    {
        _resourceTypes = resourceTypes ?? new List<Type> { typeof(DefaultResource) };
        _cache = cache;
    }

    public LocalizedString this[string name]
    {
        get
        {
            string value = GetLocalizationDictionary().GetOrNull(name);
            return new LocalizedString(name, value ?? name, value == null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var actualValue = this[name];
            return !actualValue.ResourceNotFound
                ? new LocalizedString(name, string.Format(actualValue.Value, arguments), false)
                : actualValue;
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return GetLocalizationDictionary().GetDictionary().Select(x => x.Value).ToList();
    }

    private ILocalizationDictionary GetLocalizationDictionary()
    {
        // string test = CultureInfo.CurrentUICulture.Name;
        var cultureName = Thread.CurrentThread.CurrentCulture.Name;

        //Try to get from same language dictionary (without country code)
        if (cultureName.Contains('-')) //Example: "tr-TR" (length=5)
        {
            cultureName = CultureHelper.GetBaseCultureName(cultureName);
        }

        var cacheKey = $"locale_{cultureName}_{_resourceTypes.First().Name}";

        // If found in cache, return cached data
        if (_cache.TryGetValue(cacheKey, out ServiceResourceDictionary localizationDictionary))
            return localizationDictionary;

        localizationDictionary = new ServiceResourceDictionary(cultureName);
        localizationDictionary.ImportJsonResourceToDictionary(typeof(DefaultResource));
        if (_resourceTypes is { Count: > 0 })
        {
            foreach (var resource in _resourceTypes)
            {
                localizationDictionary.ImportJsonResourceToDictionary(resource);
            }
        }

        // Set cache options
        var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(30));

        // Set object in cache
        _cache.Set(cacheKey, localizationDictionary, cacheOptions);

        return localizationDictionary;
    }
}