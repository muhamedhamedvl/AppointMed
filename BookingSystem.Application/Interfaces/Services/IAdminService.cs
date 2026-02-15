using BookingSystem.Application.DTOs.Admin;

namespace BookingSystem.Application.Interfaces.Services;

public interface IAdminService
{
    Task<StatisticsDto> GetStatisticsAsync();
}
