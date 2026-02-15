using BookingSystem.Domain.Base;

namespace BookingSystem.Domain.Entities;

public class Review : BaseEntity
{
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Appointment Appointment { get; set; } = null!;
}
