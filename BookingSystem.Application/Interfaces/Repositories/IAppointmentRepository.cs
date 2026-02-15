using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.Interfaces.Repositories;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(int id);
    Task<Appointment?> GetByIdWithPatientDoctorAsync(int id);
    Task<Appointment?> GetByIdWithPatientDoctorClinicAsync(int id);
    Task<Appointment?> GetByIdWithPatientDoctorSlotAsync(int id);
    Task<Appointment?> GetByIdWithReviewAsync(int id);
    Task<Appointment> AddAsync(Appointment appointment);
    Task UpdateAsync(Appointment appointment);
    Task<IEnumerable<Appointment>> GetByPatientIdAsync(int patientId, AppointmentStatus? status, bool? upcoming, bool? past, int skip, int take);
    Task<int> CountByPatientIdAsync(int patientId, AppointmentStatus? status, bool? upcoming, bool? past);
    Task<IEnumerable<Appointment>> GetByDoctorIdAsync(int doctorId, AppointmentStatus? status, bool? upcoming, bool? past, int skip, int take);
    Task<int> CountByDoctorIdAsync(int doctorId, AppointmentStatus? status, bool? upcoming, bool? past);
    Task<IEnumerable<Appointment>> GetAllAsync(AppointmentStatus? status, int? doctorId, int? patientId, DateOnly? startDate, DateOnly? endDate, int skip, int take);
    Task<int> CountAllAsync(AppointmentStatus? status, int? doctorId, int? patientId, DateOnly? startDate, DateOnly? endDate);
    Task<int> SaveChangesAsync();
}
