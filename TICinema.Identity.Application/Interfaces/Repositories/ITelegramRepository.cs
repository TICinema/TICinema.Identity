using TICinema.Identity.Domain.Entities;

namespace TICinema.Identity.Application.Interfaces.Repositories;

public interface ITelegramRepository
{
    Task<ApplicationUser?> GetUserByTelegramIdAsync(string telegramId);
}