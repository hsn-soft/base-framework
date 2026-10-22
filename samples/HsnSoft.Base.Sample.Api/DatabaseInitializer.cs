using HsnSoft.Base.Sample.Api.EfCore.Context;
using HsnSoft.Base.Sample.Api.MongoDb.Context;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace HsnSoft.Base.Sample.Api;

/// <summary>Applies pending migrations and seeds sample data. Controlled by the <c>Database:AutoInitialize</c> setting.</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        await InitializeEfCoreAsync(scope.ServiceProvider);
        await InitializeMongoDbAsync(scope.ServiceProvider);
    }

    private static async Task InitializeEfCoreAsync(IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<AppEfCoreDbContext>();
        try
        {
            await dbContext.Database.MigrateAsync();
            Log.Information("{WorkerName} | MIGRATIONS APPLIED", nameof(DatabaseInitializer));

            if (!await dbContext.Users.AnyAsync())
            {
                dbContext.Users.AddRange(SeedData.GenerateUserData(5));
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            Log.Error("{WorkerName} | {OperationStatus} | {Error}", nameof(DatabaseInitializer), "EFCORE FAIL", e.Message);
        }
    }

    private static async Task InitializeMongoDbAsync(IServiceProvider services)
    {
        var mongoDbContext = services.GetRequiredService<AppMongoDbContext>();
        try
        {
            if (await mongoDbContext.Users.EstimatedDocumentCountAsync() < 1)
            {
                await mongoDbContext.Users.InsertManyAsync(SeedData.GenerateUserData(5));
            }
        }
        catch (Exception e)
        {
            Log.Error("{WorkerName} | {OperationStatus} | {Error}", nameof(DatabaseInitializer), "MONGODB FAIL", e.Message);
        }
    }
}
