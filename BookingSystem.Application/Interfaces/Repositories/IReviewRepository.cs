using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces.Repositories;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(int id);
    Task<Review?> GetByAppointmentIdAsync(int appointmentId);
    Task<Review> AddAsync(Review review);
    Task UpdateAsync(Review review);
    Task<IEnumerable<Review>> GetByDoctorIdAsync(int doctorId, int skip, int take);
    Task<int> CountByDoctorIdAsync(int doctorId);
    Task<IEnumerable<Review>> GetReviewsForDoctorAsync(int doctorId);
    Task<int> SaveChangesAsync();
}
