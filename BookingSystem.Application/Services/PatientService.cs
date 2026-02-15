using BookingSystem.Application.DTOs.Patient;
using BookingSystem.Application.Enums;
using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUserInfoProvider _userInfoProvider;

    public PatientService(IPatientRepository patientRepository, IUserInfoProvider userInfoProvider)
    {
        _patientRepository = patientRepository;
        _userInfoProvider = userInfoProvider;
    }

    public async Task<PatientProfileDto> CreatePatientProfileAsync(string userId, CreatePatientRequestDto request)
    {
        var existing = await _patientRepository.GetByUserIdAsync(userId);
        if (existing != null) throw new Exception("Patient profile already exists");

        var patient = new Patient
        {
            UserId = userId,
            DateOfBirth = request.DateOfBirth,
            Gender = (Domain.Enums.Gender)(int)request.Gender,
            BloodGroup = request.BloodGroup,
            Address = request.Address,
            EmergencyContact = request.EmergencyContact,
            MedicalHistory = request.MedicalHistory
        };

        patient = await _patientRepository.AddAsync(patient);
        await _patientRepository.SaveChangesAsync();

        return await MapToPatientDto(patient);
    }

    public async Task<PatientProfileDto> GetPatientProfileAsync(string userId)
    {
        var patient = await _patientRepository.GetByUserIdAsync(userId);
        if (patient == null) throw new Exception("Patient profile not found");
        return await MapToPatientDto(patient);
    }

    public async Task<PatientProfileDto> GetPatientByIdAsync(int id)
    {
        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null) throw new Exception("Patient not found");
        return await MapToPatientDto(patient);
    }

    public async Task<PatientProfileDto> UpdatePatientProfileAsync(string userId, UpdatePatientRequestDto request)
    {
        var patient = await _patientRepository.GetByUserIdAsync(userId);
        if (patient == null) throw new Exception("Patient profile not found");

        if (request.DateOfBirth.HasValue)
            patient.DateOfBirth = request.DateOfBirth.Value;
        if (request.Gender.HasValue)
            patient.Gender = (Domain.Enums.Gender)(int)request.Gender.Value;
        if (request.BloodGroup != null)
            patient.BloodGroup = request.BloodGroup;
        if (request.Address != null)
            patient.Address = request.Address;
        if (request.EmergencyContact != null)
            patient.EmergencyContact = request.EmergencyContact;
        if (request.MedicalHistory != null)
            patient.MedicalHistory = request.MedicalHistory;

        patient.UpdatedAt = DateTime.UtcNow;
        await _patientRepository.UpdateAsync(patient);
        await _patientRepository.SaveChangesAsync();

        return await MapToPatientDto(patient);
    }

    public async Task<PatientProfileDto> GetOrCreatePatientProfileAsync(string userId, CreatePatientRequestDto? createRequest = null)
    {
        var patient = await _patientRepository.GetByUserIdAsync(userId);
        if (patient != null)
            return await MapToPatientDto(patient);

        if (createRequest == null)
            throw new Exception("Patient profile does not exist. Please create one first.");

        return await CreatePatientProfileAsync(userId, createRequest);
    }

    private async Task<PatientProfileDto> MapToPatientDto(Patient patient)
    {
        var user = await _userInfoProvider.GetByIdAsync(patient.UserId);
        return new PatientProfileDto
        {
            Id = patient.Id,
            UserId = patient.UserId,
            FirstName = user?.FirstName ?? "",
            LastName = user?.LastName ?? "",
            Email = user?.Email ?? "",
            DateOfBirth = patient.DateOfBirth,
            Gender = (Gender)(int)patient.Gender,
            BloodGroup = patient.BloodGroup,
            Address = patient.Address,
            EmergencyContact = patient.EmergencyContact,
            MedicalHistory = patient.MedicalHistory,
            CreatedAt = patient.CreatedAt
        };
    }
}
