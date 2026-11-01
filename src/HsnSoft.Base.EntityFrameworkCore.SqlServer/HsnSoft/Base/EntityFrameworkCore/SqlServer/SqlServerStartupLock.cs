using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Data;
using Microsoft.Data.SqlClient;

namespace HsnSoft.Base.EntityFrameworkCore.SqlServer;

/// <summary>
/// The startup lock of SQL Server: an application lock (<c>sp_getapplock</c>) of a session on the server, taken on the <c>master</c> database (the database that is being prepared may not exist yet). The session has a
/// connection of its own (no pool), so closing it, which a process that dies does, releases the lock. It asks the server for the lock in turns of a few seconds until its own deadline.
/// </summary>
public sealed class SqlServerStartupLock(string connectionString) : IStartupLock
{
    private readonly string _connectionString = connectionString;

    // the server waits for the lock only this long in one call: the deadline is the clock of this process, not the timer of the server (a server in an emulator has been seen to wait a tenth of what it was told)
    private static readonly TimeSpan s_attempt = TimeSpan.FromSeconds(5);

    public async Task<IAsyncDisposable> AcquireAsync(string name, TimeSpan timeout, CancellationToken cancellationToken)
    {
        SqlConnectionStringBuilder builder = new(_connectionString) { InitialCatalog = "master", Pooling = false };
        SqlConnection connection = new(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            DateTime deadline = DateTime.UtcNow + timeout;
            while (true)
            {
                TimeSpan remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    throw new TimeoutException($"The startup lock '{name}' was not free within {timeout}.");
                }

                int result = await TryAsync(connection, name, (int)Math.Max(1, Math.Min(s_attempt.TotalMilliseconds, remaining.TotalMilliseconds)), cancellationToken).ConfigureAwait(false);
                if (result >= 0)
                {
                    return new Held(connection, name);
                }

                if (result != -1)
                {
                    // -2 the request was cancelled, -3 it was chosen as a deadlock victim, -999 it was not valid: the step is tried again by its caller
                    throw new InvalidOperationException($"sp_getapplock for the startup lock '{name}' returned {result}.");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<int> TryAsync(SqlConnection connection, string name, int waitMilliseconds, CancellationToken cancellationToken)
    {
        SqlCommand command = connection.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "sp_getapplock";
            command.CommandTimeout = 60;
            command.Parameters.AddWithValue("@Resource", name);
            command.Parameters.AddWithValue("@LockMode", "Exclusive");
            command.Parameters.AddWithValue("@LockOwner", "Session");
            command.Parameters.AddWithValue("@LockTimeout", waitMilliseconds);
            SqlParameter result = command.Parameters.Add("@Result", SqlDbType.Int);
            result.Direction = ParameterDirection.ReturnValue;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return (int)result.Value;
        }
    }

    private sealed class Held(SqlConnection connection, string name) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                SqlCommand command = connection.CreateCommand();
                await using (command.ConfigureAwait(false))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "sp_releaseapplock";
                    command.Parameters.AddWithValue("@Resource", name);
                    command.Parameters.AddWithValue("@LockOwner", "Session");
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
            catch (SqlException)
            {
                // the connection is closed next: the server releases the lock with the session either way
            }
            finally
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
