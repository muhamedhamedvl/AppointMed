using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Application.DTOs.Review;

public class CreateReviewRequestDto
{
    [Required]
    public int AppointmentId { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}
