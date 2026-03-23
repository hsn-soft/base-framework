using JetBrains.Annotations;

namespace Microsoft.Extensions.Localization;

public interface IStringLocalizerFactoryWithDefaultResourceSupport
{
    [CanBeNull]
    IStringLocalizer CreateDefaultOrNull();
}