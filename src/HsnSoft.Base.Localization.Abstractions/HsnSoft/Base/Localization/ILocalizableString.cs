using Microsoft.Extensions.Localization;

namespace HsnSoft.Base.Localization;

public interface ILocalizableString
{
    LocalizedString Localize(IStringLocalizerFactory stringLocalizerFactory);
}