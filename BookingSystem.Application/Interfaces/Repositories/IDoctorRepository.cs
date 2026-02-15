using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces.Repositories;

public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(int id);
    Task<Doctor?> GetByIdWithTimeSlotsAsync(int id);
    Task<Doctor?> GetByIdWithClinicAndTimeSlotsAsync(int id);
    Task<Doctor?> GetByUserIdAsync(string userId);
    Task<Doctor> AddAsync(Doctor doctor);
    Task UpdateAsync(Doctor doctor);
    Task<IEnumerable<Doctor>> SearchAsync(
        string? specialization, int? clinicId, string? city,
        decimal? minFee, decimal? maxFee, decimal? minRating,
        DateOnly? date, int skip, int take);
    Task<int> CountSearchAsync(
        string? specialization, int? clinicId, string? city,
        decimal? minFee, decimal? maxFee, decimal? minRating,
        DateOnly? date);
    Task<IEnumerable<Doctor>> GetPendingAsync(int skip, int take);
    Task<int> CountPendingAsync();
    Task<int> SaveChangesAsync();
}
