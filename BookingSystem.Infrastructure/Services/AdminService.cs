using BookingSystem.Application.DTOs.Admin;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;

    public AdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StatisticsDto> GetStatisticsAsync()
    {
        var totalPatients = await _context.Patients.CountAsync();
        var totalDoctors = await _context.Doctors.CountAsync();
        var totalClinics = await _context.Clinics.CountAsync();
        var totalAppointments = await _context.Appointments.CountAsync();
        
        var pendingAppointments = await _context.Appointments
            .CountAsync(a => a.Status == AppointmentStatus.Pending);
        
        var confirmedAppointments = await _context.Appointments
            .CountAsync(a => a.Status == AppointmentStatus.Confirmed);
        
        var completedAppointments = await _context.Appointments
            .CountAsync(a => a.Status == AppointmentStatus.Completed);
        
        var canceledAppointments = await _context.Appointments
            .CountAsync(a => a.Status == AppointmentStatus.Canceled);
        
        var totalReviews = await _context.Reviews.CountAsync();
        
        var averageRating = await _context.Reviews.AnyAsync()
            ? (decimal)await _context.Reviews.AverageAsync(r => r.Rating)
            : 0m;

        return new StatisticsDto
        {
            TotalPatients = totalPatients,
            TotalDoctors = totalDoctors,
            TotalClinics = totalClinics,
            TotalAppointments = totalAppointments,
            PendingAppointments = pendingAppointments,
            ConfirmedAppointments = confirmedAppointments,
            CompletedAppointments = completedAppointments,
            CanceledAppointments = canceledAppointments,
            TotalReviews = totalReviews,
            AverageRating = averageRating
        };
    }
}
