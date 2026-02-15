namespace BookingSystem.Domain.Exceptions;

public class BusinessRuleException : Exception
{
    public string Rule { get; }
    
    public BusinessRuleException(string message) : base(message)
    {
        Rule = message;
    }
    
    public BusinessRuleException(string message, Exception innerException) 
        : base(message, innerException)
    {
        Rule = message;
    }
}
