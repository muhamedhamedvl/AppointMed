using BookingSystem.Application.DTOs.Patient;

namespace BookingSystem.Application.Interfaces.Services;

public interface IPatientService
{
    Task<PatientProfileDto> CreatePatientProfileAsync(string userId, CreatePatientRequestDto request);
    Task<PatientProfileDto> GetPatientProfileAsync(string userId);
    Task<PatientProfileDto> GetPatientByIdAsync(int id);
    Task<PatientProfileDto> UpdatePatientProfileAsync(string userId, UpdatePatientRequestDto request);
    Task<PatientProfileDto> GetOrCreatePatientProfileAsync(string userId, CreatePatientRequestDto? createRequest = null);
}
