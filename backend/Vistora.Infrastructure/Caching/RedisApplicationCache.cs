using StackExchange.Redis;
using Vistora.Application.Caching;

namespace Vistora.Infrastructure.Caching;

public sealed class RedisApplicationCache(IConnectionMultiplexer connection) : IApplicationCache
{
    private readonly IDatabase _database = connection.GetDatabase();

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();
        return await _database.StringGetAsync(key);
    }

    public async Task SetAsync(string key, string value, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        if (lifetime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(lifetime));

        cancellationToken.ThrowIfCancellationRequested();
        await _database.StringSetAsync(key, value, lifetime);
    }
}
