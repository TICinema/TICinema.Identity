namespace TICinema.Identity.Application.Interfaces.Services;

public interface IAccountService
{
    Task<bool> InitEmailChangeAsync(string userId, string newEmail);
    Task<bool> ConfirmEmailChangeAsync(string userId, string newEmail, string code);
    Task<bool> InitPhoneChangeAsync(string userId, string newPhone);
    Task<bool> ConfirmPhoneChangeAsync(string userId, string newPhone, string code);
}