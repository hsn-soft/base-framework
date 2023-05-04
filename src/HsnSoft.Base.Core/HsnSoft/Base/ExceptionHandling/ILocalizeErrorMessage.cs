using HsnSoft.Base.Localization;

namespace HsnSoft.Base.ExceptionHandling;

public interface ILocalizeErrorMessage
{
    string LocalizeMessage(LocalizationContext context);
}