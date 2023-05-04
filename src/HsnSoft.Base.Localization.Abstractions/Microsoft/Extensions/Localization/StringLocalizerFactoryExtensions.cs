namespace Microsoft.Extensions.Localization;

public static class StringLocalizerFactoryExtensions
{
    public static IStringLocalizer CreateDefaultOrNull(this IStringLocalizerFactory localizerFactory)
    {
        return (localizerFactory as IStringLocalizerFactoryWithDefaultResourceSupport)
            ?.CreateDefaultOrNull();
    }
}