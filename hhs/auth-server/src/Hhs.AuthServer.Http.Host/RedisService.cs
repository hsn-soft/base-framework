using StackExchange.Redis;

namespace Hhs.AuthServer;

public class RedisService
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisService(IConfiguration config)
    {
        _redis = ConnectionMultiplexer.Connect(config["Redis:Configuration"]);
        _db = _redis.GetDatabase();
    }

    // kullanıcıId => refresh token seti
    private string GetUserKey(Guid userId) => $"refreshTokens:{userId}";

    public async Task AddRefreshTokenAsync(Guid userId, string refreshToken, TimeSpan expiry)
    {
        var key = GetUserKey(userId);
        await _db.SetAddAsync(key, refreshToken);
        await _db.KeyExpireAsync(key, expiry); // TTL

        // ek olarak mapping ekle
        await _db.StringSetAsync($"refreshTokenToUser:{refreshToken}", userId.ToString(), TimeSpan.FromDays(7));
    }

    public async Task<Guid?> GetUserIdByRefreshTokenAsync(string refreshToken)
    {
        var userId = await _db.StringGetAsync($"refreshTokenToUser:{refreshToken}");

        return userId.IsNullOrEmpty ? null : Guid.Parse(userId.ToString());
    }

    public async Task RemoveRefreshTokenAsync(Guid userId, string refreshToken)
    {
        var key = GetUserKey(userId);
        await _db.SetRemoveAsync(key, refreshToken);

        // remove mapping
        await _db.KeyDeleteAsync($"refreshTokenToUser:{refreshToken}");
    }

    public async Task<bool> IsRefreshTokenValidAsync(Guid userId, string refreshToken)
    {
        var key = GetUserKey(userId);
        return await _db.SetContainsAsync(key, refreshToken);
    }

    public async Task RemoveAllUserTokensAsync(Guid userId)
    {
        var key = GetUserKey(userId);
        await _db.KeyDeleteAsync(key);
    }
}