using System.Security.Claims;

namespace BookingSystem.Application.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(string userId, string email, IList<string> roles);
    ClaimsPrincipal? ValidateToken(string token);
}
