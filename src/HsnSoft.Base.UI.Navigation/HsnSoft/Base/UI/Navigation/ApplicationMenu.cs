using JetBrains.Annotations;

namespace HsnSoft.Base.UI.Navigation;

public class ApplicationMenu : IHasMenuItems
{
    private string _displayName;

    public ApplicationMenu(
        [NotNull] string name,
        string displayName = null)
    {
        Check.NotNullOrWhiteSpace(name, nameof(name));

        Name = name;
        DisplayName = displayName ?? Name;

        Items = new ApplicationMenuItemList();
    }

    /// <summary>
    /// Unique name of the menu in the application.
    /// </summary>
    [NotNull]
    public string Name { get; }

    /// <summary>
    /// Display name of the menu.
    /// Default value is the <see cref="Name"/>.
    /// </summary>
    [NotNull]
    public string DisplayName
    {
        get { return _displayName; }
        set
        {
            Check.NotNullOrWhiteSpace(value, nameof(value));
            _displayName = value;
        }
    }

    /// <summary>
    /// Can be used to store a custom object related to this menu.
    /// TODO: Convert to dictionary!
    /// </summary>
    [CanBeNull]
    public object CustomData { get; set; }

    /// <inheritdoc cref="IHasMenuItems.Items"/>
    [NotNull]
    public ApplicationMenuItemList Items { get; }

    /// <summary>
    /// Adds a <see cref="ApplicationMenuItem"/> to <see cref="Items"/>.
    /// </summary>
    /// <param name="menuItem"><see cref="ApplicationMenuItem"/> to be added</param>
    /// <returns>This <see cref="ApplicationMenu"/> object</returns>
    public ApplicationMenu AddItem([NotNull] ApplicationMenuItem menuItem)
    {
        Items.Add(menuItem);
        return this;
    }

    public override string ToString()
    {
        return $"[ApplicationMenu] Name = {Name}";
    }
}