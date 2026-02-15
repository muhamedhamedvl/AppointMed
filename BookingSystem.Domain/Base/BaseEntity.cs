namespace BookingSystem.Domain.Base;

/// <summary>
/// Base entity with audit fields and soft delete.
/// All DateTimes are stored and interpreted as UTC.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>Alias for ModifiedAt; all audit timestamps are UTC.</summary>
    public DateTime? UpdatedAt
    {
        get => ModifiedAt;
        set => ModifiedAt = value;
    }
}
