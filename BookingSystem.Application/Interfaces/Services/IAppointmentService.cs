using BookingSystem.Application.DTOs.Appointment;
using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.Enums;

namespace BookingSystem.Application.Interfaces.Services;

public interface IAppointmentService
{
    Task<AppointmentDto> BookAppointmentAsync(string userId, CreateAppointmentRequestDto request);
    Task<AppointmentDetailDto> GetAppointmentByIdAsync(int id, string userId);
    Task<PaginatedResult<AppointmentDto>> GetMyAppointmentsAsync(
        string userId,
        AppointmentStatus? status = null,
        bool? upcoming = null,
        bool? past = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<PaginatedResult<AppointmentDto>> GetDoctorAppointmentsAsync(
        int doctorId,
        string userId,
        AppointmentStatus? status = null,
        bool? upcoming = null,
        bool? past = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<AppointmentDto> UpdateAppointmentStatusAsync(int id, string userId, UpdateAppointmentStatusRequestDto request);
    Task<AppointmentDto> ConfirmAppointmentAsync(int id, string userId);
    Task<AppointmentDto> CompleteAppointmentAsync(int id, string userId, CompleteAppointmentRequestDto request);
    Task<AppointmentDto> CancelAppointmentAsync(int id, string userId, CancelAppointmentRequestDto request);
    Task<AppointmentDto> RescheduleAppointmentAsync(int id, string userId, RescheduleAppointmentRequestDto request);
    Task<PaginatedResult<AppointmentDto>> GetAllAppointmentsAsync(
        AppointmentStatus? status = null,
        int? doctorId = null,
        int? patientId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 10);
}
