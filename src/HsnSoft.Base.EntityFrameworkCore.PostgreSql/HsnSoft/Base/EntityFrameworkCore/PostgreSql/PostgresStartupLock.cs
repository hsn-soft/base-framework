using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Data;
using Npgsql;

namespace HsnSoft.Base.EntityFrameworkCore.PostgreSql;

/// <summary>
/// The startup lock of PostgreSQL: an advisory lock of a session, taken on the maintenance database <c>postgres</c> (the database that is being prepared may not exist yet, and the advisory locks of a database
/// are those of its own). The session has a connection of its own (no pool), so closing it, which a process that dies does, releases the lock.
/// </summary>
public sealed class PostgresStartupLock(string connectionString) : IStartupLock
{
    private static readonly TimeSpan s_poll = TimeSpan.FromMilliseconds(500);

    private readonly string _connectionString = connectionString;

    public async Task<IAsyncDisposable> AcquireAsync(string name, TimeSpan timeout, CancellationToken cancellationToken)
    {
        NpgsqlConnectionStringBuilder builder = new(_connectionString) { Database = "postgres", Pooling = false };
        NpgsqlConnection connection = new(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            DateTime deadline = DateTime.UtcNow + timeout;
            while (true)
            {
                NpgsqlCommand command = new("SELECT pg_try_advisory_lock(hashtextextended(@name, 0))", connection);
                await using (command.ConfigureAwait(false))
                {
                    command.Parameters.AddWithValue("name", name);
                    if ((bool)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!)
                    {
                        return new Held(connection, name);
                    }
                }

                if (DateTime.UtcNow >= deadline)
                {
                    throw new TimeoutException($"The startup lock '{name}' was not free within {timeout}.");
                }

                await Task.Delay(s_poll, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private sealed class Held(NpgsqlConnection connection, string name) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                NpgsqlCommand command = new("SELECT pg_advisory_unlock(hashtextextended(@name, 0))", connection);
                await using (command.ConfigureAwait(false))
                {
                    command.Parameters.AddWithValue("name", name);
                    await command.ExecuteScalarAsync().ConfigureAwait(false);
                }
            }
            catch (NpgsqlException)
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
