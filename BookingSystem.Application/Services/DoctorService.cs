using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.DTOs.Doctor;
using BookingSystem.Application.DTOs.TimeSlot;
using BookingSystem.Application.Exceptions;
using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IAvailableTimeSlotRepository _timeSlotRepository;
    private readonly IUserInfoProvider _userInfoProvider;

    public DoctorService(
        IDoctorRepository doctorRepository,
        IClinicRepository clinicRepository,
        IAvailableTimeSlotRepository timeSlotRepository,
        IUserInfoProvider userInfoProvider)
    {
        _doctorRepository = doctorRepository;
        _clinicRepository = clinicRepository;
        _timeSlotRepository = timeSlotRepository;
        _userInfoProvider = userInfoProvider;
    }

    public async Task<DoctorProfileDto> OnboardDoctorAsync(string userId, OnboardDoctorRequestDto request)
    {
        var user = await _userInfoProvider.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        var existing = await _doctorRepository.GetByUserIdAsync(userId);
        if (existing != null) throw new Exception("User is already registered as a doctor");

        var doctor = new Doctor
        {
            UserId = userId,
            Specialization = request.Specialization,
            LicenseNumber = request.LicenseNumber,
            YearsOfExperience = request.YearsOfExperience,
            ConsultationFee = request.ConsultationFee,
            Bio = request.Bio,
            ClinicId = request.ClinicId,
            IsApproved = false
        };

        doctor = await _doctorRepository.AddAsync(doctor);
        await _doctorRepository.SaveChangesAsync();

        await _userInfoProvider.AddToRoleAsync(userId, "Doctor");

        return await MapToDoctorProfileDto(doctor, user);
    }

    public async Task<DoctorProfileDto> GetDoctorByIdAsync(int id)
    {
        var doctor = await _doctorRepository.GetByIdWithTimeSlotsAsync(id);
        if (doctor == null) throw new Exception("Doctor not found");
        return await MapToDoctorProfileDto(doctor);
    }

    public async Task<DoctorDetailDto> GetDoctorDetailsAsync(int id)
    {
        var doctor = await _doctorRepository.GetByIdWithClinicAndTimeSlotsAsync(id);
        if (doctor == null) throw new Exception("Doctor not found");

        var user = await _userInfoProvider.GetByIdAsync(doctor.UserId);
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
            Clinic = new DTOs.Clinic.ClinicDto
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
        var doctor = await _doctorRepository.GetByIdWithTimeSlotsAsync(id);
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
        await _doctorRepository.UpdateAsync(doctor);
        await _doctorRepository.SaveChangesAsync();

        return await MapToDoctorProfileDto(doctor);
    }

    public async Task<PaginatedResult<DoctorProfileDto>> SearchDoctorsAsync(
        string? specialization, string? name, int? clinicId, string? city,
        decimal? minFee, decimal? maxFee, decimal? minRating,
        DateOnly? date, int pageNumber, int pageSize)
    {
        var skip = (pageNumber - 1) * pageSize;
        var doctors = await _doctorRepository.SearchAsync(
            specialization, clinicId, city, minFee, maxFee, minRating, date, skip, pageSize);
        var totalCount = await _doctorRepository.CountSearchAsync(
            specialization, clinicId, city, minFee, maxFee, minRating, date);

        var userIds = doctors.Select(d => d.UserId).Distinct().ToList();
        var users = await _userInfoProvider.GetByIdsAsync(userIds);

        var doctorDtos = doctors.Select(doctor =>
        {
            var user = users.GetValueOrDefault(doctor.UserId);
            var clinic = doctor.Clinic;
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
        }).ToList();

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
        var slots = await _timeSlotRepository.GetByDoctorAndDateRangeAsync(doctorId, startDate, endDate);
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
        var doctor = await _doctorRepository.GetByIdAsync(doctorId);
        if (doctor == null || doctor.UserId != userId)
            throw new Exception("Unauthorized or doctor not found");

        var slots = new List<AvailableTimeSlot>();
        foreach (var slot in request.Slots)
        {
            if (slot.Date < DateOnly.FromDateTime(DateTime.UtcNow))
                throw new Exception("Cannot create slots in the past");
            if (slot.StartTime >= slot.EndTime)
                throw new Exception("Start time must be before end time");

            var hasOverlap = await _timeSlotRepository.HasOverlappingSlotAsync(
                doctorId, slot.Date, slot.StartTime, slot.EndTime);
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

        await _timeSlotRepository.AddRangeAsync(slots);
        await _timeSlotRepository.SaveChangesAsync();

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
        var doctor = await _doctorRepository.GetByIdAsync(doctorId);
        if (doctor == null || doctor.UserId != userId)
            throw new Exception("Unauthorized or doctor not found");

        var slot = await _timeSlotRepository.GetByIdAsync(slotId);
        if (slot == null || slot.DoctorId != doctorId)
            throw new Exception("Time slot not found");
        if (slot.IsBooked)
            throw new BusinessRuleException("Cannot delete a time slot that is already booked.");

        await _timeSlotRepository.DeleteAsync(slot);
        await _timeSlotRepository.SaveChangesAsync();
    }

    public async Task<DoctorProfileDto> ApproveDoctorAsync(int id)
    {
        var doctor = await _doctorRepository.GetByIdAsync(id);
        if (doctor == null) throw new Exception("Doctor not found");

        doctor.IsApproved = true;
        doctor.UpdatedAt = DateTime.UtcNow;
        await _doctorRepository.UpdateAsync(doctor);
        await _doctorRepository.SaveChangesAsync();

        return await MapToDoctorProfileDto(doctor);
    }

    public async Task<PaginatedResult<DoctorProfileDto>> GetPendingDoctorsAsync(int pageNumber = 1, int pageSize = 10)
    {
        var skip = (pageNumber - 1) * pageSize;
        var doctors = await _doctorRepository.GetPendingAsync(skip, pageSize);
        var totalCount = await _doctorRepository.CountPendingAsync();

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

    private async Task<DoctorProfileDto> MapToDoctorProfileDto(Doctor doctor, UserInfoDto? user = null)
    {
        user ??= await _userInfoProvider.GetByIdAsync(doctor.UserId);
        var clinic = doctor.Clinic ?? await _clinicRepository.GetByIdAsync(doctor.ClinicId);

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
