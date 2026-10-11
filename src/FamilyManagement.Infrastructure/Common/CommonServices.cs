using FamilyManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FamilyManagement.Infrastructure.Common;

public class DateTimeProvider(TimeProvider timeProvider) : IDateTimeProvider
{
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
    public DateOnly Today => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
}

/// <summary>IMemoryCache-backed cache. Swap for a distributed implementation (Redis) without touching callers.</summary>
public class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) =>
        Task.FromResult(cache.TryGetValue(key, out T? value) ? value : default);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        cache.Set(key, value, ttl);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await factory(ct);
        cache.Set(key, value, ttl);
        return value;
    }

    public void Remove(params string[] keys)
    {
        foreach (var key in keys)
            cache.Remove(key);
    }
}

/// <summary>
/// Development default: writes the mail to the log. Replace with an SMTP/API-based
/// implementation by registering a different IEmailService.
/// </summary>
public class LoggingEmailService(ILogger<LoggingEmailService> logger) : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation("EMAIL to {To} | {Subject} | {Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Development default: writes the SMS to the log. Replace with a provider-specific
/// implementation by registering a different ISmsService.
/// </summary>
public class LoggingSmsService(ILogger<LoggingSmsService> logger) : ISmsService
{
    public Task SendAsync(string mobileNumber, string message, CancellationToken ct = default)
    {
        logger.LogInformation("SMS to {Mobile} | {Message}", mobileNumber, message);
        return Task.CompletedTask;
    }
}
