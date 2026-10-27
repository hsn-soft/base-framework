namespace HsnSoft.Base.Authorization;

/// <summary>
/// Says in which scope the grants of the caller are kept, when the system has more than one (the tenant: the roles of a tenant are its own, so an <c>admin</c> of one tenant is not the <c>admin</c> of another). A product that has
/// scopes registers one (<see cref="PermissionServiceCollectionExtensions.AddPermissionScope{TProvider}"/>); without one the grants are system wide. The framework knows no tenant: the product decides what a scope is.
/// </summary>
public interface IPermissionScopeProvider
{
    /// <summary>The scope of the current caller, or <see langword="null"/> when it has none.</summary>
    string? GetScope();
}
