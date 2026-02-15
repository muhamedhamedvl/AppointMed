using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Data;
using BookingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly ApplicationDbContext _context;

    public DoctorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Doctor?> GetByIdAsync(int id)
    {
        return await _context.Doctors.FindAsync(id);
    }

    public async Task<Doctor?> GetByIdWithTimeSlotsAsync(int id)
    {
        return await _context.Doctors
            .Include(d => d.AvailableTimeSlots)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Doctor?> GetByIdWithClinicAndTimeSlotsAsync(int id)
    {
        return await _context.Doctors
            .Include(d => d.Clinic)
            .Include(d => d.AvailableTimeSlots)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Doctor?> GetByUserIdAsync(string userId)
    {
        return await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
    }

    public async Task<Doctor> AddAsync(Doctor doctor)
    {
        await _context.Doctors.AddAsync(doctor);
        return doctor;
    }

    public Task UpdateAsync(Doctor doctor)
    {
        _context.Doctors.Update(doctor);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Doctor>> SearchAsync(
        string? specialization, int? clinicId, string? city,
        decimal? minFee, decimal? maxFee, decimal? minRating,
        DateOnly? date, int skip, int take)
    {
        var query = _context.Doctors
            .Include(d => d.Clinic)
            .Include(d => d.AvailableTimeSlots)
            .Where(d => d.IsApproved)
            .AsQueryable();

        if (!string.IsNullOrEmpty(specialization))
            query = query.Where(d => d.Specialization.Contains(specialization));
        if (clinicId.HasValue)
            query = query.Where(d => d.ClinicId == clinicId);
        if (!string.IsNullOrEmpty(city))
            query = query.Where(d => d.Clinic != null && d.Clinic.City.Contains(city));
        if (minFee.HasValue)
            query = query.Where(d => d.ConsultationFee >= minFee.Value);
        if (maxFee.HasValue)
            query = query.Where(d => d.ConsultationFee <= maxFee.Value);
        if (minRating.HasValue)
            query = query.Where(d => d.AverageRating >= minRating.Value);
        if (date.HasValue)
            query = query.Where(d => d.AvailableTimeSlots.Any(s => s.Date == date.Value && !s.IsBooked));

        return await query
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountSearchAsync(
        string? specialization, int? clinicId, string? city,
        decimal? minFee, decimal? maxFee, decimal? minRating,
        DateOnly? date)
    {
        var query = _context.Doctors
            .Include(d => d.Clinic)
            .Where(d => d.IsApproved)
            .AsQueryable();

        if (!string.IsNullOrEmpty(specialization))
            query = query.Where(d => d.Specialization.Contains(specialization));
        if (clinicId.HasValue)
            query = query.Where(d => d.ClinicId == clinicId);
        if (!string.IsNullOrEmpty(city))
            query = query.Where(d => d.Clinic != null && d.Clinic.City.Contains(city));
        if (minFee.HasValue)
            query = query.Where(d => d.ConsultationFee >= minFee.Value);
        if (maxFee.HasValue)
            query = query.Where(d => d.ConsultationFee <= maxFee.Value);
        if (minRating.HasValue)
            query = query.Where(d => d.AverageRating >= minRating.Value);
        if (date.HasValue)
            query = query.Where(d => d.AvailableTimeSlots.Any(s => s.Date == date.Value && !s.IsBooked));

        return await query.CountAsync();
    }

    public async Task<IEnumerable<Doctor>> GetPendingAsync(int skip, int take)
    {
        return await _context.Doctors
            .Include(d => d.AvailableTimeSlots)
            .Where(d => !d.IsApproved)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountPendingAsync()
    {
        return await _context.Doctors.CountAsync(d => !d.IsApproved);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await DbExceptionTranslator.SaveChangesWithTranslationAsync(() => _context.SaveChangesAsync());
    }
}
