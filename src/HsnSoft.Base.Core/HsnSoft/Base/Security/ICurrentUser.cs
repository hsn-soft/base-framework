namespace HsnSoft.Base.Security;

/// <summary>
/// Who is making the request, for the code that does not know HTTP (an application service that writes who created a row). The host answers it from the token
/// (<c>HsnSoft.Base.Authentication</c> registers it with the JWT authentication); a test or a worker answers it with its own.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The user id (the <c>sub</c> claim), or <see langword="null"/> when there is no signed-in user.</summary>
    string? UserId { get; }

    /// <summary>Whether a user is signed in.</summary>
    bool IsAuthenticated { get; }
}
