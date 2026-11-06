using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hsx.ContentService.EntityFrameworkCore.Context;

/// <summary>
/// What <c>dotnet ef migrations add</c> makes the context with: it only reads the model, so the connection string is a placeholder and nothing is opened. The host makes it with the options of <c>AddBaseEfCore</c> and applies the
/// migrations when it starts (<c>AddMigrationStartupStep</c>).
/// </summary>
internal sealed class ContentDbContextFactory : IDesignTimeDbContextFactory<ContentDbContext>
{
    public ContentDbContext CreateDbContext(string[] args)
        => new(new DbContextOptionsBuilder<ContentDbContext>().UseNpgsql("Host=localhost;Database=design-time-only").Options);
}
