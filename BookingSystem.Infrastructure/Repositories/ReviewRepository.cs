using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Data;
using BookingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _context;

    public ReviewRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> GetByIdAsync(int id)
    {
        return await _context.Reviews.FindAsync(id);
    }

    public async Task<Review?> GetByAppointmentIdAsync(int appointmentId)
    {
        return await _context.Reviews
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);
    }

    public async Task<Review> AddAsync(Review review)
    {
        await _context.Reviews.AddAsync(review);
        return review;
    }

    public Task UpdateAsync(Review review)
    {
        _context.Reviews.Update(review);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Review>> GetByDoctorIdAsync(int doctorId, int skip, int take)
    {
        return await _context.Reviews
            .Include(r => r.Appointment)
            .Where(r => r.Appointment.DoctorId == doctorId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountByDoctorIdAsync(int doctorId)
    {
        return await _context.Reviews
            .Include(r => r.Appointment)
            .Where(r => r.Appointment.DoctorId == doctorId)
            .CountAsync();
    }

    public async Task<IEnumerable<Review>> GetReviewsForDoctorAsync(int doctorId)
    {
        return await _context.Reviews
            .Include(r => r.Appointment)
            .Where(r => r.Appointment.DoctorId == doctorId)
            .ToListAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await DbExceptionTranslator.SaveChangesWithTranslationAsync(() => _context.SaveChangesAsync());
    }
}
