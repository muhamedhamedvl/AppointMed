using BookingSystem.Application.DTOs.Appointment;
using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Exceptions;
using BookingSystem.Domain.Helpers;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IAvailableTimeSlotRepository _timeSlotRepository;
    private readonly IUserInfoProvider _userInfoProvider;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        IClinicRepository clinicRepository,
        IAvailableTimeSlotRepository timeSlotRepository,
        IUserInfoProvider userInfoProvider,
        ITransactionManager transactionManager,
        ILogger<AppointmentService> logger)
    {
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _clinicRepository = clinicRepository;
        _timeSlotRepository = timeSlotRepository;
        _userInfoProvider = userInfoProvider;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task<AppointmentDto> BookAppointmentAsync(string userId, CreateAppointmentRequestDto request)
    {
        if (!await _userInfoProvider.IsEmailConfirmedAsync(userId))
        {
            _logger.LogWarning("Booking attempt failed: Email not verified. UserId: {UserId}", userId);
            throw new Exception("Email must be verified to book appointments");
        }

        var patient = await _patientRepository.GetByUserIdAsync(userId);
        if (patient == null)
        {
            _logger.LogWarning("Booking attempt failed: No patient profile. UserId: {UserId}", userId);
            throw new Exception("Please create a patient profile first");
        }

        await using var transaction = await _transactionManager.BeginTransactionAsync();
        try
        {
            var timeSlot = await _timeSlotRepository.GetByIdWithDoctorAsync(request.TimeSlotId);
            if (timeSlot == null || timeSlot.IsBooked)
            {
                _logger.LogWarning("Booking failed: Time slot unavailable. TimeSlotId: {TimeSlotId}", request.TimeSlotId);
                throw new Exception("Time slot not available or already booked");
            }
            if (timeSlot.DoctorId != request.DoctorId)
            {
                _logger.LogWarning("Booking failed: Time slot doctor mismatch. TimeSlotId: {TimeSlotId}", request.TimeSlotId);
                throw new Exception("Time slot does not belong to the specified doctor");
            }

            timeSlot.IsBooked = true;
            await _timeSlotRepository.UpdateAsync(timeSlot);

            var appointment = new Appointment
            {
                PatientId = patient.Id,
                DoctorId = request.DoctorId,
                ClinicId = timeSlot.Doctor!.ClinicId,
                SlotId = timeSlot.Id,
                AppointmentDate = timeSlot.Date,
                StartTime = timeSlot.StartTime,
                EndTime = timeSlot.EndTime,
                Status = AppointmentStatus.Pending,
                ReasonForVisit = request.ReasonForVisit
            };

            appointment = await _appointmentRepository.AddAsync(appointment);
            await _appointmentRepository.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Appointment booked successfully. AppointmentId: {AppointmentId}", appointment.Id);
            return await MapToAppointmentDto(appointment);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AppointmentDetailDto> GetAppointmentByIdAsync(int id, string userId)
    {
        var appointment = await _appointmentRepository.GetByIdWithPatientDoctorClinicAsync(id);
        if (appointment == null) throw new Exception("Appointment not found");

        var patient = await _patientRepository.GetByUserIdAsync(userId);
        var doctorUserId = appointment.Doctor?.UserId;
        var isAdmin = await _userInfoProvider.IsInRoleAsync(userId, "Admin");

        var canAccess = (patient != null && appointment.PatientId == patient.Id) ||
                       (doctorUserId == userId) ||
                       isAdmin;

        if (!canAccess)
            throw new Exception("Unauthorized access to appointment");

        return await MapToAppointmentDetailDto(appointment);
    }

    public async Task<PaginatedResult<AppointmentDto>> GetMyAppointmentsAsync(
        string userId, AppointmentStatus? status, bool? upcoming, bool? past, int pageNumber, int pageSize)
    {
        var patient = await _patientRepository.GetByUserIdAsync(userId);
        var doctor = await _doctorRepository.GetByUserIdAsync(userId);

        IEnumerable<Appointment> appointments;
        int totalCount;

        if (patient != null)
        {
            var skip = (pageNumber - 1) * pageSize;
            appointments = await _appointmentRepository.GetByPatientIdAsync(
                patient.Id, status, upcoming, past, skip, pageSize);
            totalCount = await _appointmentRepository.CountByPatientIdAsync(
                patient.Id, status, upcoming, past);
        }
        else if (doctor != null)
        {
            var skip = (pageNumber - 1) * pageSize;
            appointments = await _appointmentRepository.GetByDoctorIdAsync(
                doctor.Id, status, upcoming, past, skip, pageSize);
            totalCount = await _appointmentRepository.CountByDoctorIdAsync(
                doctor.Id, status, upcoming, past);
        }
        else
        {
            throw new BusinessRuleException("No patient or doctor profile found");
        }

        var userIds = appointments.SelectMany(a => new[] { a.Patient!.UserId, a.Doctor!.UserId }).Distinct().ToList();
        var users = await _userInfoProvider.GetByIdsAsync(userIds);
        var dtos = appointments.Select(apt => MapToAppointmentDtoFromEntities(apt, users)).ToList();

        return new PaginatedResult<AppointmentDto>
        {
            Data = dtos,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PaginatedResult<AppointmentDto>> GetDoctorAppointmentsAsync(
        int doctorId, string userId, AppointmentStatus? status, bool? upcoming, bool? past, int pageNumber, int pageSize)
    {
        var doctor = await _doctorRepository.GetByIdAsync(doctorId);
        if (doctor == null || doctor.UserId != userId)
            throw new Exception("Unauthorized or doctor not found");

        var skip = (pageNumber - 1) * pageSize;
        var appointments = await _appointmentRepository.GetByDoctorIdAsync(
            doctorId, status, upcoming, past, skip, pageSize);
        var totalCount = await _appointmentRepository.CountByDoctorIdAsync(
            doctorId, status, upcoming, past);

        var userIds = appointments.SelectMany(a => new[] { a.Patient!.UserId, a.Doctor!.UserId }).Distinct().ToList();
        var users = await _userInfoProvider.GetByIdsAsync(userIds);
        var dtos = appointments.Select(apt => MapToAppointmentDtoFromEntities(apt, users)).ToList();

        return new PaginatedResult<AppointmentDto>
        {
            Data = dtos,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<AppointmentDto> UpdateAppointmentStatusAsync(int id, string userId, UpdateAppointmentStatusRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            throw new BusinessRuleException("Status is required.");

        var statusStr = request.Status.Trim();
        if (!Enum.TryParse<AppointmentStatus>(statusStr, ignoreCase: true, out var newStatus))
        {
            if (string.Equals(statusStr, "Cancelled", StringComparison.OrdinalIgnoreCase))
                newStatus = AppointmentStatus.Canceled;
            else
                throw new BusinessRuleException($"Invalid status '{request.Status}'. Allowed: Pending, Confirmed, Completed, Cancelled, NoShow.");
        }

        var appointment = await _appointmentRepository.GetByIdWithPatientDoctorAsync(id);
        if (appointment == null) throw new InvalidOperationException("Appointment not found");

        try
        {
            AppointmentStatusTransitionValidator.ValidateTransition(appointment.Status, newStatus);
        }
        catch (InvalidStatusTransitionException)
        {
            throw new InvalidStatusTransitionException($"Invalid transition from {appointment.Status} to {newStatus}");
        }

        switch (newStatus)
        {
            case AppointmentStatus.Confirmed:
                if (appointment.Doctor!.UserId != userId)
                    throw new UnauthorizedAccessException("Only the assigned doctor can confirm appointments.");
                appointment.Status = AppointmentStatus.Confirmed;
                appointment.ModifiedAt = DateTime.UtcNow;
                break;

            case AppointmentStatus.Completed:
                if (appointment.Doctor!.UserId != userId)
                    throw new UnauthorizedAccessException("Only the assigned doctor can complete appointments.");
                appointment.Status = AppointmentStatus.Completed;
                appointment.Notes = request.Notes;
                appointment.ModifiedAt = DateTime.UtcNow;
                break;

            case AppointmentStatus.NoShow:
                if (appointment.Doctor!.UserId != userId)
                    throw new UnauthorizedAccessException("Only the assigned doctor can mark appointment as NoShow.");
                appointment.Status = AppointmentStatus.NoShow;
                appointment.ModifiedAt = DateTime.UtcNow;
                break;

            case AppointmentStatus.Canceled:
                var canCancel = appointment.Patient!.UserId == userId || appointment.Doctor!.UserId == userId;
                if (!canCancel) canCancel = await _userInfoProvider.IsInRoleAsync(userId, "Admin");
                if (!canCancel)
                    throw new UnauthorizedAccessException("Unauthorized to cancel this appointment.");
                if (appointment.AppointmentDate < DateOnly.FromDateTime(DateTime.UtcNow))
                    throw new BusinessRuleException("Cannot cancel past appointments.");
                appointment.Status = AppointmentStatus.Canceled;
                appointment.CancellationReason = request.CancellationReason;
                appointment.CancelledAt = DateTime.UtcNow;
                appointment.ModifiedAt = DateTime.UtcNow;
                var slotToRelease = await _timeSlotRepository.GetByIdAsync(appointment.SlotId);
                if (slotToRelease != null)
                {
                    slotToRelease.IsBooked = false;
                    await _timeSlotRepository.UpdateAsync(slotToRelease);
                }
                break;
        }

        await _appointmentRepository.UpdateAsync(appointment);
        await _appointmentRepository.SaveChangesAsync();
        return await MapToAppointmentDto(appointment);
    }

    public async Task<AppointmentDto> ConfirmAppointmentAsync(int id, string userId)
    {
        var appointment = await _appointmentRepository.GetByIdWithPatientDoctorAsync(id);
        if (appointment == null) throw new Exception("Appointment not found");
        if (appointment.Doctor!.UserId != userId)
            throw new Exception("Only the assigned doctor can confirm appointments");

        try
        {
            AppointmentStatusTransitionValidator.ValidateTransition(appointment.Status, AppointmentStatus.Confirmed);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid status transition: AppointmentId: {AppointmentId}", id);
            throw new InvalidStatusTransitionException(ex.Message);
        }

        appointment.Status = AppointmentStatus.Confirmed;
        appointment.ModifiedAt = DateTime.UtcNow;
        await _appointmentRepository.UpdateAsync(appointment);
        await _appointmentRepository.SaveChangesAsync();

        _logger.LogInformation("Appointment confirmed. AppointmentId: {AppointmentId}", id);
        return await MapToAppointmentDto(appointment);
    }

    public async Task<AppointmentDto> CompleteAppointmentAsync(int id, string userId, CompleteAppointmentRequestDto request)
    {
        var appointment = await _appointmentRepository.GetByIdWithPatientDoctorAsync(id);
        if (appointment == null) throw new Exception("Appointment not found");
        if (appointment.Doctor!.UserId != userId)
            throw new Exception("Only the assigned doctor can complete appointments");

        try
        {
            AppointmentStatusTransitionValidator.ValidateTransition(appointment.Status, AppointmentStatus.Completed);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid status transition while completing: AppointmentId: {AppointmentId}", id);
            throw new InvalidStatusTransitionException(ex.Message);
        }

        appointment.Status = AppointmentStatus.Completed;
        appointment.Notes = request.Notes;
        appointment.ModifiedAt = DateTime.UtcNow;
        await _appointmentRepository.UpdateAsync(appointment);
        await _appointmentRepository.SaveChangesAsync();

        _logger.LogInformation("Appointment completed. AppointmentId: {AppointmentId}", id);
        return await MapToAppointmentDto(appointment);
    }

    public async Task<AppointmentDto> CancelAppointmentAsync(int id, string userId, CancelAppointmentRequestDto request)
    {
        var appointment = await _appointmentRepository.GetByIdWithPatientDoctorAsync(id);
        if (appointment == null) throw new Exception("Appointment not found");

        var canCancel = appointment.Patient!.UserId == userId || appointment.Doctor!.UserId == userId;
        if (!canCancel) canCancel = await _userInfoProvider.IsInRoleAsync(userId, "Admin");
        if (!canCancel) throw new Exception("Unauthorized to cancel this appointment");
        if (appointment.AppointmentDate < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new Exception("Cannot cancel past appointments");

        try
        {
            AppointmentStatusTransitionValidator.ValidateTransition(appointment.Status, AppointmentStatus.Canceled);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invalid status transition while canceling: AppointmentId: {AppointmentId}", id);
            throw new InvalidStatusTransitionException(ex.Message);
        }

        appointment.Status = AppointmentStatus.Canceled;
        appointment.CancellationReason = request.CancellationReason;
        appointment.CancelledAt = DateTime.UtcNow;
        appointment.ModifiedAt = DateTime.UtcNow;

        var slotToRelease = await _timeSlotRepository.GetByIdAsync(appointment.SlotId);
        if (slotToRelease != null)
        {
            slotToRelease.IsBooked = false;
            await _timeSlotRepository.UpdateAsync(slotToRelease);
        }

        await _appointmentRepository.UpdateAsync(appointment);
        await _appointmentRepository.SaveChangesAsync();
        _logger.LogInformation("Appointment {AppointmentId} canceled. Reason: {Reason}", id, request.CancellationReason ?? "Not provided");
        return await MapToAppointmentDto(appointment);
    }

    public async Task<AppointmentDto> RescheduleAppointmentAsync(int id, string userId, RescheduleAppointmentRequestDto request)
    {
        var appointment = await _appointmentRepository.GetByIdWithPatientDoctorClinicAsync(id);
        if (appointment == null) throw new Exception("Appointment not found");
        if (appointment.Patient!.UserId != userId)
            throw new Exception("Only the patient can reschedule appointments");
        if (appointment.Status == AppointmentStatus.Canceled || appointment.Status == AppointmentStatus.Completed)
            throw new Exception("Cannot reschedule canceled or completed appointments");

        await using var transaction = await _transactionManager.BeginTransactionAsync();
        try
        {
            var newTimeSlot = await _timeSlotRepository.GetByIdWithDoctorAsync(request.NewTimeSlotId);
            if (newTimeSlot == null || newTimeSlot.IsBooked)
                throw new BusinessRuleException("The selected time slot is not available or has already been booked. Please choose another slot.");
            if (newTimeSlot.DoctorId != appointment.DoctorId)
                throw new Exception("New time slot must belong to the same doctor");

            var oldSlot = await _timeSlotRepository.GetByIdAsync(appointment.SlotId);
            if (oldSlot != null)
            {
                oldSlot.IsBooked = false;
                await _timeSlotRepository.UpdateAsync(oldSlot);
            }

            newTimeSlot.IsBooked = true;
            await _timeSlotRepository.UpdateAsync(newTimeSlot);

            appointment.SlotId = newTimeSlot.Id;
            appointment.AppointmentDate = newTimeSlot.Date;
            appointment.StartTime = newTimeSlot.StartTime;
            appointment.EndTime = newTimeSlot.EndTime;
            appointment.ModifiedAt = DateTime.UtcNow;
            await _appointmentRepository.UpdateAsync(appointment);
            await _appointmentRepository.SaveChangesAsync();
            await transaction.CommitAsync();

            return await MapToAppointmentDto(appointment);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PaginatedResult<AppointmentDto>> GetAllAppointmentsAsync(
        AppointmentStatus? status, int? doctorId, int? patientId, DateTime? startDate, DateTime? endDate, int pageNumber, int pageSize)
    {
        var skip = (pageNumber - 1) * pageSize;
        DateOnly? start = startDate.HasValue ? DateOnly.FromDateTime(startDate.Value) : null;
        DateOnly? end = endDate.HasValue ? DateOnly.FromDateTime(endDate.Value) : null;

        var appointments = await _appointmentRepository.GetAllAsync(status, doctorId, patientId, start, end, skip, pageSize);
        var totalCount = await _appointmentRepository.CountAllAsync(status, doctorId, patientId, start, end);

        var userIds = appointments.SelectMany(a => new[] { a.Patient!.UserId, a.Doctor!.UserId }).Distinct().ToList();
        var users = await _userInfoProvider.GetByIdsAsync(userIds);
        var dtos = appointments.Select(apt => MapToAppointmentDtoFromEntities(apt, users)).ToList();

        return new PaginatedResult<AppointmentDto>
        {
            Data = dtos,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    private async Task<AppointmentDto> MapToAppointmentDto(Appointment appointment)
    {
        var patient = appointment.Patient ?? await _patientRepository.GetByIdAsync(appointment.PatientId);
        var doctor = appointment.Doctor ?? await _doctorRepository.GetByIdAsync(appointment.DoctorId);
        var clinic = appointment.Clinic ?? null;
        var userIds = new[] { patient!.UserId, doctor!.UserId }.Distinct().ToList();
        var users = await _userInfoProvider.GetByIdsAsync(userIds);
        var patientUser = users.GetValueOrDefault(patient.UserId);
        var doctorUser = users.GetValueOrDefault(doctor.UserId);
        var clinicName = clinic?.Name ?? (await _clinicRepository.GetByIdAsync(appointment.ClinicId))?.Name ?? "";

        return new AppointmentDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = patientUser != null ? $"{patientUser.FirstName} {patientUser.LastName}".Trim() : "Unknown",
            DoctorId = appointment.DoctorId,
            DoctorName = doctorUser != null ? $"{doctorUser.FirstName} {doctorUser.LastName}".Trim() : "Unknown",
            DoctorSpecialization = doctor.Specialization,
            ClinicId = appointment.ClinicId,
            ClinicName = clinicName,
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status,
            ReasonForVisit = appointment.ReasonForVisit,
            CreatedAt = appointment.CreatedAt
        };
    }

    private async Task<AppointmentDetailDto> MapToAppointmentDetailDto(Appointment appointment)
    {
        var patient = appointment.Patient ?? await _patientRepository.GetByIdAsync(appointment.PatientId);
        var doctor = appointment.Doctor ?? await _doctorRepository.GetByIdAsync(appointment.DoctorId);
        var clinic = appointment.Clinic;
        var userIds = new[] { patient!.UserId, doctor!.UserId }.Distinct().ToList();
        var users = await _userInfoProvider.GetByIdsAsync(userIds);
        var patientUser = users.GetValueOrDefault(patient.UserId);
        var doctorUser = users.GetValueOrDefault(doctor.UserId);

        return new AppointmentDetailDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = patientUser != null ? $"{patientUser.FirstName} {patientUser.LastName}".Trim() : "Unknown",
            PatientEmail = patientUser?.Email ?? "",
            PatientPhone = patientUser?.PhoneNumber,
            DoctorId = appointment.DoctorId,
            DoctorName = doctorUser != null ? $"{doctorUser.FirstName} {doctorUser.LastName}".Trim() : "Unknown",
            DoctorSpecialization = doctor!.Specialization,
            ClinicId = appointment.ClinicId,
            ClinicName = clinic?.Name ?? "",
            ClinicAddress = clinic?.Address ?? "",
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status,
            ReasonForVisit = appointment.ReasonForVisit,
            Notes = appointment.Notes,
            CancellationReason = appointment.CancellationReason,
            CancelledAt = appointment.CancelledAt,
            CreatedAt = appointment.CreatedAt,
            UpdatedAt = appointment.UpdatedAt
        };
    }

    private AppointmentDto MapToAppointmentDtoFromEntities(Appointment apt, IReadOnlyDictionary<string, UserInfoDto> users)
    {
        var patientUser = users.GetValueOrDefault(apt.Patient!.UserId);
        var doctorUser = users.GetValueOrDefault(apt.Doctor!.UserId);
        return new AppointmentDto
        {
            Id = apt.Id,
            PatientId = apt.PatientId,
            PatientName = patientUser != null ? $"{patientUser.FirstName} {patientUser.LastName}".Trim() : "Unknown",
            DoctorId = apt.DoctorId,
            DoctorName = doctorUser != null ? $"{doctorUser.FirstName} {doctorUser.LastName}".Trim() : "Unknown",
            DoctorSpecialization = apt.Doctor!.Specialization,
            ClinicId = apt.ClinicId,
            ClinicName = apt.Clinic?.Name ?? "",
            AppointmentDate = apt.AppointmentDate,
            StartTime = apt.StartTime,
            EndTime = apt.EndTime,
            Status = apt.Status,
            ReasonForVisit = apt.ReasonForVisit,
            CreatedAt = apt.CreatedAt
        };
    }
}
