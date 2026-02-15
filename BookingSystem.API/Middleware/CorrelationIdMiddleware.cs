namespace BookingSystem.API.Middleware;
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";
    
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        // Check if correlation ID is provided in request header
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        
        // If not provided, generate a new one
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }
        
        // Add to response header
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        
        // Add to HttpContext items for access throughout the request pipeline
        context.Items["CorrelationId"] = correlationId;
        
        // Log the correlation ID
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }
}
