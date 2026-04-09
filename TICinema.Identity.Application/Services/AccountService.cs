using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TICinema.Contracts.Events;
using TICinema.Identity.Application.Interfaces;
using TICinema.Identity.Application.Interfaces.Repositories;
using TICinema.Identity.Application.Interfaces.Services;
using TICinema.Identity.Domain.Entities;

namespace TICinema.Identity.Application.Services;

public class AccountService(UserManager<ApplicationUser> userManager, IOtpService otpService, IAccountRepository accountRepository, IPublishEndpoint publishEndpoint, // <-- Добавили шину
    ILogger<AccountService> logger) : IAccountService
{
    // --- EMAIL ---
    public async Task<bool> InitEmailChangeAsync(string userId, string newEmail)
    {
        var existingUser = await userManager.FindByEmailAsync(newEmail);
        if (existingUser != null)
            throw new Exception("Email уже используется другим аккаунтом.");

        // 1. Генерируем код (пусть SendAsync теперь только генерирует и возвращает код/хэш)
        var (code, hash) = await otpService.SendAsync(newEmail, "email");

        // 2. Сохраняем в БД временные данные
        var pendingChange = new PendingContactChange
        {
            UserId = userId,
            Type = "email",
            Value = newEmail,
            CodeHash = hash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            UpdatedAt = DateTime.UtcNow
        };
        await accountRepository.UpsertPendingContactChange(pendingChange);

        // 3. ПУБЛИКУЕМ СОБЫТИЕ В RABBITMQ
        await publishEndpoint.Publish(new EmailChangedEvent
        {
            NewEmail = newEmail,
            Code = code
        });
        
        logger.LogInformation("СОБЫТИЕ ОПУБЛИКОВАНО!");

        logger.LogInformation("Запрос на смену Email опубликован для {Email}", newEmail);
        return true;
    }

    public async Task<bool> ConfirmEmailChangeAsync(string userId, string newEmail, string code)
    {
        var pending = await accountRepository.FindPendingContactChange(userId, "email");

        if (pending == null)
            throw new Exception("Запрос на изменение почты не найден.");

        if (pending.Value != newEmail)
            throw new Exception("Email не совпадает с запрошенным.");

        if (pending.ExpiresAt < DateTime.UtcNow)
        {
            await accountRepository.DeletePendingContactChange(userId, "email");
            throw new Exception("Код просрочен.");
        }

        if (pending.CodeHash != otpService.HashCode(code)) // Убедись, что HashCode public
            throw new Exception("Неверный код подтверждения.");

        var user = await userManager.FindByIdAsync(userId);
        if (user == null) throw new Exception("Пользователь не найден.");

        user.Email = newEmail;
        user.UserName = newEmail; 
        user.IsEmailVerified = true;

        await userManager.UpdateAsync(user);
        await accountRepository.DeletePendingContactChange(userId, "email");
        return true;
    }

    // --- PHONE ---
    public async Task<bool> InitPhoneChangeAsync(string userId, string newPhone)
    {
        // Используем AnyAsync для производительности
        var isPhoneTaken = userManager.Users.Any(u => u.PhoneNumber == newPhone);
        if (isPhoneTaken)
            throw new Exception("Этот номер телефона уже используется.");

        var (code, hash) = await otpService.SendAsync(newPhone, "phone");

        var pendingChange = new PendingContactChange
        {
            UserId = userId,
            Type = "phone",
            Value = newPhone,
            CodeHash = hash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            UpdatedAt = DateTime.UtcNow
        };

        await accountRepository.UpsertPendingContactChange(pendingChange);
        Console.WriteLine($"[DEBUG] Phone OTP for {newPhone}: {code}");
        return true;
    }

    public async Task<bool> ConfirmPhoneChangeAsync(string userId, string newPhone, string code)
    {
        var pending = await accountRepository.FindPendingContactChange(userId, "phone");

        if (pending == null || pending.Value != newPhone)
            throw new Exception("Запрос на изменение номера не найден.");

        if (pending.ExpiresAt < DateTime.UtcNow)
        {
            await accountRepository.DeletePendingContactChange(userId, "phone");
            throw new Exception("Код просрочен.");
        }

        if (pending.CodeHash != otpService.HashCode(code))
            throw new Exception("Неверный код.");

        var user = await userManager.FindByIdAsync(userId);
        if (user == null) throw new Exception("Пользователь не найден.");

        user.PhoneNumber = newPhone;
        user.IsPhoneVerified = true;

        await userManager.UpdateAsync(user);
        await accountRepository.DeletePendingContactChange(userId, "phone");
        return true;
    }
}