using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HsnSoft.Base.Localization.Json;
using HsnSoft.Base.VirtualFileSystem;
using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Localization;

public sealed class ServiceResourceDictionary : StaticLocalizationDictionary
{
    public ServiceResourceDictionary(string cultureName) : base(cultureName, new Dictionary<string, LocalizedString>())
    {
    }

    public void ImportJsonResourceToDictionary(Type resourceType)
    {
        // var resource = $"Localization.{GetLocalizationResourceName(typeof(T))}.{CultureName}.json";
        // var sut = new EmbeddedFileProviderQuery();

        var resource = $"{GetLocalizationResourceName(resourceType)}.{CultureName}.json";
        var sut = new EmbeddedResourceQuery();
        // var resources = typeof(T).Assembly.GetManifestResourceNames();

        // using var stream = sut.Read<T>(resource);
        using var stream = sut.Read(Assembly.GetAssembly(resourceType), resource, resourceType.Namespace);
        if (stream == null) return;
        var json = new StreamReader(stream).ReadToEnd();

        // Create static localization dictionary and fill current dictionary
        JsonLocalizationDictionaryBuilder.BuildFromJsonString(json).Fill(Dictionary);
    }

    private string GetLocalizationResourceName(MemberInfo desiredType)
    {
        var attributeInstance = Attribute.GetCustomAttribute(desiredType, typeof(LocalizationResourceNameAttribute));
        return attributeInstance == null ? string.Empty : (attributeInstance as LocalizationResourceNameAttribute)?.Name;
    }
}