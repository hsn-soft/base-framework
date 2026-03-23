using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Localization.Abstractions;

public interface ILocalizableString
{
    LocalizedString Localize(IStringLocalizerFactory stringLocalizerFactory);
}