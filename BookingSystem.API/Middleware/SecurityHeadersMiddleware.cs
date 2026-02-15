namespace BookingSystem.API.Middleware;

/// <summary>
/// Adds security headers to all HTTP responses to protect against common web vulnerabilities.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent MIME sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        
        // Prevent clickjacking
        context.Response.Headers["X-Frame-Options"] = "DENY";
        
        // Enable XSS filter
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        
        // Control referrer information
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        
        // Content Security Policy - restrictive for APIs
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        
        // Prevent browsers from caching sensitive data
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, proxy-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
        
        // Remove server header to avoid revealing server information
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        
        // Strict Transport Security (HSTS) - enforce HTTPS
        // Only add if already on HTTPS to avoid errors
        if (context.Request.IsHttps)
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }
        
        await _next(context);
    }
}
