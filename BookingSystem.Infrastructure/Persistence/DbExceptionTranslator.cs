using BookingSystem.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Persistence;

/// <summary>
/// Translates EF Core exceptions into Application/Domain-level exceptions.
/// Ensures EF-specific exceptions never bubble up to the API layer.
/// </summary>
internal static class DbExceptionTranslator
{
    public static async Task<int> SaveChangesWithTranslationAsync(Func<Task<int>> saveChanges)
    {
        try
        {
            return await saveChanges();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException(
                "The resource was modified by another user. Please refresh and try again.", ex);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message?.Contains("IX_", StringComparison.OrdinalIgnoreCase) == true ||
            ex.InnerException?.Message?.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true ||
            ex.InnerException?.Message?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new BusinessRuleException(
                "A duplicate record already exists. The operation could not be completed.", ex);
        }
    }
}
