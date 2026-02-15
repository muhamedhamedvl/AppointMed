namespace BookingSystem.Application.Exceptions;

public class NotFoundException : Exception
{
    public string? EntityName { get; }
    public object? Key { get; }

    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key '{key}' was not found.")
    {
        EntityName = entityName;
        Key = key;
    }

    public NotFoundException(string message) : base(message)
    {
    }
}
