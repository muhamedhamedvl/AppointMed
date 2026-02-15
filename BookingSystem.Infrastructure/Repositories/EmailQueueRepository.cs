using BookingSystem.Application.Interfaces.Repositories;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure.Data;
using BookingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Repositories;

public class EmailQueueRepository : IEmailQueueRepository
{
    private readonly ApplicationDbContext _context;

    public EmailQueueRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmailQueue> AddAsync(EmailQueue email)
    {
        await _context.EmailQueues.AddAsync(email);
        return email;
    }

    public async Task<IEnumerable<EmailQueue>> GetPendingAsync(int take)
    {
        return await _context.EmailQueues
            .Where(e => e.Status == EmailStatus.Pending && e.RetryCount < e.MaxRetries)
            .OrderBy(e => e.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public Task UpdateAsync(EmailQueue email)
    {
        _context.EmailQueues.Update(email);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await DbExceptionTranslator.SaveChangesWithTranslationAsync(() => _context.SaveChangesAsync());
    }
}
