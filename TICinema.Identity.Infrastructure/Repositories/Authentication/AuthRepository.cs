using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TICinema.Identity.Application.Interfaces;
using TICinema.Identity.Application.Interfaces.Repositories;
using TICinema.Identity.Domain.Entities;
using TICinema.Identity.Infrastructure.Persistence;

namespace TICinema.Identity.Infrastructure.Repositories.Authentication
{
    public class AuthRepository(ApplicationDbContext dbContext) : IAuthRepository
    {
        public async Task<ApplicationUser?> FindByEmail(string email) => await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        
        public async Task<ApplicationUser?> FindByPhone(string phoneNumber) => await dbContext.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

        public async Task<ApplicationUser> Create(ApplicationUser user)
        {
            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            return user;
        }

        public async Task Update(ApplicationUser user)
        {
            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync();
        }

        public async Task AddRefreshToken(RefreshToken token)
        {
            await dbContext.RefreshTokens.AddAsync(token);
            await dbContext.SaveChangesAsync();
        }
        
        public async Task<RefreshToken?> FindRefreshToken(string token)
        {
            return await dbContext.RefreshTokens
                .Include(t => t.User) // Подгружаем пользователя, чтобы знать кому выдать новый access_token
                .FirstOrDefaultAsync(t => t.Token == token);
        }

        public async Task DeleteRefreshToken(string token)
        {
            var tokenEntity = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (tokenEntity != null)
            {
                dbContext.RefreshTokens.Remove(tokenEntity);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
