namespace BookingSystem.Application.DTOs.TimeSlot;

public class AvailableTimeSlotDto
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsBooked { get; set; }
    public DateTime CreatedAt { get; set; }
}
