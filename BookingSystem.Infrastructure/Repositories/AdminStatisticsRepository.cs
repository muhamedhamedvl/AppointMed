using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Repositories;

public class AdminStatisticsRepository : IAdminStatisticsRepository
{
    private readonly ApplicationDbContext _context;

    public AdminStatisticsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> CountPatientsAsync()
    {
        return await _context.Patients.CountAsync();
    }

    public async Task<int> CountDoctorsAsync()
    {
        return await _context.Doctors.CountAsync();
    }

    public async Task<int> CountClinicsAsync()
    {
        return await _context.Clinics.CountAsync();
    }

    public async Task<int> CountAppointmentsAsync()
    {
        return await _context.Appointments.CountAsync();
    }

    public async Task<int> CountAppointmentsByStatusAsync(AppointmentStatus status)
    {
        return await _context.Appointments.CountAsync(a => a.Status == status);
    }

    public async Task<int> CountReviewsAsync()
    {
        return await _context.Reviews.CountAsync();
    }

    public async Task<decimal> AverageReviewRatingAsync()
    {
        return await _context.Reviews.AnyAsync()
            ? (decimal)await _context.Reviews.AverageAsync(r => r.Rating)
            : 0m;
    }
}
