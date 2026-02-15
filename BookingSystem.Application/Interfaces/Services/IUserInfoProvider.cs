namespace BookingSystem.Application.Interfaces.Services;

public record UserInfoDto(string Id, string FirstName, string LastName, string? Email, string? PhoneNumber);

public interface IUserInfoProvider
{
    Task<UserInfoDto?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, UserInfoDto>> GetByIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
    Task<bool> IsEmailConfirmedAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsInRoleAsync(string userId, string role, CancellationToken cancellationToken = default);
    Task AddToRoleAsync(string userId, string role, CancellationToken cancellationToken = default);
}
