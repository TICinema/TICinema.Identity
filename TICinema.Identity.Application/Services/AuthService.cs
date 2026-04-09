using System;
using System.Collections.Generic;
using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TICinema.Contracts.Events;
using TICinema.Identity.Application.DTOs.Inputs;
using TICinema.Identity.Application.DTOs.Outputs;
using TICinema.Identity.Application.Interfaces;
using TICinema.Identity.Application.Interfaces.Clients;
using TICinema.Identity.Application.Interfaces.Repositories;
using TICinema.Identity.Application.Interfaces.Services;
using TICinema.Identity.Domain.Entities;

namespace TICinema.Identity.Application.Services
{
    public class AuthService(
        IUsersGrpcClient usersGrpcClient,
        IPublishEndpoint publishEndpoint,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuthRepository authRepository,
        IOtpService otpService,
        IJwtService jwtService,
        ILogger<AuthService> logger) : IAuthService
    {
        public async Task<bool> SendOtpAsync(SendOtpDto dto)
        {
            ApplicationUser? account = dto.Type.ToLower() switch
            {
                "phone" => await authRepository.FindByPhone(dto.Identifier),
                "email" => await authRepository.FindByEmail(dto.Identifier),
                _ => throw new ArgumentException("Неверный тип идентификатора")
            };

            if (account == null)
            {
                account = new ApplicationUser
                {
                    PhoneNumber = dto.Type == "phone" ? dto.Identifier : null,
                    Email = dto.Type == "email" ? dto.Identifier : null,
                    UserName = dto.Identifier,
                    CreatedAt = DateTime.UtcNow
                };

                await userManager.CreateAsync(account);
                await userManager.AddToRoleAsync(account, "User");
            }

            var (code, otpHash) = await otpService.SendAsync(dto.Identifier, dto.Type);

            Console.WriteLine($"[DEBUG] Код отправки: {code}");

            await publishEndpoint.Publish(new OtpRequestedEvent
            {
                Identifier = dto.Identifier, // Использовал dto вместо request
                Type = dto.Type,
                Code = code
            });

            Console.WriteLine($"Событие OtpRequestedEvent отправлено в RabbitMQ для {dto.Identifier}");

            return true;
        }

        public async Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpDto dto)
        {
            var isCodeValid = await otpService.VerifyAsync(dto.Identifier, dto.Type, dto.Code);
            if (!isCodeValid) throw new Exception("Неверный код или срок его действия истек.");

            var account = dto.Type.ToLower() switch
            {
                "phone" => await authRepository.FindByPhone(dto.Identifier),
                "email" => await authRepository.FindByEmail(dto.Identifier),
                _ => throw new ArgumentException("Неверный тип")
            };
            if (account == null) throw new Exception("Аккаунт не найден.");

            bool isUpdated = false;

            if (dto.Type == "phone" && !account.IsPhoneVerified)
            {
                account.IsPhoneVerified = true;
                isUpdated = true;
            }
            else if (dto.Type == "email" && !account.IsEmailVerified)
            {
                account.IsEmailVerified = true;
                isUpdated = true;
            }

            if (isUpdated)
            {
                await authRepository.Update(account);

                try
                {
                    await usersGrpcClient.CreateUserAsync(account.Id);
                    logger.LogInformation("Профиль пользователя {UserId} создан после верификации", account.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при создании профиля в User Service для {UserId}", account.Id);
                }
            }

            var roles = await userManager.GetRolesAsync(account);
            var (accessToken, refreshToken) = jwtService.GenerateTokens(account.Id, roles);

            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                UserId = account.Id
            };
            await authRepository.AddRefreshToken(refreshTokenEntity);

            return new VerifyOtpResponse(accessToken, refreshToken);
        }

        // AuthService.cs
        public async Task<VerifyOtpResponse> RefreshAsync(string refreshToken)
        {
            // 1. Ищем токен в базе
            var tokenEntity = await authRepository.FindRefreshToken(refreshToken);

            // 2. Проверяем валидность
            if (tokenEntity == null || !tokenEntity.IsActive)
            {
                throw new Exception("Невалидный или просроченный Refresh Token");
            }

            var user = tokenEntity.User;

            var roles = await userManager.GetRolesAsync(user);

            var (newAccessToken, newRefreshTokenString) = jwtService.GenerateTokens(user.Id, roles);

            // 4. Удаляем старый токен (Rotation policy - один токен живет один раз)
            await authRepository.DeleteRefreshToken(refreshToken);

            // 5. Сохраняем новый токен
            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newRefreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                UserId = user.Id
            };
            await authRepository.AddRefreshToken(newRefreshTokenEntity);

            return new VerifyOtpResponse(newAccessToken, refreshToken);
        }

        public async Task<bool> AssignRoleAsync(string userId, string roleName)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Проверяем, существует ли такая роль
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // Назначаем роль (если её еще нет у пользователя)
            if (!await userManager.IsInRoleAsync(user, roleName))
            {
                var result = await userManager.AddToRoleAsync(user, roleName);
                return result.Succeeded;
            }

            return true;
        }
    }
}