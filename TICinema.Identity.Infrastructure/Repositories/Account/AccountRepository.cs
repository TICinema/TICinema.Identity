using Microsoft.EntityFrameworkCore;
using TICinema.Identity.Application.Interfaces.Repositories;
using TICinema.Identity.Domain.Entities;
using TICinema.Identity.Infrastructure.Persistence;

namespace TICinema.Identity.Infrastructure.Repositories.Account;

public class AccountRepository(ApplicationDbContext dbContext) : IAccountRepository
{
    public async Task<PendingContactChange?> FindPendingContactChange(string accountId, string type)
        => await dbContext.PendingContactChanges.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == accountId && p.Type == type);
    
    public async Task UpsertPendingContactChange(PendingContactChange data)
    {
        var existing = await dbContext.PendingContactChanges
            .FirstOrDefaultAsync(p => p.UserId == data.UserId && p.Type == data.Type);

        if (existing == null)
        {
            await dbContext.PendingContactChanges.AddAsync(data);
        }
        else
        {
            // Обновляем существующую запись
            existing.Value = data.Value;
            existing.CodeHash = data.CodeHash;
            existing.ExpiresAt = data.ExpiresAt;
            existing.UpdatedAt = DateTime.UtcNow;
        
            dbContext.PendingContactChanges.Update(existing);
        }

        await dbContext.SaveChangesAsync();
    }
    
    public async Task DeletePendingContactChange(string accountId, string type)
    {
        await dbContext.PendingContactChanges
            .Where(p => p.UserId == accountId && p.Type == type)
            .ExecuteDeleteAsync(); // Доступно в .NET 8+ и 10
    }
}