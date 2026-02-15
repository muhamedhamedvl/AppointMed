using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.DTOs.Doctor;
using BookingSystem.Application.DTOs.TimeSlot;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Exceptions;
using BookingSystem.Infrastructure.Data;
using BookingSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Services;

public class DoctorService : IDoctorService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DoctorService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<DoctorProfileDto> OnboardDoctorAsync(string userId, OnboardDoctorRequestDto request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        var existing = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
        if (existing != null) throw new Exception("User is already registered as a doctor");

        var doctor = new Doctor
        {
            UserId = userId,
            Specialization = request.Specialization,
            LicenseNumber = request.LicenseNumber,
            YearsOfExperience = request.YearsOfExperience,
            ConsultationFee = request.ConsultationFee,
            Bio = request.Bio,
            ClinicId = request.ClinicId, // Required
            IsApproved = false
        };

        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();

        await _userManager.AddToRoleAsync(user, "Doctor");

        return await MapToDoctorProfileDto(doctor, user);
    }

    public async Task<DoctorProfileDto> GetDoctorByIdAsync(int id)
    {
        var doctor = await _context.Doctors
            .Include(d => d.AvailableTimeSlots)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doctor == null) throw new Exception("Doctor not found");
        return await MapToDoctorProfileDto(doctor);
    }

    public async Task<DoctorDetailDto> GetDoctorDetailsAsync(int id)
    {
        var doctor = await _context.Doctors
            .Include(d => d.Clinic)
            .Include(d => d.AvailableTimeSlots)
            .FirstOrDefaultAsync(d => d.Id == id);
        
        if (doctor == null) throw new Exception("Doctor not found");

        var user = await _userManager.FindByIdAsync(doctor.UserId);
        if (user == null) throw new Exception("User not found");

        var hasAvailability = doctor.AvailableTimeSlots.Any(s => s.Date >= DateOnly.FromDateTime(DateTime.UtcNow) && !s.IsBooked);

        return new DoctorDetailDto
        {
            Id = doctor.Id,
            UserId = doctor.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber ?? "",
            Specialization = doctor.Specialization,
            LicenseNumber = doctor.LicenseNumber,
            YearsOfExperience = doctor.YearsOfExperience,
            ConsultationFee = doctor.ConsultationFee,
            Bio = doctor.Bio,
            IsAvailable = hasAvailability,
            IsApproved = doctor.IsApproved,
            AverageRating = doctor.AverageRating,
            TotalReviews = doctor.TotalReviews,
            Clinic = new Application.DTOs.Clinic.ClinicDto
            {
                Id = doctor.Clinic.Id,
                Name = doctor.Clinic.Name,
                Address = doctor.Clinic.Address,
                City = doctor.Clinic.City,
                State = doctor.Clinic.State,
                ZipCode = doctor.Clinic.ZipCode,
                PhoneNumber = doctor.Clinic.PhoneNumber,
                Email = doctor.Clinic.Email,
                OpeningTime = doctor.Clinic.OpeningTime,
                ClosingTime = doctor.Clinic.ClosingTime,
                CreatedAt = doctor.Clinic.CreatedAt
            },
            CreatedAt = doctor.CreatedAt
        };
    }

    public async Task<DoctorProfileDto> UpdateDoctorAsync(int id, string userId, UpdateDoctorRequestDto request)
    {
        var doctor = await _context.Doctors
            .Include(d => d.AvailableTimeSlots)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doctor == null || doctor.UserId != userId)
            throw new Exception("Unauthorized or doctor not found");

        if (!string.IsNullOrEmpty(request.Specialization))
            doctor.Specialization = request.Specialization;
        
        if (request.YearsOfExperience.HasValue)
            doctor.YearsOfExperience = request.YearsOfExperience.Value;
        
        if (request.ConsultationFee.HasValue)
            doctor.ConsultationFee = request.ConsultationFee.Value;
        
        if (request.Bio != null)
            doctor.Bio = request.Bio;
        
        if (request.ClinicId.HasValue)
            doctor.ClinicId = request.ClinicId.Value;

        doctor.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await MapToDoctorProfileDto(doctor);
    }

    public async Task<PaginatedResult<DoctorProfileDto>> SearchDoctorsAsync(
        string? specialization, string? name, int? clinicId, string? city,
        decimal? minFee, decimal? maxFee, decimal? minRating,
        DateOnly? date, int pageNumber, int pageSize)
    {
        var query = _context.Doctors
            .Include(d => d.Clinic)
            .Include(d => d.AvailableTimeSlots)
            .Where(d => d.IsApproved)
            .AsQueryable();

        if (!string.IsNullOrEmpty(specialization))
            query = query.Where(d => d.Specialization.Contains(specialization));

        if (clinicId.HasValue)
            query = query.Where(d => d.ClinicId == clinicId);

        // Filter by city (through Clinic)
        if (!string.IsNullOrEmpty(city))
            query = query.Where(d => d.Clinic != null && d.Clinic.City.Contains(city));

        // Filter by consultation fee range
        if (minFee.HasValue)
            query = query.Where(d => d.ConsultationFee >= minFee.Value);

        if (maxFee.HasValue)
            query = query.Where(d => d.ConsultationFee <= maxFee.Value);

        if (minRating.HasValue)
            query = query.Where(d => d.AverageRating >= minRating.Value);

        if (date.HasValue)
        {
            query = query.Where(d => d.AvailableTimeSlots.Any(
                s => s.Date == date.Value && !s.IsBooked));
        }

        var totalCount = await query.CountAsync();
        var doctors = await query
            .Include(d => d.Clinic) // Already included above, but keep for clarity
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Batch load all users to avoid N+1 queries
        var userIds = doctors.Select(d => d.UserId).Distinct().ToList();
        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();
        
        var userLookup = users.ToDictionary(u => u.Id);

        var doctorDtos = new List<DoctorProfileDto>();
        foreach (var doctor in doctors)
        {
            var user = userLookup.GetValueOrDefault(doctor.UserId);
            doctorDtos.Add(await MapToDoctorProfileDto(doctor, user));
        }

        return new PaginatedResult<DoctorProfileDto>
        {
            Data = doctorDtos,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<AvailableTimeSlotDto>> GetDoctorAvailabilityAsync(int doctorId, DateOnly startDate, DateOnly endDate)
    {
        var slots = await _context.AvailableTimeSlots
            .Where(s => s.DoctorId == doctorId && s.Date >= startDate && s.Date <= endDate)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        return slots.Select(s => new AvailableTimeSlotDto
        {
            Id = s.Id,
            DoctorId = s.DoctorId,
            Date = s.Date,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            IsBooked = s.IsBooked,
            CreatedAt = s.CreatedAt
        }).ToList();
    }

    public async Task<List<AvailableTimeSlotDto>> AddAvailabilityAsync(int doctorId, string userId, AddAvailabilityRequestDto request)
    {
        var doctor = await _context.Doctors.FindAsync(doctorId);
        if (doctor == null || doctor.UserId != userId)
            throw new Exception("Unauthorized or doctor not found");

        var slots = new List<AvailableTimeSlot>();
        foreach (var slot in request.Slots)
        {
            if (slot.Date < DateOnly.FromDateTime(DateTime.UtcNow))
                throw new Exception("Cannot create slots in the past");

            if (slot.StartTime >= slot.EndTime)
                throw new Exception("Start time must be before end time");

            // Check for overlapping slots for the same doctor on the same date
            var hasOverlap = await _context.AvailableTimeSlots
                .Where(s => s.DoctorId == doctorId 
                    && s.Date == slot.Date 
                    && !s.IsDeleted
                    && !s.IsBooked
                    && (
                        // New slot starts during existing slot
                        (slot.StartTime >= s.StartTime && slot.StartTime < s.EndTime) ||
                        // New slot ends during existing slot
                        (slot.EndTime > s.StartTime && slot.EndTime <= s.EndTime) ||
                        // New slot completely contains existing slot
                        (slot.StartTime <= s.StartTime && slot.EndTime >= s.EndTime) ||
                        // Existing slot completely contains new slot
                        (s.StartTime <= slot.StartTime && s.EndTime >= slot.EndTime)
                    ))
                .AnyAsync();

            if (hasOverlap)
                throw new Exception($"Time slot overlaps with an existing slot on {slot.Date:yyyy-MM-dd} between {slot.StartTime:HH:mm} and {slot.EndTime:HH:mm}");

            slots.Add(new AvailableTimeSlot
            {
                DoctorId = doctorId,
                Date = slot.Date,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                IsBooked = false
            });
        }

        _context.AvailableTimeSlots.AddRange(slots);
        await _context.SaveChangesAsync();

        return slots.Select(s => new AvailableTimeSlotDto
        {
            Id = s.Id,
            DoctorId = s.DoctorId,
            Date = s.Date,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            IsBooked = s.IsBooked,
            CreatedAt = s.CreatedAt
        }).ToList();
    }

    public async Task DeleteTimeSlotAsync(int doctorId, int slotId, string userId)
    {
        var doctor = await _context.Doctors.FindAsync(doctorId);
        if (doctor == null || doctor.UserId != userId)
            throw new Exception("Unauthorized or doctor not found");

        var slot = await _context.AvailableTimeSlots.FindAsync(slotId);
        if (slot == null || slot.DoctorId != doctorId)
            throw new Exception("Time slot not found");

        if (slot.IsBooked)
            throw new BusinessRuleException("Cannot delete a time slot that is already booked.");

        _context.AvailableTimeSlots.Remove(slot);
        await _context.SaveChangesAsync();
    }

    public async Task<DoctorProfileDto> ApproveDoctorAsync(int id)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor == null) throw new Exception("Doctor not found");

        doctor.IsApproved = true;
        doctor.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await MapToDoctorProfileDto(doctor);
    }

    public async Task<PaginatedResult<DoctorProfileDto>> GetPendingDoctorsAsync(int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.Doctors
            .Include(d => d.AvailableTimeSlots)
            .Where(d => !d.IsApproved);

        var totalCount = await query.CountAsync();
        var doctors = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<DoctorProfileDto>();
        foreach (var doctor in doctors)
        {
            result.Add(await MapToDoctorProfileDto(doctor));
        }
        return new PaginatedResult<DoctorProfileDto>
        {
            Data = result,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    private async Task<DoctorProfileDto> MapToDoctorProfileDto(Doctor doctor, ApplicationUser? user = null)
    {
        // Use provided user or load if not provided (should be provided from batch load)
        user ??= await _userManager.FindByIdAsync(doctor.UserId);
        
        var clinic = doctor.Clinic ?? await _context.Clinics.FindAsync(doctor.ClinicId);

        return new DoctorProfileDto
        {
            Id = doctor.Id,
            UserId = doctor.UserId,
            FirstName = user?.FirstName ?? "",
            LastName = user?.LastName ?? "",
            Email = user?.Email ?? "",
            Specialization = doctor.Specialization,
            LicenseNumber = doctor.LicenseNumber,
            YearsOfExperience = doctor.YearsOfExperience,
            ConsultationFee = doctor.ConsultationFee,
            Bio = doctor.Bio,
            ClinicId = doctor.ClinicId,
            ClinicName = clinic?.Name ?? "",
            IsAvailable = doctor.AvailableTimeSlots?.Any(s => s.Date >= DateOnly.FromDateTime(DateTime.UtcNow) && !s.IsBooked) ?? false,
            IsApproved = doctor.IsApproved,
            AverageRating = doctor.AverageRating,
            TotalReviews = doctor.TotalReviews,
            CreatedAt = doctor.CreatedAt
        };
    }
}
