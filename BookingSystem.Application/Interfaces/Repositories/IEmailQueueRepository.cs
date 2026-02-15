using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.Interfaces.Repositories;

public interface IEmailQueueRepository
{
    Task<EmailQueue> AddAsync(EmailQueue email);
    Task<IEnumerable<EmailQueue>> GetPendingAsync(int take);
    Task UpdateAsync(EmailQueue email);
    Task<int> SaveChangesAsync();
}
