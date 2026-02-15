using System.Collections.Concurrent;

namespace BookingSystem.API.Middleware;

/// <summary>
/// Rate limiting middleware to prevent brute force attacks and DoS.
/// Implements fixed window rate limiting using in-memory cache.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    
    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _requestCounts = new();
    

    private const int AuthEndpointLimit = 5;  
    private const int GeneralEndpointLimit = 100;  
    private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1);
    
    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var endpoint = context.Request.Path.Value ?? "/";
        var key = $"{clientIp}:{endpoint}";
        
        var limit = IsAuthEndpoint(endpoint) ? AuthEndpointLimit : GeneralEndpointLimit;
        
        var now = DateTime.UtcNow;
        var (count, windowStart) = _requestCounts.GetOrAdd(key, _ => (0, now));
        
        if (now - windowStart > WindowDuration)
        {
            _requestCounts[key] = (1, now);
            await _next(context);
            return;
        }
        
        if (count >= limit)
        {
            var retryAfter = (int)(WindowDuration - (now - windowStart)).TotalSeconds;
            
            _logger.LogWarning(
                "Rate limit exceeded for {ClientIp} on {Endpoint}. Limit: {Limit}, Count: {Count}",
                clientIp, endpoint, limit, count);
            
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = retryAfter.ToString();
            context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)windowStart.Add(WindowDuration)).ToUnixTimeSeconds().ToString();
            
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc6585#section-4",
                title = "Too Many Requests",
                status = 429,
                detail = $"Rate limit exceeded. Try again in {retryAfter} seconds.",
                instance = context.Request.Path.Value
            });
            
            return;
        }
        
        _requestCounts[key] = (count + 1, windowStart);
        
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, limit - count - 1).ToString();
            context.Response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)windowStart.Add(WindowDuration)).ToUnixTimeSeconds().ToString();
            return Task.CompletedTask;
        });
        
        await _next(context);
    }
    
    private static bool IsAuthEndpoint(string endpoint)
    {
        return endpoint.Contains("/api/auth", StringComparison.OrdinalIgnoreCase);
    }
    
    public static void CleanupExpiredEntries()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _requestCounts
            .Where(kvp => now - kvp.Value.WindowStart > WindowDuration)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in expiredKeys)
        {
            _requestCounts.TryRemove(key, out _);
        }
    }
}
