using Hhs.AdministrationService.Domain.Enums;
using Hhs.AdministrationService.Domain.MenuDomain.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.AdministrationService.Domain.MenuDomain.Entities;

public sealed class AppMenu : Entity<Guid>
{
    public Guid? ParentId { get; set; }
    [CanBeNull] public AppMenu Parent { get; set; }
    public ICollection<AppMenu> Nodes { get; set; }

    [NotNull] public string UniqueCode { get; private set; }

    public ChannelTypes ChannelType { get; set; }
    public MenuDockTypes DockType { get; set; }
    public MenuNodeTypes NodeType { get; set; }

    [CanBeNull] public string Title { get; private set; }

    [CanBeNull] public string Route { get; private set; }

    [CanBeNull] public string Icon { get; private set; }

    public int SortOrder { get; set; }

    // Navigation fields
    public ICollection<AppMenuPermission> MenuPermissions { get; set; }

    private AppMenu()
    {
        // Not-Null string fields
        UniqueCode = string.Empty;

        // Navigation fields
        Parent = null;
        Nodes = [];
        MenuPermissions = [];
    }

    internal AppMenu(
        [NotNull] string uniqueCode,
        ChannelTypes channelType,
        MenuDockTypes dockType,
        MenuNodeTypes nodeType,
        [CanBeNull] string title = null,
        [CanBeNull] string route = null,
        [CanBeNull] string icon = null,
        int sortOrder = 0,
        Guid? parentId = null
    ) : this(Guid.CreateVersion7(), uniqueCode, channelType, dockType, nodeType, title, route, icon, sortOrder, parentId)
    {
    }

    internal AppMenu(Guid id,
        [NotNull] string uniqueCode,
        ChannelTypes channelType,
        MenuDockTypes dockType,
        MenuNodeTypes nodeType,
        [CanBeNull] string title = null,
        [CanBeNull] string route = null,
        [CanBeNull] string icon = null,
        int sortOrder = 0,
        Guid? parentId = null
    ) : this()
    {
        Id = id;
        ParentId = parentId;

        SetUniqueCode(uniqueCode);
        ChannelType = channelType;
        DockType = dockType;
        NodeType = nodeType;
        SetTitle(title);
        SetRoute(route);
        SetIcon(icon);
        SortOrder = sortOrder;
    }

    private void SetUniqueCode(string uniqueCode)
    {
        string checkValue = LocalizedModelValidator.NotNullOrWhiteSpace(uniqueCode, $"{nameof(AppMenu)}:{nameof(UniqueCode)}", AppMenuConsts.UniqueCodeMaxLength);
        UniqueCode = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(checkValue));
    }

    internal void SetTitle(string title) =>
        Title = !string.IsNullOrWhiteSpace(title)
            ? LocalizedModelValidator.NotNullOrWhiteSpace(title, $"{nameof(AppMenu)}:{nameof(Title)}", AppMenuConsts.TitleMaxLength)
            : null;

    internal void SetRoute(string route) =>
        Route = !string.IsNullOrWhiteSpace(route)
            ? LocalizedModelValidator.NotNullOrWhiteSpace(route, $"{nameof(AppMenu)}:{nameof(Route)}", AppMenuConsts.RouteMaxLength)
            : null;

    internal void SetIcon(string icon) =>
        Icon = !string.IsNullOrWhiteSpace(icon)
            ? LocalizedModelValidator.NotNullOrWhiteSpace(icon, $"{nameof(AppMenu)}:{nameof(Icon)}", AppMenuConsts.IconMaxLength)
            : null;
}