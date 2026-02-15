using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.Interfaces.Repositories;

public interface IAdminStatisticsRepository
{
    Task<int> CountPatientsAsync();
    Task<int> CountDoctorsAsync();
    Task<int> CountClinicsAsync();
    Task<int> CountAppointmentsAsync();
    Task<int> CountAppointmentsByStatusAsync(AppointmentStatus status);
    Task<int> CountReviewsAsync();
    Task<decimal> AverageReviewRatingAsync();
}
