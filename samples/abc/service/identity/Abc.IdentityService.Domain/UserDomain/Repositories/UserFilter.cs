namespace Abc.IdentityService.Domain.UserDomain.Repositories;

/// <summary>What a list of users is narrowed by: every given value must match (names and emails by a part of them, without regard to case).</summary>
public sealed record UserFilter(string? UserName = null, string? Email = null, bool? IsActive = null);
