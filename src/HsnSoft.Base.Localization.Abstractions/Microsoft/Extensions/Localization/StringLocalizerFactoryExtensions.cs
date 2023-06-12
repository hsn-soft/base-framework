using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Localization;

public static class StringLocalizerFactoryExtensions
{
    public static IStringLocalizer CreateDefaultOrNull(this IStringLocalizerFactory localizerFactory)
    {
        return (localizerFactory as IStringLocalizerFactoryWithDefaultResourceSupport)
            ?.CreateDefaultOrNull();
    }

    public static IStringLocalizer CreateMultiple(this IStringLocalizerFactory localizerFactory, List<Type> resourceTypes)
    {
        return (localizerFactory as IStringLocalizerFactoryWithMultiple)
            ?.CreateMultiple(resourceTypes);
    }
}