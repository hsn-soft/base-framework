namespace Hhs.ContentService.Domain;

public static class DomainErrorCodes
{
    public const string CustomerContentNotFound = "Error:ContentService:000021";
    public const string CustomerContentDuplicate = "Error:ContentService:000022";
    public const string CustomerContentStateError = "Error:ContentService:000023";

    public const string AnalysisContentNotFound = "Error:ContentService:000051";
    public const string AnalysisContentStateError = "Error:ContentService:000052";

    public const string CustomerVpSettingNotFound = "Error:ContentService:000031";
    public const string CustomerVpSettingDuplicate = "Error:ContentService:000032";
    public const string CustomerVpSettingInvalidDomain = "Error:ContentService:000033";
    public const string CustomerVpSettingDomainBlocked = "Error:ContentService:000034";
}