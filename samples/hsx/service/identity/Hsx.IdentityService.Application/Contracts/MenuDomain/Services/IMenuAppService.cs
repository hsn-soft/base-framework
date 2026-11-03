using Hsx.IdentityService.Application.Contracts.MenuDomain.Dtos;

namespace Hsx.IdentityService.Application.Contracts.MenuDomain.Services;

public interface IMenuAppService
{
    /// <summary>
    /// The menu of the back office for the caller: only what it may see, each page with what it needs. A leaf is in it when the caller has the permission of its visibility, a node when something under it is, a caption when something after it, before the next caption,
    /// is. A caller that may see nothing gets an empty menu.
    /// </summary>
    Task<List<MenuDto>> GetMyMenusAsync(CancellationToken cancellationToken = default);
}
