using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.DTOs.Review;

namespace BookingSystem.Application.Interfaces.Services;

public interface IReviewService
{
    Task<ReviewDto> CreateReviewAsync(string userId, CreateReviewRequestDto request);
    Task<PaginatedResult<ReviewDto>> GetDoctorReviewsAsync(int doctorId, int pageNumber = 1, int pageSize = 10);
    Task<ReviewDto?> GetAppointmentReviewAsync(int appointmentId);
}
