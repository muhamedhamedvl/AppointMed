using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly ApplicationDbContext _context;

    public AppointmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Appointment?> GetByIdAsync(int id)
    {
        return await _context.Appointments.FindAsync(id);
    }

    public async Task<Appointment?> GetByIdWithPatientDoctorAsync(int id)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Appointment?> GetByIdWithPatientDoctorClinicAsync(int id)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Clinic)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Appointment?> GetByIdWithPatientDoctorSlotAsync(int id)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Appointment?> GetByIdWithReviewAsync(int id)
    {
        return await _context.Appointments
            .Include(a => a.Review)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Appointment> AddAsync(Appointment appointment)
    {
        await _context.Appointments.AddAsync(appointment);
        return appointment;
    }

    public Task UpdateAsync(Appointment appointment)
    {
        _context.Appointments.Update(appointment);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Appointment>> GetByPatientIdAsync(
        int patientId, AppointmentStatus? status, bool? upcoming, bool? past, int skip, int take)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Clinic)
            .Where(a => a.PatientId == patientId)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (upcoming == true)
            query = query.Where(a => a.AppointmentDate >= today);
        if (past == true)
            query = query.Where(a => a.AppointmentDate < today);

        return await query
            .OrderByDescending(a => a.AppointmentDate)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> CountByPatientIdAsync(
        int patientId, AppointmentStatus? status, bool? upcoming, bool? past)
    {
        var query = _context.Appointments.Where(a => a.PatientId == patientId).AsQueryable();
        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (upcoming == true)
            query = query.Where(a => a.AppointmentDate >= today);
        if (past == true)
            query = query.Where(a => a.AppointmentDate < today);
        return await query.CountAsync();
    }

    public async Task<IEnumerable<Appointment>> GetByDoctorIdAsync(
        int doctorId, AppointmentStatus? status, bool? upcoming, bool? past, int skip, int take)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Clinic)
            .Where(a => a.DoctorId == doctorId)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (upcoming == true)
            query = query.Where(a => a.AppointmentDate >= today);
        if (past == true)
            query = query.Where(a => a.AppointmentDate < today);

        return await query
            .OrderByDescending(a => a.AppointmentDate)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> CountByDoctorIdAsync(
        int doctorId, AppointmentStatus? status, bool? upcoming, bool? past)
    {
        var query = _context.Appointments.Where(a => a.DoctorId == doctorId).AsQueryable();
        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (upcoming == true)
            query = query.Where(a => a.AppointmentDate >= today);
        if (past == true)
            query = query.Where(a => a.AppointmentDate < today);
        return await query.CountAsync();
    }

    public async Task<IEnumerable<Appointment>> GetAllAsync(
        AppointmentStatus? status, int? doctorId, int? patientId,
        DateOnly? startDate, DateOnly? endDate, int skip, int take)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Clinic)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);
        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId.Value);
        if (patientId.HasValue)
            query = query.Where(a => a.PatientId == patientId.Value);
        if (startDate.HasValue)
            query = query.Where(a => a.AppointmentDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(a => a.AppointmentDate <= endDate.Value);

        return await query
            .OrderByDescending(a => a.AppointmentDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountAllAsync(
        AppointmentStatus? status, int? doctorId, int? patientId,
        DateOnly? startDate, DateOnly? endDate)
    {
        var query = _context.Appointments.AsQueryable();
        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);
        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId.Value);
        if (patientId.HasValue)
            query = query.Where(a => a.PatientId == patientId.Value);
        if (startDate.HasValue)
            query = query.Where(a => a.AppointmentDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(a => a.AppointmentDate <= endDate.Value);
        return await query.CountAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
