namespace Hhs.TextNormalizerService.Domain;

public static class DomainErrorCodes
{
    public const string NormalizedRequestNotFound = "Error:TextNormalizerService:000021";
    public const string NormalizedRequestStateError = "Error:TextNormalizerService:000022";

    public const string NormalizedAnalysisNotFound = "Error:TextNormalizerService:000031";
    public const string NormalizedAnalysisStateError = "Error:TextNormalizerService:000032";
}