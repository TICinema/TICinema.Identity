using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Identity;
using TICinema.Contracts.Protos.Identity;
using TICinema.Identity.Application.DTOs.Inputs;
using TICinema.Identity.Application.Interfaces.Clients;
using TICinema.Identity.Application.Interfaces.Repositories;
using TICinema.Identity.Application.Interfaces.Services;
using TICinema.Identity.Domain.Entities;
using TICinema.Identity.Infrastructure.Configurations;

namespace TICinema.Identity.Infrastructure.Services
{
    public class TelegramService(
        IOptions<TelegramSettings> tgOptions,
        ITelegramRepository telegramRepository,
        ICacheService cacheService,
        IJwtService jwtService, // Для генерации реальных токенов
        UserManager<ApplicationUser> userManager,
        IAuthRepository authRepository,
        IUsersGrpcClient usersGrpcClient) : ITelegramService
    {
        private readonly TelegramSettings _tgSettings = tgOptions.Value;

        public string GetAuthUrl()
        {
            var builder = new UriBuilder("https://oauth.telegram.org/auth");

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["bot_id"] = _tgSettings.TelegramBotId.ToString();
            query["origin"] = _tgSettings.TelegramRedirectOrigin;
            query["request_access"] = "write";
            query["return_to"] = _tgSettings.TelegramRedirectOrigin;

            builder.Query = query.ToString();

            return builder.ToString();
        }

        public async Task<TelegramVerifyResponse> VerifyAsync(IDictionary<string, string> query)
        {
            // 1. Проверка хэша (Безопасность!)
            if (!VerifyTelegramHash(query))
                throw new Exception("Security breach: Telegram hash is invalid.");

            if (!query.TryGetValue("id", out var telegramId))
                throw new Exception("Telegram ID missing.");

            // 2. Ищем пользователя
            var existsUser = await telegramRepository.GetUserByTelegramIdAsync(telegramId);

            // 3. Если пользователь есть и у него есть телефон — логиним
            if (existsUser != null && existsUser.PhoneNumber != null)
            {
                try
                {
                    await usersGrpcClient.CreateUserAsync(existsUser.Id);
                }
                catch (Exception ex)
                {
                    throw new Exception("The user is not created.", ex);
                }
                
                var roles = await userManager.GetRolesAsync(existsUser);
                var (access, refresh) = jwtService.GenerateTokens(existsUser.Id, roles);

                return new TelegramVerifyResponse { AccessToken = access, RefreshToken = refresh };
            }

            var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLower();
            query.TryGetValue("username", out var username);

            var userData = new TelegramUserDataDto 
            { 
                TelegramId = telegramId, 
                Username = username ?? "unknown" 
            };

            await cacheService.SendAsync(
                $"telegram_session:{sessionId}",
                userData, 
                TimeSpan.FromMinutes(15)
            );

            return new TelegramVerifyResponse
            {
                Url = $"https://t.me/{_tgSettings.TelegramBotUsername}?start={sessionId}"
            };
        }

        public async Task<TelegramCompleteResponse> CompleteAsync(TelegramCompleteRequest request)
        {
            var sessionKey = $"telegram_session:{request.SessionId}";
            var cachedData = await cacheService.GetAsync<TelegramUserDataDto>(sessionKey);

            if (cachedData == null)
                throw new Exception("Сессия не найдена или истекла.");

            var user = await authRepository.FindByPhone(request.Phone);

            if (user == null)
            {
                // РЕГИСТРАЦИЯ: Создаем нового пользователя
                user = new ApplicationUser
                {
                    UserName = request.Phone, // UserName обязателен!
                    PhoneNumber = request.Phone,
                    TelegramId = cachedData.TelegramId,
                    IsPhoneVerified = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                    throw new Exception(
                        $"Ошибка при создании: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

                // Назначаем роль по умолчанию
                await userManager.AddToRoleAsync(user, "User");
            }
            else
            {
                // АВТОРИЗАЦИЯ: Обновляем существующего пользователя
                user.TelegramId = cachedData.TelegramId;
                user.IsPhoneVerified = true;
                user.UpdatedAt = DateTime.UtcNow;

                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    throw new Exception("Ошибка при обновлении данных пользователя.");
            }
            
            var roles = await userManager.GetRolesAsync(user);

            // 4. Генерируем токены (деструктуризация кортежа)
            var (accessToken, refreshToken) = jwtService.GenerateTokens(user.Id, roles);

            var consumeTokens = new TelegramConsumeResponse()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
            };

            await cacheService.SendAsync($"telegram_tokens:{request.SessionId}", consumeTokens);
            await cacheService.RemoveAsync(sessionKey);

            return new TelegramCompleteResponse()
            {
                SessionId = request.SessionId,
            };
        }

        public async Task<TelegramConsumeResponse> ConsumeAsync(TelegramConsumeRequest request)
        {
            var tokensKey = $"telegram_tokens:{request.SessionId}";
            
            var raw = await cacheService.GetAsync<TelegramConsumeResponse>(tokensKey);
            
            if (raw == null) 
                throw new Exception("Session Not Found.");
            
            await cacheService.RemoveAsync(tokensKey);

            return raw;
        }


        private bool VerifyTelegramHash(IDictionary<string, string> query)
        {
            if (!query.TryGetValue("hash", out var receivedHash)) return false;

            // 2. Подготавливаем данные: убираем хэш, сортируем ключи по алфавиту
            // и соединяем в строку через \n (как на image_164ba4.jpg)
            var dataCheckString = string.Join("\n", query
                .Where(x => x.Key != "hash")
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}"));

            // 3. Создаем Secret Key (как на скриншоте: SHA256 от "BOT_ID:BOT_TOKEN")
            // ВАЖНО: На скрине используется HEX-строка как ключ для HMAC
            string secretKeySource = $"{_tgSettings.TelegramBotId}:{_tgSettings.TelegramBotToken}";

            using var sha256 = SHA256.Create();
            byte[] secretKeyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretKeySource));

            // 4. Вычисляем HMAC-SHA256 (image_164bc5.png)
            using var hmac = new HMACSHA256(secretKeyBytes);
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
            string calculatedHash = Convert.ToHexString(hashBytes).ToLower();

            // 5. Сравниваем результат
            return calculatedHash == receivedHash;
        }
    }
}