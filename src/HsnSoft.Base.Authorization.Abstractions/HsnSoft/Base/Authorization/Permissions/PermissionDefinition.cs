using System.Collections.Generic;
using System.Collections.Immutable;
using HsnSoft.Base.Localization;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;

namespace HsnSoft.Base.Authorization.Permissions;

public class PermissionDefinition
{
    private readonly List<PermissionDefinition> _children;

    private ILocalizableString _displayName;

    protected internal PermissionDefinition(
        [NotNull] string name,
        ILocalizableString displayName = null,
        MultiTenancySides multiTenancySide = MultiTenancySides.Both,
        bool isEnabled = true)
    {
        Name = Check.NotNull(name, nameof(name));
        DisplayName = displayName ?? new FixedLocalizableString(name);
        MultiTenancySide = multiTenancySide;
        IsEnabled = isEnabled;

        Properties = new Dictionary<string, object>();
        _children = new List<PermissionDefinition>();
    }

    /// <summary>
    /// Unique name of the permission.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Parent of this permission if one exists.
    /// If set, this permission can be granted only if parent is granted.
    /// </summary>
    public PermissionDefinition Parent { get; private set; }

    /// <summary>
    /// MultiTenancy side.
    /// Default: <see cref="MultiTenancySides.Both"/>
    /// </summary>
    public MultiTenancySides MultiTenancySide { get; set; }

    public ILocalizableString DisplayName
    {
        get => _displayName;
        set => _displayName = Check.NotNull(value, nameof(value));
    }

    public IReadOnlyList<PermissionDefinition> Children => _children.ToImmutableList();

    /// <summary>
    /// Can be used to get/set custom properties for this permission definition.
    /// </summary>
    public Dictionary<string, object> Properties { get; }

    /// <summary>
    /// Indicates whether this permission is enabled or disabled.
    /// A permission is normally enabled.
    /// A disabled permission can not be granted to anyone, but it is still
    /// will be available to check its value (while it will always be false).
    ///
    /// Disabling a permission would be helpful to hide a related application
    /// functionality from users/clients.
    ///
    /// Default: true.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets/sets a key-value on the <see cref="Properties"/>.
    /// </summary>
    /// <param name="name">Name of the property</param>
    /// <returns>
    /// Returns the value in the <see cref="Properties"/> dictionary by given <paramref name="name"/>.
    /// Returns null if given <paramref name="name"/> is not present in the <see cref="Properties"/> dictionary.
    /// </returns>
    public object this[string name]
    {
        get => Properties.GetOrDefault(name);
        set => Properties[name] = value;
    }

    public virtual PermissionDefinition AddChild(
        [NotNull] string name,
        ILocalizableString displayName = null,
        MultiTenancySides multiTenancySide = MultiTenancySides.Both,
        bool isEnabled = true)
    {
        var child = new PermissionDefinition(
            name,
            displayName,
            multiTenancySide,
            isEnabled) { Parent = this };

        _children.Add(child);

        return child;
    }

    /// <summary>
    /// Sets a property in the <see cref="Properties"/> dictionary.
    /// This is a shortcut for nested calls on this object.
    /// </summary>
    public virtual PermissionDefinition WithProperty(string key, object value)
    {
        Properties[key] = value;
        return this;
    }

    public override string ToString()
    {
        return $"[{nameof(PermissionDefinition)} {Name}]";
    }
}