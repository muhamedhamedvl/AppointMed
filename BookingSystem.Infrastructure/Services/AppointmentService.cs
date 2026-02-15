using BookingSystem.Application.DTOs.Appointment;
using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Domain.Exceptions;
using BookingSystem.Domain.Helpers;
using BookingSystem.Infrastructure.Data;
using BookingSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Infrastructure.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        ILogger<AppointmentService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<AppointmentDto> BookAppointmentAsync(string userId, CreateAppointmentRequestDto request)
    {
        // 1. Verify email confirmed
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.EmailConfirmed)
        {
            _logger.LogWarning(
                "Booking attempt failed: Email not verified. UserId: {UserId}, EmailConfirmed: {EmailConfirmed}",
                userId, user?.EmailConfirmed ?? false);
            throw new Exception("Email must be verified to book appointments");
        }

        // 2. Get patient profile
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        if (patient == null)
        {
            _logger.LogWarning(
                "Booking attempt failed: No patient profile. UserId: {UserId}",
                userId);
            throw new Exception("Please create a patient profile first");
        }

        // 3. TRANSACTION: Prevent double booking with optimistic locking
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var timeSlot = await _context.AvailableTimeSlots
                .Include(t => t.Doctor)
                .FirstOrDefaultAsync(t => t.Id == request.TimeSlotId && !t.IsBooked);

            if (timeSlot == null)
            {
                _logger.LogWarning(
                    "Booking failed: Time slot unavailable. TimeSlotId: {TimeSlotId}, PatientId: {PatientId}, DoctorId: {DoctorId}",
                    request.TimeSlotId, patient.Id, request.DoctorId);
                throw new Exception("Time slot not available or already booked");
            }

            if (timeSlot.DoctorId != request.DoctorId)
            {
                _logger.LogWarning(
                    "Booking failed: Time slot doctor mismatch. TimeSlotId: {TimeSlotId}, Expected DoctorId: {ExpectedDoctorId}, Actual DoctorId: {ActualDoctorId}",
                    request.TimeSlotId, request.DoctorId, timeSlot.DoctorId);
                throw new Exception("Time slot does not belong to the specified doctor");
            }

            // Lock the time slot
            timeSlot.IsBooked = true;

            var appointment = new Appointment
            {
                PatientId = patient.Id,
                DoctorId = request.DoctorId,
                ClinicId = timeSlot.Doctor.ClinicId,
                SlotId = timeSlot.Id,
                AppointmentDate = timeSlot.Date,
                StartTime = timeSlot.StartTime,
                EndTime = timeSlot.EndTime,
                Status = AppointmentStatus.Pending,
                ReasonForVisit = request.ReasonForVisit
            };

            _context.Appointments.Add(appointment);
            
            try
            {
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogInformation(
                    "Appointment booked successfully. AppointmentId: {AppointmentId}, PatientId: {PatientId}, DoctorId: {DoctorId}, TimeSlotId: {TimeSlotId}",
                    appointment.Id, patient.Id, request.DoctorId, request.TimeSlotId);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(
                    ex,
                    "Concurrency conflict while booking appointment. TimeSlotId: {TimeSlotId}, PatientId: {PatientId}",
                    request.TimeSlotId, patient.Id);
                throw new Exception("This time slot was just booked by another user. Please select a different time slot.", ex);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Appointment_DoctorId_AppointmentDate_StartTime") == true)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(
                    ex,
                    "Double booking prevented by database constraint. DoctorId: {DoctorId}, Date: {Date}, Time: {Time}",
                    request.DoctorId, timeSlot.Date, timeSlot.StartTime);
                throw new Exception("This time slot was just booked by another user. Please select a different time slot.");
            }

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
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Clinic)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            throw new Exception("Appointment not found");

        // Authorization check
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
        var user = await _userManager.FindByIdAsync(userId);
        var isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");

        var canAccess = (patient != null && appointment.PatientId == patient.Id) ||
                       (doctor != null && appointment.DoctorId == doctor.Id) ||
                       isAdmin;

        if (!canAccess)
            throw new Exception("Unauthorized access to appointment");

        return await MapToAppointmentDetailDto(appointment);
    }

    public async Task<PaginatedResult<AppointmentDto>> GetMyAppointmentsAsync(
        string userId, AppointmentStatus? status, bool? upcoming, bool? past,
        int pageNumber, int pageSize)
    {
        // Determine if user is patient or doctor
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

        IQueryable<Appointment> query;
        
        if (patient != null)
            query = _context.Appointments.Where(a => a.PatientId == patient.Id);
        else if (doctor != null)
            query = _context.Appointments.Where(a => a.DoctorId == doctor.Id);
        else
            throw new BusinessRuleException("No patient or doctor profile found");

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (upcoming == true)
            query = query.Where(a => a.AppointmentDate >= today);

        if (past == true)
            query = query.Where(a => a.AppointmentDate < today);

        var totalCount = await query.CountAsync();
        
        // FIX N+1: Eager load all related entities
        var appointments = await query
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Clinic)
            .OrderByDescending(a => a.AppointmentDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        // FIX N+1: Batch load all users upfront
        var userIds = appointments
            .SelectMany(a => new[] { a.Patient!.UserId, a.Doctor!.UserId })
            .Distinct()
            .ToList();
        
        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .AsNoTracking()
            .ToListAsync();
        
        var userDict = users.ToDictionary(u => u.Id);

        var dtos = appointments.Select(apt => new AppointmentDto
        {
            Id = apt.Id,
            PatientId = apt.PatientId,
            PatientName = userDict.TryGetValue(apt.Patient!.UserId, out var patientUser)
                ? $"{patientUser.FirstName} {patientUser.LastName}".Trim()
                : "Unknown",
            DoctorId = apt.DoctorId,
            DoctorName = userDict.TryGetValue(apt.Doctor!.UserId, out var doctorUser)
                ? $"{doctorUser.FirstName} {doctorUser.LastName}".Trim()
                : "Unknown",
            DoctorSpecialization = apt.Doctor.Specialization,
            ClinicId = apt.ClinicId,
            ClinicName = apt.Clinic.Name,
            AppointmentDate = apt.AppointmentDate,
            StartTime = apt.StartTime,
            EndTime = apt.EndTime,
            Status = apt.Status,
            ReasonForVisit = apt.ReasonForVisit,
            CreatedAt = apt.CreatedAt
        }).ToList();

        return new PaginatedResult<AppointmentDto>
        {
            Data = dtos,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PaginatedResult<AppointmentDto>> GetDoctorAppointmentsAsync(
        int doctorId, string userId, AppointmentStatus? status, bool? upcoming, bool? past,
        int pageNumber, int pageSize)
    {
        var doctor = await _context.Doctors.FindAsync(doctorId);
        if (doctor == null || doctor.UserId != userId)
            throw new Exception("Unauthorized or doctor not found");

        var query = _context.Appointments.Where(a => a.DoctorId == doctorId).AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (upcoming == true)
            query = query.Where(a => a.AppointmentDate >= DateOnly.FromDateTime(DateTime.UtcNow));

        if (past == true)
            query = query.Where(a => a.AppointmentDate < DateOnly.FromDateTime(DateTime.UtcNow));

        var totalCount = await query.CountAsync();
        
        // FIX N+1: Eager load all related entities
        var appointments = await query
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Clinic)
            .OrderByDescending(a => a.AppointmentDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        // FIX N+1: Batch load all users upfront
        var userIds = appointments
            .SelectMany(a => new[] { a.Patient!.UserId, a.Doctor!.UserId })
            .Distinct()
            .ToList();
        
        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .AsNoTracking()
            .ToListAsync();
        
        var userDict = users.ToDictionary(u => u.Id);

        var dtos = appointments.Select(apt => new AppointmentDto
        {
            Id = apt.Id,
            PatientId = apt.PatientId,
            PatientName = userDict.TryGetValue(apt.Patient!.UserId, out var patientUser)
                ? $"{patientUser.FirstName} {patientUser.LastName}".Trim()
                : "Unknown",
            DoctorId = apt.DoctorId,
            DoctorName = userDict.TryGetValue(apt.Doctor!.UserId, out var doctorUser)
                ? $"{doctorUser.FirstName} {doctorUser.LastName}".Trim()
                : "Unknown",
            DoctorSpecialization = apt.Doctor.Specialization,
            ClinicId = apt.ClinicId,
            ClinicName = apt.Clinic.Name,
            AppointmentDate = apt.AppointmentDate,
            StartTime = apt.StartTime,
            EndTime = apt.EndTime,
            Status = apt.Status,
            ReasonForVisit = apt.ReasonForVisit,
            CreatedAt = apt.CreatedAt
        }).ToList();

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

        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            throw new InvalidOperationException("Appointment not found");

        try
        {
            AppointmentStatusTransitionValidator.ValidateTransition(appointment.Status, newStatus);
        }
        catch (InvalidStatusTransitionException ex)
        {
            throw new InvalidStatusTransitionException(ex.Message);
        }

        switch (newStatus)
        {
            case AppointmentStatus.Confirmed:
                if (appointment.Doctor.UserId != userId)
                    throw new UnauthorizedAccessException("Only the assigned doctor can confirm appointments.");
                appointment.Status = AppointmentStatus.Confirmed;
                appointment.ModifiedAt = DateTime.UtcNow;
                break;

            case AppointmentStatus.Completed:
                if (appointment.Doctor.UserId != userId)
                    throw new UnauthorizedAccessException("Only the assigned doctor can complete appointments.");
                appointment.Status = AppointmentStatus.Completed;
                appointment.Notes = request.Notes;
                appointment.ModifiedAt = DateTime.UtcNow;
                break;

            case AppointmentStatus.NoShow:
                if (appointment.Doctor.UserId != userId)
                    throw new UnauthorizedAccessException("Only the assigned doctor can mark appointment as NoShow.");
                appointment.Status = AppointmentStatus.NoShow;
                appointment.ModifiedAt = DateTime.UtcNow;
                break;

            case AppointmentStatus.Canceled:
                var canCancel = appointment.Patient.UserId == userId || appointment.Doctor.UserId == userId;
                if (!canCancel)
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    canCancel = user != null && await _userManager.IsInRoleAsync(user, "Admin");
                }
                if (!canCancel)
                    throw new UnauthorizedAccessException("Unauthorized to cancel this appointment.");
                if (appointment.AppointmentDate < DateOnly.FromDateTime(DateTime.UtcNow))
                    throw new BusinessRuleException("Cannot cancel past appointments.");
                appointment.Status = AppointmentStatus.Canceled;
                appointment.CancellationReason = request.CancellationReason;
                appointment.CancelledAt = DateTime.UtcNow;
                appointment.ModifiedAt = DateTime.UtcNow;
                var slotToRelease = await _context.AvailableTimeSlots.FindAsync(appointment.SlotId);
                if (slotToRelease != null)
                    slotToRelease.IsBooked = false;
                break;

            default:
                break;
            }

        await _context.SaveChangesAsync();
        return await MapToAppointmentDto(appointment);
    }

    public async Task<AppointmentDto> ConfirmAppointmentAsync(int id, string userId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            throw new Exception("Appointment not found");

        if (appointment.Doctor.UserId != userId)
            throw new Exception("Only the assigned doctor can confirm appointments");

        // Strict status transition validation
        try
        {
            AppointmentStatusTransitionValidator.ValidateTransition(appointment.Status, AppointmentStatus.Confirmed);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                "Invalid status transition: AppointmentId: {AppointmentId}, CurrentStatus: {CurrentStatus}, AttemptedStatus: {AttemptedStatus}",
                id, appointment.Status, AppointmentStatus.Confirmed);
            throw new InvalidStatusTransitionException(ex.Message);
        }

        appointment.Status = AppointmentStatus.Confirmed;
        appointment.ModifiedAt = DateTime.UtcNow;
        
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Appointment confirmed. AppointmentId: {AppointmentId}, DoctorId: {DoctorId}, PatientId: {PatientId}",
                id, appointment.DoctorId, appointment.PatientId);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(
                ex,
                "Concurrency conflict while confirming appointment. AppointmentId: {AppointmentId}",
                id);
            throw new Exception("Appointment was modified by another user. Please refresh and try again.", ex);
        }

        return await MapToAppointmentDto(appointment);
    }

    public async Task<AppointmentDto> CompleteAppointmentAsync(int id, string userId, CompleteAppointmentRequestDto request)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            throw new Exception("Appointment not found");

        if (appointment.Doctor.UserId != userId)
            throw new Exception("Only the assigned doctor can complete appointments");

        // Strict status transition validation - cannot complete a canceled appointment
        try
        {
            AppointmentStatusTransitionValidator.ValidateTransition(appointment.Status, AppointmentStatus.Completed);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                "Invalid status transition while completing: AppointmentId: {AppointmentId}, CurrentStatus: {CurrentStatus}, AttemptedStatus: {AttemptedStatus}",
                id, appointment.Status, AppointmentStatus.Completed);
            throw new InvalidStatusTransitionException(ex.Message);
        }

        appointment.Status = AppointmentStatus.Completed;
        appointment.Notes = request.Notes;
        appointment.ModifiedAt = DateTime.UtcNow;
        
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Appointment completed. AppointmentId: {AppointmentId}, DoctorId: {DoctorId}, PatientId: {PatientId}, HasNotes: {HasNotes}",
                id, appointment.DoctorId, appointment.PatientId, !string.IsNullOrEmpty(request.Notes));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(
                ex,
                "Concurrency conflict while completing appointment. AppointmentId: {AppointmentId}",
                id);
            throw new Exception("Appointment was modified by another user. Please refresh and try again.", ex);
        }

        return await MapToAppointmentDto(appointment);
    }

    public async Task<AppointmentDto> CancelAppointmentAsync(int id, string userId, CancelAppointmentRequestDto request)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            throw new Exception("Appointment not found");

        // Authorization: patient, doctor, or admin can cancel
        var canCancel = appointment.Patient.UserId == userId || 
                       appointment.Doctor.UserId == userId;

        if (!canCancel)
        {
            var user = await _userManager.FindByIdAsync(userId);
            canCancel = user != null && await _userManager.IsInRoleAsync(user, "Admin");
        }

        if (!canCancel)
            throw new Exception("Unauthorized to cancel this appointment");

        if (appointment.AppointmentDate < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new Exception("Cannot cancel past appointments");

        // Strict status transition validation
        try
        {
            AppointmentStatusTransitionValidator.ValidateTransition(appointment.Status, AppointmentStatus.Canceled);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                "Invalid status transition while canceling: AppointmentId: {AppointmentId}, CurrentStatus: {CurrentStatus}, AttemptedStatus: {AttemptedStatus}",
                id, appointment.Status, AppointmentStatus.Canceled);
            throw new InvalidStatusTransitionException(ex.Message);
        }

        appointment.Status = AppointmentStatus.Canceled;
        appointment.CancellationReason = request.CancellationReason;
        appointment.CancelledAt = DateTime.UtcNow;
        appointment.ModifiedAt = DateTime.UtcNow;

        var slotToRelease = await _context.AvailableTimeSlots.FindAsync(appointment.SlotId);
        if (slotToRelease != null)
        {
            slotToRelease.IsBooked = false;
        }

        await _context.SaveChangesAsync();
        
        _logger.LogInformation(
            "Appointment {AppointmentId} canceled. Reason: {Reason}, CanceledBy: {UserId}",
            id, request.CancellationReason ?? "Not provided", userId);
        
        return await MapToAppointmentDto(appointment);
    }

    public async Task<AppointmentDto> RescheduleAppointmentAsync(int id, string userId, RescheduleAppointmentRequestDto request)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            throw new Exception("Appointment not found");

        if (appointment.Patient.UserId != userId)
            throw new Exception("Only the patient can reschedule appointments");

        if (appointment.Status == AppointmentStatus.Canceled || appointment.Status == AppointmentStatus.Completed)
            throw new Exception("Cannot reschedule canceled or completed appointments");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var newTimeSlot = await _context.AvailableTimeSlots
                .FirstOrDefaultAsync(t => t.Id == request.NewTimeSlotId && !t.IsBooked);

            if (newTimeSlot == null)
                throw new BusinessRuleException("The selected time slot is not available or has already been booked. Please choose another slot.");

            if (newTimeSlot.DoctorId != appointment.DoctorId)
                throw new Exception("New time slot must belong to the same doctor");

            var oldSlot = await _context.AvailableTimeSlots.FindAsync(appointment.SlotId);
            if (oldSlot != null)
                oldSlot.IsBooked = false;

            newTimeSlot.IsBooked = true;

            appointment.SlotId = newTimeSlot.Id;
            appointment.AppointmentDate = newTimeSlot.Date;
            appointment.StartTime = newTimeSlot.StartTime;
            appointment.EndTime = newTimeSlot.EndTime;
            appointment.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
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
        AppointmentStatus? status, int? doctorId, int? patientId,
        DateTime? startDate, DateTime? endDate, int pageNumber, int pageSize)
    {
        var query = _context.Appointments.AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId.Value);

        if (patientId.HasValue)
            query = query.Where(a => a.PatientId == patientId.Value);

        if (startDate.HasValue)
            query = query.Where(a => a.AppointmentDate >= DateOnly.FromDateTime(startDate.Value));

        if (endDate.HasValue)
            query = query.Where(a => a.AppointmentDate <= DateOnly.FromDateTime(endDate.Value));

        var totalCount = await query.CountAsync();
        var appointments = await query
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Clinic)
            .OrderByDescending(a => a.AppointmentDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // FIX N+1: Batch load all users upfront
        var userIds = appointments
            .SelectMany(a => new[] { a.Patient!.UserId, a.Doctor!.UserId })
            .Distinct()
            .ToList();
        
        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .AsNoTracking()
            .ToListAsync();
        
        var userDict = users.ToDictionary(u => u.Id);

        var dtos = appointments.Select(apt => new AppointmentDto
        {
            Id = apt.Id,
            PatientId = apt.PatientId,
            PatientName = userDict.TryGetValue(apt.Patient!.UserId, out var patientUser)
                ? $"{patientUser.FirstName} {patientUser.LastName}".Trim()
                : "Unknown",
            DoctorId = apt.DoctorId,
            DoctorName = userDict.TryGetValue(apt.Doctor!.UserId, out var doctorUser)
                ? $"{doctorUser.FirstName} {doctorUser.LastName}".Trim()
                : "Unknown",
            DoctorSpecialization = apt.Doctor.Specialization,
            ClinicId = apt.ClinicId,
            ClinicName = apt.Clinic.Name,
            AppointmentDate = apt.AppointmentDate,
            StartTime = apt.StartTime,
            EndTime = apt.EndTime,
            Status = apt.Status,
            ReasonForVisit = apt.ReasonForVisit,
            CreatedAt = apt.CreatedAt
        }).ToList();

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
        // Use already loaded navigation properties to avoid N+1 queries
        var patient = appointment.Patient ?? await _context.Patients.FindAsync(appointment.PatientId);
        var doctor = appointment.Doctor ?? await _context.Doctors.FindAsync(appointment.DoctorId);
        var clinic = appointment.Clinic ?? await _context.Clinics.FindAsync(appointment.ClinicId);

        // Batch load users to avoid N+1 queries
        var userIds = new[] { patient!.UserId, doctor!.UserId }.Distinct().ToList();
        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();
        
        var patientUser = users.FirstOrDefault(u => u.Id == patient.UserId);
        var doctorUser = users.FirstOrDefault(u => u.Id == doctor.UserId);

        return new AppointmentDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = $"{patientUser?.FirstName} {patientUser?.LastName}".Trim(),
            DoctorId = appointment.DoctorId,
            DoctorName = $"{doctorUser?.FirstName} {doctorUser?.LastName}".Trim(),
            DoctorSpecialization = doctor.Specialization,
            ClinicId = appointment.ClinicId,
            ClinicName = clinic?.Name ?? "",
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
        var patient = appointment.Patient ?? await _context.Patients.FindAsync(appointment.PatientId);
        var doctor = appointment.Doctor ?? await _context.Doctors.FindAsync(appointment.DoctorId);
        var clinic = appointment.Clinic ?? await _context.Clinics.FindAsync(appointment.ClinicId);

        var userIds = new[] { patient!.UserId, doctor!.UserId }.Distinct().ToList();
        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();
        
        var patientUser = users.FirstOrDefault(u => u.Id == patient.UserId);
        var doctorUser = users.FirstOrDefault(u => u.Id == doctor.UserId);

        return new AppointmentDetailDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = $"{patientUser?.FirstName} {patientUser?.LastName}".Trim(),
            PatientEmail = patientUser?.Email ?? "",
            PatientPhone = patientUser?.PhoneNumber,
            DoctorId = appointment.DoctorId,
            DoctorName = $"{doctorUser?.FirstName} {doctorUser?.LastName}".Trim(),
            DoctorSpecialization = doctor.Specialization,
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
}
