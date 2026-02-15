using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.TimeSlot;

public class TimeSlotInputDto
{
    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }
}

public class AddAvailabilityRequestDto
{
    [Required]
    [MinLength(1)]
    public List<TimeSlotInputDto> Slots { get; set; } = new();
}
