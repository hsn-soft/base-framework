using Hhs.TextNormalizerService.Application.Contracts.Providers.Dtos.Outline;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.Providers;

public interface IOutlineProvider
{
    Task<OutlineResponseDto> OutlineAsync(OutlineRequestDto input, [CanBeNull] string model=null, bool useStructuredOutput = false);
    Task<List<OutlineResponseDto>> OutlineAsync(List<OutlineRequestDto> input);
    Task<OutlineResponseDto> OutlineWithStructuredOutputAsync(OutlineRequestDto input, [CanBeNull] string model=null);
}