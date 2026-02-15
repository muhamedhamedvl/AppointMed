namespace BookingSystem.Domain.Exceptions;

public class UnauthorizedException : Exception
{
    public string Resource { get; }
    
    public UnauthorizedException(string message) : base(message)
    {
        Resource = string.Empty;
    }
    
    public UnauthorizedException(string message, string resource) : base(message)
    {
        Resource = resource;
    }
}
