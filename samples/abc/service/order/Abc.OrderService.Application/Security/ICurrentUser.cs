namespace Abc.OrderService.Application.Security;

/// <summary>Who is making the request: the application layer asks, the host answers from the token.</summary>
public interface ICurrentUser
{
    /// <summary>The <c>sub</c> claim of the token (the user name), or <see langword="null"/> when there is no signed-in user.</summary>
    string? UserName { get; }
}
