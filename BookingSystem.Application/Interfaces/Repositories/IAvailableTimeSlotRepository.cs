using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Interfaces.Repositories;

public interface IAvailableTimeSlotRepository
{
    Task<AvailableTimeSlot?> GetByIdAsync(int id);
    Task<AvailableTimeSlot?> GetByIdWithDoctorAsync(int id);
    Task<IEnumerable<AvailableTimeSlot>> GetByDoctorAndDateRangeAsync(int doctorId, DateOnly startDate, DateOnly endDate);
    Task<AvailableTimeSlot> AddAsync(AvailableTimeSlot slot);
    Task AddRangeAsync(IEnumerable<AvailableTimeSlot> slots);
    Task UpdateAsync(AvailableTimeSlot slot);
    Task DeleteAsync(AvailableTimeSlot slot);
    Task<bool> HasOverlappingSlotAsync(int doctorId, DateOnly date, TimeOnly startTime, TimeOnly endTime, int? excludeSlotId = null);
    Task<int> SaveChangesAsync();
}
