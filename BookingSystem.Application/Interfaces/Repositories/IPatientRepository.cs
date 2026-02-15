using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces.Repositories;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(int id);
    Task<Patient?> GetByUserIdAsync(string userId);
    Task<Patient> AddAsync(Patient patient);
    Task UpdateAsync(Patient patient);
    Task<int> SaveChangesAsync();
}
