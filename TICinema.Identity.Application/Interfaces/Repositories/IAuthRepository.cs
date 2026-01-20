using TICinema.Identity.Domain.Entities;

namespace TICinema.Identity.Application.Interfaces.Repositories
{
    public interface IAuthRepository
    {
        Task<ApplicationUser?> FindByPhone(string phoneNumber);
        Task<ApplicationUser?> FindByEmail(string email);
        Task<ApplicationUser> Create(ApplicationUser user);
        Task Update(ApplicationUser user);
        Task AddRefreshToken(RefreshToken token);
        // IAuthRepository.cs
        Task<RefreshToken?> FindRefreshToken(string token);
        Task DeleteRefreshToken(string token);
    }
}
