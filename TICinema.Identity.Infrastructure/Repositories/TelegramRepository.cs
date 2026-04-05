using Microsoft.EntityFrameworkCore;
using TICinema.Identity.Application.Interfaces.Repositories;
using TICinema.Identity.Domain.Entities;
using TICinema.Identity.Infrastructure.Persistence;

namespace TICinema.Identity.Infrastructure.Repositories;

public class TelegramRepository(ApplicationDbContext dbContext) : ITelegramRepository
{
    public async Task<ApplicationUser?> GetUserByTelegramIdAsync(string telegramId)
    {
        return await dbContext.Users
            .AsNoTracking() 
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
    }
}