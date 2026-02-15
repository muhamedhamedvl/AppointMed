using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.DTOs.Doctor;
using BookingSystem.Application.DTOs.TimeSlot;

namespace BookingSystem.Application.Interfaces.Services;

public interface IDoctorService
{
    Task<DoctorProfileDto> OnboardDoctorAsync(string userId, OnboardDoctorRequestDto request);
    Task<DoctorProfileDto> GetDoctorByIdAsync(int id);
    Task<DoctorDetailDto> GetDoctorDetailsAsync(int id);
    Task<DoctorProfileDto> UpdateDoctorAsync(int id, string userId, UpdateDoctorRequestDto request);
    Task<PaginatedResult<DoctorProfileDto>> SearchDoctorsAsync(
        string? specialization = null,
        string? name = null,
        int? clinicId = null,
        string? city = null,
        decimal? minFee = null,
        decimal? maxFee = null,
        decimal? minRating = null,
        DateOnly? date = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<List<AvailableTimeSlotDto>> GetDoctorAvailabilityAsync(int doctorId, DateOnly startDate, DateOnly endDate);
    Task<List<AvailableTimeSlotDto>> AddAvailabilityAsync(int doctorId, string userId, AddAvailabilityRequestDto request);
    Task DeleteTimeSlotAsync(int doctorId, int slotId, string userId);
    Task<DoctorProfileDto> ApproveDoctorAsync(int id);
    Task<PaginatedResult<DoctorProfileDto>> GetPendingDoctorsAsync(int pageNumber = 1, int pageSize = 10);
}
