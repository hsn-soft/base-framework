namespace Hhs.ContentService.Domain;

public static class DomainErrorCodes
{
    public const string AppContentNotFound = "Error:ContentService:000021";
    public const string AppContentDuplicate = "Error:ContentService:000022";
    public const string AppContentStateError = "Error:ContentService:000023";

    public const string AnalysisContentNotFound = "Error:ContentService:000051";
    public const string AnalysisContentStateError = "Error:ContentService:000052";

    public const string CustomerContentSettingNotFound = "Error:ContentService:000031";
    public const string CustomerContentSettingDuplicate = "Error:ContentService:000032";
    public const string CustomerContentSettingInvalidDomain = "Error:ContentService:000033";
    public const string CustomerContentSettingDomainBlocked = "Error:ContentService:000034";

    public const string CustomerContentSettingPathFilterNotFound = "Error:ContentService:000041";
}