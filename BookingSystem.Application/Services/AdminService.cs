using BookingSystem.Application.DTOs.Admin;
using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.Services;

public class AdminService : IAdminService
{
    private readonly IAdminStatisticsRepository _statisticsRepository;

    public AdminService(IAdminStatisticsRepository statisticsRepository)
    {
        _statisticsRepository = statisticsRepository;
    }

    public async Task<StatisticsDto> GetStatisticsAsync()
    {
        var totalPatients = await _statisticsRepository.CountPatientsAsync();
        var totalDoctors = await _statisticsRepository.CountDoctorsAsync();
        var totalClinics = await _statisticsRepository.CountClinicsAsync();
        var totalAppointments = await _statisticsRepository.CountAppointmentsAsync();
        var pendingAppointments = await _statisticsRepository.CountAppointmentsByStatusAsync(AppointmentStatus.Pending);
        var confirmedAppointments = await _statisticsRepository.CountAppointmentsByStatusAsync(AppointmentStatus.Confirmed);
        var completedAppointments = await _statisticsRepository.CountAppointmentsByStatusAsync(AppointmentStatus.Completed);
        var canceledAppointments = await _statisticsRepository.CountAppointmentsByStatusAsync(AppointmentStatus.Canceled);
        var totalReviews = await _statisticsRepository.CountReviewsAsync();
        var averageRating = await _statisticsRepository.AverageReviewRatingAsync();

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
