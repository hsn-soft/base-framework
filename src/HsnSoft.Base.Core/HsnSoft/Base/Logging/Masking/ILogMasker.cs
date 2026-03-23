using JetBrains.Annotations;

namespace HsnSoft.Base.Logging.Masking;

public interface ILogMasker
{
    string MaskText([CanBeNull] string text);
    object MaskObject<T>(T value);
}