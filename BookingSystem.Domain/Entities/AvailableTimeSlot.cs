using BookingSystem.Domain.Base;

namespace BookingSystem.Domain.Entities;

public class AvailableTimeSlot : BaseEntity
{
    public int DoctorId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsBooked { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Doctor Doctor { get; set; } = null!;
    public virtual Appointment? Appointment { get; set; }
}
