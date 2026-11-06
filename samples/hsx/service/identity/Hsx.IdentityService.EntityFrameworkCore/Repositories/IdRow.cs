namespace Hsx.IdentityService.EntityFrameworkCore.Repositories;

/// <summary>One id read from a list: the projection of the repository needs a class to read into, and the ids are what some queries ask for.</summary>
internal sealed record IdRow(Guid Id);
