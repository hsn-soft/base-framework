using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Data;
using MySql.Data.MySqlClient;

namespace HsnSoft.Base.EntityFrameworkCore.MySql;

/// <summary>
/// The startup lock of MySQL: <c>GET_LOCK</c> of a session on the server, taken with no database selected (the database that is being prepared may not exist yet). The session has a connection of its own (no pool),
/// so closing it, which a process that dies does, releases the lock. It tries again until its own deadline.
/// </summary>
public sealed class MySqlStartupLock(string connectionString) : IStartupLock
{
    private readonly string _connectionString = connectionString;

    private static readonly TimeSpan s_poll = TimeSpan.FromMilliseconds(500);

    public async Task<IAsyncDisposable> AcquireAsync(string name, TimeSpan timeout, CancellationToken cancellationToken)
    {
        MySqlConnectionStringBuilder builder = new(_connectionString) { Database = string.Empty, Pooling = false };
        MySqlConnection connection = new(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            DateTime deadline = DateTime.UtcNow + timeout;
            while (true)
            {
                // a try (no wait on the server): the deadline is the clock of this process
                MySqlCommand command = new("SELECT GET_LOCK(@name, 0)", connection);
                await using (command.ConfigureAwait(false))
                {
                    command.Parameters.AddWithValue("@name", name);
                    object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    if (Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) == 1)
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

    private sealed class Held(MySqlConnection connection, string name) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                MySqlCommand command = new("SELECT RELEASE_LOCK(@name)", connection);
                await using (command.ConfigureAwait(false))
                {
                    command.Parameters.AddWithValue("@name", name);
                    await command.ExecuteScalarAsync().ConfigureAwait(false);
                }
            }
            catch (MySqlException)
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
