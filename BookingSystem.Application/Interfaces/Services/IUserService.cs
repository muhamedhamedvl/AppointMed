using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.DTOs.User;

namespace BookingSystem.Application.Interfaces.Services;

public interface IUserService
{
    Task<(bool Success, string Message, UserResponseDto? Data)> GetUserByIdAsync(string userId);
    Task<(bool Success, string Message, UserResponseDto? Data)> GetCurrentUserAsync(string userId);
    Task<(bool Success, string Message, UserResponseDto? Data)> UpdateCurrentUserAsync(string userId, UpdateMyProfileDto dto);
    Task<(bool Success, string Message, UserResponseDto? Data)> UpdateUserAsync(string userId, UpdateUserDto dto);
    Task<(bool Success, string Message)> DeleteUserAsync(string userId);
    Task<(bool Success, string Message, UserResponseDto? Data)> CreateUserAsync(CreateUserDto dto);
    Task<(bool Success, string Message, PaginatedResult<UserResponseDto>? Data)> GetUsersAsync(int page = 1, int pageSize = 10);
}
