using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace RecommendationsService.Services;

/// <summary>
/// Resilient Hybrid Distributed Cache that attempts Redis with a fast timeout,
/// and seamlessly falls back to In-Memory cache when Redis is unavailable.
/// </summary>
public class ResilientDistributedCache : IDistributedCache
{
    private readonly IDistributedCache _redisCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ResilientDistributedCache> _logger;
    private bool _isRedisAvailable = true;
    private DateTimeOffset _lastRedisFailure = DateTimeOffset.MinValue;
    private readonly TimeSpan _redisRetryInterval = TimeSpan.FromSeconds(30);

    public ResilientDistributedCache(
        IDistributedCache redisCache,
        IMemoryCache memoryCache,
        ILogger<ResilientDistributedCache> logger)
    {
        _redisCache = redisCache;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public byte[]? Get(string key)
    {
        if (ShouldAttemptRedis())
        {
            try
            {
                var val = _redisCache.Get(key);
                if (val is not null)
                {
                    _memoryCache.Set(key, val, TimeSpan.FromMinutes(1));
                    return val;
                }
            }
            catch (Exception ex)
            {
                RecordRedisFailure(ex, "Get");
            }
        }

        return _memoryCache.TryGetValue(key, out byte[]? memVal) ? memVal : null;
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        if (ShouldAttemptRedis())
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(TimeSpan.FromMilliseconds(200));

                var val = await _redisCache.GetAsync(key, cts.Token);
                if (val is not null)
                {
                    _memoryCache.Set(key, val, TimeSpan.FromMinutes(1));
                    return val;
                }
            }
            catch (Exception ex)
            {
                RecordRedisFailure(ex, "GetAsync");
            }
        }

        return _memoryCache.TryGetValue(key, out byte[]? memVal) ? memVal : null;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        _memoryCache.Set(key, value, options.SlidingExpiration ?? options.AbsoluteExpirationRelativeToNow ?? TimeSpan.FromMinutes(5));

        if (ShouldAttemptRedis())
        {
            try
            {
                _redisCache.Set(key, value, options);
            }
            catch (Exception ex)
            {
                RecordRedisFailure(ex, "Set");
            }
        }
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        _memoryCache.Set(key, value, options.SlidingExpiration ?? options.AbsoluteExpirationRelativeToNow ?? TimeSpan.FromMinutes(5));

        if (ShouldAttemptRedis())
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(TimeSpan.FromMilliseconds(200));
                await _redisCache.SetAsync(key, value, options, cts.Token);
            }
            catch (Exception ex)
            {
                RecordRedisFailure(ex, "SetAsync");
            }
        }
    }

    public void Refresh(string key)
    {
        if (ShouldAttemptRedis())
        {
            try { _redisCache.Refresh(key); }
            catch (Exception ex) { RecordRedisFailure(ex, "Refresh"); }
        }
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        if (ShouldAttemptRedis())
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(TimeSpan.FromMilliseconds(200));
                await _redisCache.RefreshAsync(key, cts.Token);
            }
            catch (Exception ex) { RecordRedisFailure(ex, "RefreshAsync"); }
        }
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
        if (ShouldAttemptRedis())
        {
            try { _redisCache.Remove(key); }
            catch (Exception ex) { RecordRedisFailure(ex, "Remove"); }
        }
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        _memoryCache.Remove(key);
        if (ShouldAttemptRedis())
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(TimeSpan.FromMilliseconds(200));
                await _redisCache.RemoveAsync(key, cts.Token);
            }
            catch (Exception ex) { RecordRedisFailure(ex, "RemoveAsync"); }
        }
    }

    private bool ShouldAttemptRedis()
    {
        if (_isRedisAvailable) return true;
        if (DateTimeOffset.UtcNow - _lastRedisFailure > _redisRetryInterval)
        {
            _isRedisAvailable = true;
            return true;
        }
        return false;
    }

    private void RecordRedisFailure(Exception ex, string operation)
    {
        _isRedisAvailable = false;
        _lastRedisFailure = DateTimeOffset.UtcNow;
        _logger.LogDebug("[ResilientDistributedCache] Redis {Operation} timed out or failed ({Message}). Utilizing memory cache fallback.", operation, ex.Message);
    }
}
