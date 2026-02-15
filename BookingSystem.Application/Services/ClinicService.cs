using BookingSystem.Application.DTOs.Clinic;
using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.DTOs.Doctor;
using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Services;

public class ClinicService : IClinicService
{
    private readonly IClinicRepository _clinicRepository;

    public ClinicService(IClinicRepository clinicRepository)
    {
        _clinicRepository = clinicRepository;
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

        clinic = await _clinicRepository.AddAsync(clinic);
        await _clinicRepository.SaveChangesAsync();

        return MapToClinicDto(clinic);
    }

    public async Task<ClinicDto> GetClinicByIdAsync(int id)
    {
        var clinic = await _clinicRepository.GetByIdAsync(id);
        if (clinic == null) throw new Exception("Clinic not found");
        return MapToClinicDto(clinic);
    }

    public async Task<ClinicDetailDto> GetClinicDetailsAsync(int id)
    {
        var clinic = await _clinicRepository.GetByIdWithDoctorsAsync(id);
        if (clinic == null) throw new Exception("Clinic not found");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var doctorDtos = clinic.Doctors.Select(doctor => new DoctorProfileDto
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
            IsAvailable = doctor.AvailableTimeSlots.Any(s => s.Date >= today && !s.IsBooked),
            IsApproved = doctor.IsApproved,
            AverageRating = doctor.AverageRating,
            TotalReviews = doctor.TotalReviews,
            CreatedAt = doctor.CreatedAt
        }).ToList();

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
        var skip = (pageNumber - 1) * pageSize;
        var clinics = await _clinicRepository.GetAllAsync(city, skip, pageSize);
        var totalCount = await _clinicRepository.CountAllAsync(city);
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
        var clinic = await _clinicRepository.GetByIdAsync(id);
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
        await _clinicRepository.UpdateAsync(clinic);
        await _clinicRepository.SaveChangesAsync();

        return MapToClinicDto(clinic);
    }

    public async Task DeleteClinicAsync(int id)
    {
        var clinic = await _clinicRepository.GetByIdWithDoctorsAsync(id);
        if (clinic == null) throw new Exception("Clinic not found");

        if (clinic.Doctors.Count > 0)
            throw new Exception("Cannot delete clinic with assigned doctors");

        await _clinicRepository.DeleteAsync(clinic);
        await _clinicRepository.SaveChangesAsync();
    }

    private static ClinicDto MapToClinicDto(Clinic clinic)
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
