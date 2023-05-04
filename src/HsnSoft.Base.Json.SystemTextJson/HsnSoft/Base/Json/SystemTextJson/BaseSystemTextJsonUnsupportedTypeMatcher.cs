using System;
using HsnSoft.Base.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Json.SystemTextJson;

public class BaseSystemTextJsonUnsupportedTypeMatcher : ITransientDependency
{
    public BaseSystemTextJsonUnsupportedTypeMatcher(IOptions<BaseSystemTextJsonSerializerOptions> options)
    {
        Options = options.Value;
    }

    protected BaseSystemTextJsonSerializerOptions Options { get; }

    public virtual bool Match([CanBeNull] Type type)
    {
        return Options.UnsupportedTypes.Contains(type);
    }
}