using JetBrains.Annotations;

namespace HsnSoft.Base.Localization.Abstractions;

public interface IHasNameWithLocalizableDisplayName
{
    [NotNull] string Name { get; }

    [CanBeNull] ILocalizableString DisplayName { get; }
}