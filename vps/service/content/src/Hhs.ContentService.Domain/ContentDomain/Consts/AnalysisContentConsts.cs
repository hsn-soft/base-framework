namespace Hhs.ContentService.Domain.ContentDomain.Consts;

public static class AnalysisContentConsts
{
    public const string TableName = "analysis_contents";
    public const int ScopeKeyMaxLength = 100;
    public const int DomainNameMaxLength = 500;
    public const int TitleMaxLength = 500;
    public const int CorrelationIdMaxLength = 50;
    public const int NormalizeStatusMaxLength = 80;
    public const int VideoStatusMaxLength = 80;
    public const int FinalVideoUrlMaxLength = 2000;
    public const int LastFacilityMaxLength = 120;
    public const int LastErrorMaxLength = 1000;
}

public static class AnalysisContentItemConsts
{
    public const string TableName = "analysis_content_items";
}
