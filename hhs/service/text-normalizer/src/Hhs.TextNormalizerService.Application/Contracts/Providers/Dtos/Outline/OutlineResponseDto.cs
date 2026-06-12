namespace Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline;

public class OutlineResponseDto
{
    public Guid RefContentId { get; set; }
    public string OutlinedData { get; set; }

    public bool HasError { get; set; }
    public string ErrorDetails { get; set; }
}