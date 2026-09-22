namespace Vistora.Application.Caching;

public interface IApplicationCache
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task SetAsync(string key, string value, TimeSpan lifetime, CancellationToken cancellationToken = default);
}
