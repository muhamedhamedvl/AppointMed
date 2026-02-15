using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Repositories;

public class ClinicRepository : IClinicRepository
{
    private readonly ApplicationDbContext _context;

    public ClinicRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Clinic?> GetByIdAsync(int id)
    {
        return await _context.Clinics.FindAsync(id);
    }

    public async Task<Clinic?> GetByIdWithDoctorsAsync(int id)
    {
        return await _context.Clinics
            .Include(c => c.Doctors)
            .ThenInclude(d => d.AvailableTimeSlots)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Clinic> AddAsync(Clinic clinic)
    {
        await _context.Clinics.AddAsync(clinic);
        return clinic;
    }

    public Task UpdateAsync(Clinic clinic)
    {
        _context.Clinics.Update(clinic);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Clinic clinic)
    {
        _context.Clinics.Remove(clinic);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Clinic>> GetAllAsync(string? city, int skip, int take)
    {
        var query = _context.Clinics.AsQueryable();
        if (!string.IsNullOrEmpty(city))
            query = query.Where(c => c.City.Contains(city));
        return await query
            .OrderBy(c => c.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountAllAsync(string? city)
    {
        var query = _context.Clinics.AsQueryable();
        if (!string.IsNullOrEmpty(city))
            query = query.Where(c => c.City.Contains(city));
        return await query.CountAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
