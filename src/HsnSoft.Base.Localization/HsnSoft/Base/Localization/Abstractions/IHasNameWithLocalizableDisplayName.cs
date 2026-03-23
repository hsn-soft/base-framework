using JetBrains.Annotations;

namespace HsnSoft.Base.Localization.Abstractions;

public interface IHasNameWithLocalizableDisplayName
{
    [NotNull]
    public string Name { get; }

    [CanBeNull]
    public ILocalizableString DisplayName { get; }
}