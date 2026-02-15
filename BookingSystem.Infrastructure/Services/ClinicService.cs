using BookingSystem.Application.DTOs.Clinic;
using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Services;

public class ClinicService : IClinicService
{
    private readonly ApplicationDbContext _context;

    public ClinicService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ClinicDto> CreateClinicAsync(CreateClinicRequestDto request)
    {
        var clinic = new Clinic
        {
            Name = request.Name,
            Address = request.Address,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            OpeningTime = request.OpeningTime,
            ClosingTime = request.ClosingTime
        };

        _context.Clinics.Add(clinic);
        await _context.SaveChangesAsync();

        return MapToClinicDto(clinic);
    }

    public async Task<ClinicDto> GetClinicByIdAsync(int id)
    {
        var clinic = await _context.Clinics.FindAsync(id);
        if (clinic == null) throw new Exception("Clinic not found");
        
        return MapToClinicDto(clinic);
    }

    public async Task<ClinicDetailDto> GetClinicDetailsAsync(int id)
    {
        var clinic = await _context.Clinics
            .Include(c => c.Doctors)
            .ThenInclude(d => d.AvailableTimeSlots)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clinic == null) throw new Exception("Clinic not found");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var doctorDtos = new List<Application.DTOs.Doctor.DoctorProfileDto>();
        
        foreach (var doctor in clinic.Doctors)
        {
            var hasAvailability = doctor.AvailableTimeSlots.Any(s => s.Date >= today && !s.IsBooked);
            doctorDtos.Add(new Application.DTOs.Doctor.DoctorProfileDto
            {
                Id = doctor.Id,
                UserId = doctor.UserId,
                Specialization = doctor.Specialization,
                LicenseNumber = doctor.LicenseNumber,
                YearsOfExperience = doctor.YearsOfExperience,
                ConsultationFee = doctor.ConsultationFee,
                Bio = doctor.Bio,
                ClinicId = doctor.ClinicId,
                ClinicName = clinic.Name,
                IsAvailable = hasAvailability,
                IsApproved = doctor.IsApproved,
                AverageRating = doctor.AverageRating,
                TotalReviews = doctor.TotalReviews,
                CreatedAt = doctor.CreatedAt
            });
        }

        return new ClinicDetailDto
        {
            Id = clinic.Id,
            Name = clinic.Name,
            Address = clinic.Address,
            City = clinic.City,
            State = clinic.State,
            ZipCode = clinic.ZipCode,
            PhoneNumber = clinic.PhoneNumber,
            Email = clinic.Email,
            OpeningTime = clinic.OpeningTime,
            ClosingTime = clinic.ClosingTime,
            DoctorCount = clinic.Doctors.Count,
            Doctors = doctorDtos,
            CreatedAt = clinic.CreatedAt
        };
    }

    public async Task<PaginatedResult<ClinicDto>> GetAllClinicsAsync(string? city, int pageNumber, int pageSize)
    {
        var query = _context.Clinics.AsQueryable();

        if (!string.IsNullOrEmpty(city))
            query = query.Where(c => c.City.Contains(city));

        var totalCount = await query.CountAsync();
        var clinics = await query
            .OrderBy(c => c.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = clinics.Select(MapToClinicDto).ToList();

        return new PaginatedResult<ClinicDto>
        {
            Data = dtos,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<ClinicDto> UpdateClinicAsync(int id, UpdateClinicRequestDto request)
    {
        var clinic = await _context.Clinics.FindAsync(id);
        if (clinic == null) throw new Exception("Clinic not found");

        if (!string.IsNullOrEmpty(request.Name))
            clinic.Name = request.Name;
        
        if (!string.IsNullOrEmpty(request.Address))
            clinic.Address = request.Address;
        
        if (!string.IsNullOrEmpty(request.City))
            clinic.City = request.City;
        
        if (!string.IsNullOrEmpty(request.State))
            clinic.State = request.State;
        
        if (!string.IsNullOrEmpty(request.ZipCode))
            clinic.ZipCode = request.ZipCode;
        
        if (!string.IsNullOrEmpty(request.PhoneNumber))
            clinic.PhoneNumber = request.PhoneNumber;
        
        if (request.Email != null)
            clinic.Email = request.Email;
        
        if (request.OpeningTime.HasValue)
            clinic.OpeningTime = request.OpeningTime.Value;
        
        if (request.ClosingTime.HasValue)
            clinic.ClosingTime = request.ClosingTime.Value;

        clinic.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToClinicDto(clinic);
    }

    public async Task DeleteClinicAsync(int id)
    {
        var clinic = await _context.Clinics
            .Include(c => c.Doctors)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clinic == null) throw new Exception("Clinic not found");

        if (clinic.Doctors.Any())
            throw new Exception("Cannot delete clinic with assigned doctors");

        _context.Clinics.Remove(clinic);
        await _context.SaveChangesAsync();
    }

    private ClinicDto MapToClinicDto(Clinic clinic)
    {
        return new ClinicDto
        {
            Id = clinic.Id,
            Name = clinic.Name,
            Address = clinic.Address,
            City = clinic.City,
            State = clinic.State,
            ZipCode = clinic.ZipCode,
            PhoneNumber = clinic.PhoneNumber,
            Email = clinic.Email,
            OpeningTime = clinic.OpeningTime,
            ClosingTime = clinic.ClosingTime,
            CreatedAt = clinic.CreatedAt
        };
    }
}
