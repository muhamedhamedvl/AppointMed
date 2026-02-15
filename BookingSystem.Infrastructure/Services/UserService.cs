using BookingSystem.Application.DTOs.Common;
using BookingSystem.Application.DTOs.User;
using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<(bool Success, string Message, UserResponseDto? Data)> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found", null);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var response = MapToUserResponseDto(user, roles);

        return (true, "User retrieved successfully", response);
    }

    public async Task<(bool Success, string Message, UserResponseDto? Data)> GetCurrentUserAsync(string userId)
    {
        return await GetUserByIdAsync(userId);
    }

    public async Task<(bool Success, string Message, UserResponseDto? Data)> UpdateCurrentUserAsync(string userId, UpdateMyProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found", null);
        }

        // Update only allowed fields
        if (!string.IsNullOrWhiteSpace(dto.FirstName))
            user.FirstName = dto.FirstName;

        if (!string.IsNullOrWhiteSpace(dto.LastName))
            user.LastName = dto.LastName;

        if (dto.PhoneNumber != null)
            user.PhoneNumber = dto.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Update failed: {errors}", null);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var response = MapToUserResponseDto(user, roles);

        return (true, "Profile updated successfully", response);
    }

    public async Task<(bool Success, string Message, UserResponseDto? Data)> UpdateUserAsync(string userId, UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found", null);
        }

        // Update fields
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null && existingUser.Id != userId)
            {
                return (false, "Email already in use", null);
            }
            user.Email = dto.Email;
            user.UserName = dto.Email;
        }

        if (!string.IsNullOrWhiteSpace(dto.FirstName))
            user.FirstName = dto.FirstName;

        if (!string.IsNullOrWhiteSpace(dto.LastName))
            user.LastName = dto.LastName;

        if (dto.PhoneNumber != null)
            user.PhoneNumber = dto.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Update failed: {errors}", null);
        }

        // Update role if specified
        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, dto.Role);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var response = MapToUserResponseDto(user, roles);

        return (true, "User updated successfully", response);
    }

    public async Task<(bool Success, string Message)> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, "User not found");
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Delete failed: {errors}");
        }

        return (true, "User deleted successfully");
    }

    public async Task<(bool Success, string Message, UserResponseDto? Data)> CreateUserAsync(CreateUserDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            return (false, "User with this email already exists", null);
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"User creation failed: {errors}", null);
        }

        // Assign role
        await _userManager.AddToRoleAsync(user, dto.Role);

        var roles = await _userManager.GetRolesAsync(user);
        var response = MapToUserResponseDto(user, roles);

        return (true, "User created successfully", response);
    }

    public async Task<(bool Success, string Message, PaginatedResult<UserResponseDto>? Data)> GetUsersAsync(int page = 1, int pageSize = 10)
    {
        var totalCount = await _userManager.Users.CountAsync();
        var users = await _userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userDtos = new List<UserResponseDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(MapToUserResponseDto(user, roles));
        }

        var result = new PaginatedResult<UserResponseDto>
        {
            Data = userDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return (true, "Users retrieved successfully", result);
    }

    private UserResponseDto MapToUserResponseDto(ApplicationUser user, IList<string> roles)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Roles = roles,
            CreatedAt = user.CreatedAt
        };
    }
}
