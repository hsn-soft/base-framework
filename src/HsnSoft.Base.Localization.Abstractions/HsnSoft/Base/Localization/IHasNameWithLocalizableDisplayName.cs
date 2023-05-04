using JetBrains.Annotations;

namespace HsnSoft.Base.Localization;

public interface IHasNameWithLocalizableDisplayName
{
    [NotNull]
    public string Name { get; }

    [CanBeNull]
    public ILocalizableString DisplayName { get; }
}