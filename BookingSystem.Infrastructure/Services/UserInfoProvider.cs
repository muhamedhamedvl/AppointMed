using BookingSystem.Application.Interfaces.Services;
using BookingSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Services;

public class UserInfoProvider : IUserInfoProvider
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserInfoProvider(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserInfoDto?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user == null ? null : Map(user);
    }

    public async Task<IReadOnlyDictionary<string, UserInfoDto>> GetByIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<string, UserInfoDto>();

        var users = await _userManager.Users
            .Where(u => ids.Contains(u.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return users.ToDictionary(u => u.Id, Map);
    }

    public async Task<bool> IsEmailConfirmedAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.EmailConfirmed ?? false;
    }

    public async Task<bool> IsInRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task AddToRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");
        await _userManager.AddToRoleAsync(user, role);
    }

    private static UserInfoDto Map(ApplicationUser user)
    {
        return new UserInfoDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber);
    }
}
