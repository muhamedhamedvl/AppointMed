using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces.Repositories;

public interface IClinicRepository
{
    Task<Clinic?> GetByIdAsync(int id);
    Task<Clinic?> GetByIdWithDoctorsAsync(int id);
    Task<Clinic> AddAsync(Clinic clinic);
    Task UpdateAsync(Clinic clinic);
    Task DeleteAsync(Clinic clinic);
    Task<IEnumerable<Clinic>> GetAllAsync(string? city, int skip, int take);
    Task<int> CountAllAsync(string? city);
    Task<int> SaveChangesAsync();
}
