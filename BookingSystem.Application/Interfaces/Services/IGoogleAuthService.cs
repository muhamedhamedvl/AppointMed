using BookingSystem.Application.DTOs.Auth;

namespace BookingSystem.Application.Interfaces.Services;

public interface IGoogleAuthService
{
    Task<(bool Success, string Message, string? Email, string? FirstName, string? LastName)> ValidateGoogleTokenAsync(string idToken);
}
