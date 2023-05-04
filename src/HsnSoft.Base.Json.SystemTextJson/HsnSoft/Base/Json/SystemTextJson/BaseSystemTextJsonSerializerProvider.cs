using System;
using System.Collections.Concurrent;
using System.Text.Json;
using HsnSoft.Base.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Json.SystemTextJson;

public class BaseSystemTextJsonSerializerProvider : IJsonSerializerProvider, ITransientDependency
{
    private readonly ConcurrentDictionary<string, JsonSerializerOptions> JsonSerializerOptionsCache = new ConcurrentDictionary<string, JsonSerializerOptions>();

    public BaseSystemTextJsonSerializerProvider(
        IOptions<BaseSystemTextJsonSerializerOptions> options,
        BaseSystemTextJsonUnsupportedTypeMatcher baseSystemTextJsonUnsupportedTypeMatcher)
    {
        BaseSystemTextJsonUnsupportedTypeMatcher = baseSystemTextJsonUnsupportedTypeMatcher;
        Options = options.Value;
    }

    protected BaseSystemTextJsonSerializerOptions Options { get; }

    protected BaseSystemTextJsonUnsupportedTypeMatcher BaseSystemTextJsonUnsupportedTypeMatcher { get; }

    public bool CanHandle(Type type)
    {
        return !BaseSystemTextJsonUnsupportedTypeMatcher.Match(type);
    }

    public string Serialize(object obj, bool camelCase = true, bool indented = false)
    {
        return JsonSerializer.Serialize(obj, CreateJsonSerializerOptions(camelCase, indented));
    }

    public T Deserialize<T>(string jsonString, bool camelCase = true)
    {
        return JsonSerializer.Deserialize<T>(jsonString, CreateJsonSerializerOptions(camelCase));
    }

    public object Deserialize(Type type, string jsonString, bool camelCase = true)
    {
        return JsonSerializer.Deserialize(jsonString, type, CreateJsonSerializerOptions(camelCase));
    }

    protected virtual JsonSerializerOptions CreateJsonSerializerOptions(bool camelCase = true, bool indented = false)
    {
        return JsonSerializerOptionsCache.GetOrAdd($"default{camelCase}{indented}", _ =>
        {
            var settings = new JsonSerializerOptions(Options.JsonSerializerOptions);

            if (camelCase)
            {
                settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            }

            if (indented)
            {
                settings.WriteIndented = true;
            }

            return settings;
        });
    }
}