using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Data;
using BookingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Repositories;

public class AvailableTimeSlotRepository : IAvailableTimeSlotRepository
{
    private readonly ApplicationDbContext _context;

    public AvailableTimeSlotRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AvailableTimeSlot?> GetByIdAsync(int id)
    {
        return await _context.AvailableTimeSlots.FindAsync(id);
    }

    public async Task<AvailableTimeSlot?> GetByIdWithDoctorAsync(int id)
    {
        return await _context.AvailableTimeSlots
            .Include(t => t.Doctor)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<AvailableTimeSlot>> GetByDoctorAndDateRangeAsync(int doctorId, DateOnly startDate, DateOnly endDate)
    {
        return await _context.AvailableTimeSlots
            .Where(s => s.DoctorId == doctorId && s.Date >= startDate && s.Date <= endDate)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<AvailableTimeSlot> AddAsync(AvailableTimeSlot slot)
    {
        await _context.AvailableTimeSlots.AddAsync(slot);
        return slot;
    }

    public async Task AddRangeAsync(IEnumerable<AvailableTimeSlot> slots)
    {
        await _context.AvailableTimeSlots.AddRangeAsync(slots);
    }

    public Task UpdateAsync(AvailableTimeSlot slot)
    {
        _context.AvailableTimeSlots.Update(slot);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(AvailableTimeSlot slot)
    {
        _context.AvailableTimeSlots.Remove(slot);
        return Task.CompletedTask;
    }

    public async Task<bool> HasOverlappingSlotAsync(int doctorId, DateOnly date, TimeOnly startTime, TimeOnly endTime, int? excludeSlotId = null)
    {
        var query = _context.AvailableTimeSlots
            .Where(s => s.DoctorId == doctorId
                && s.Date == date
                && !s.IsDeleted
                && !s.IsBooked);

        if (excludeSlotId.HasValue)
            query = query.Where(s => s.Id != excludeSlotId.Value);

        return await query.AnyAsync(s =>
            (startTime >= s.StartTime && startTime < s.EndTime) ||
            (endTime > s.StartTime && endTime <= s.EndTime) ||
            (startTime <= s.StartTime && endTime >= s.EndTime) ||
            (s.StartTime <= startTime && s.EndTime >= endTime));
    }

    public async Task<int> SaveChangesAsync()
    {
        return await DbExceptionTranslator.SaveChangesWithTranslationAsync(() => _context.SaveChangesAsync());
    }
}
