using BookingSystem.Application.DTOs.Clinic;
using BookingSystem.Application.DTOs.Common;

namespace BookingSystem.Application.Interfaces.Services;

public interface IClinicService
{
    Task<ClinicDto> CreateClinicAsync(CreateClinicRequestDto request);
    Task<ClinicDto> GetClinicByIdAsync(int id);
    Task<ClinicDetailDto> GetClinicDetailsAsync(int id);
    Task<PaginatedResult<ClinicDto>> GetAllClinicsAsync(string? city = null, int pageNumber = 1, int pageSize = 10);
    Task<ClinicDto> UpdateClinicAsync(int id, UpdateClinicRequestDto request);
    Task DeleteClinicAsync(int id);
}
