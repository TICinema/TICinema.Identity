using System.Security.Cryptography;
using System.Text;
using TICinema.Identity.Application.Interfaces;
using TICinema.Identity.Application.Interfaces.Services;

namespace TICinema.Identity.Application.Services;

public class OtpService(ICacheService cacheService) : IOtpService
{
    public async Task<(string Code, string Hash)> SendAsync(string identifier, string type)
    {
        var (code, hash) = GenerateCode();
    
        // ДЛЯ АВТОРИЗАЦИИ: сохраняем в Redis, чтобы метод VerifyAsync работал
        string cacheKey = $"otp:{type}:{identifier}";
        await cacheService.SendAsync(cacheKey, hash, TimeSpan.FromMinutes(5));
    
        return (code, hash);
    }

    public async Task<bool> VerifyAsync(string identifier, string type, string code)
    {
        string cacheKey = $"otp:{type}:{identifier}";
        
        var storedHash = await cacheService.GetAsync<string>(cacheKey);
        
        if (string.IsNullOrEmpty(storedHash))
        {
            throw new Exception("Код недействителен или его срок действия истек.");
        }

        string incomingHash = HashCode(code);

        if (storedHash != incomingHash)
        {
            return false;
        }
        
        await cacheService.RemoveAsync(cacheKey);

        return true;
    }

    private (string code, string hash) GenerateCode()
    {
        string code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        byte[] inputBytes = Encoding.UTF8.GetBytes(code);
        byte[] hashBytes = SHA256.HashData(inputBytes);
        
        string hashes = Convert.ToHexString(hashBytes).ToLower();
        
        return (code, hashes);
    }

    public string HashCode(string code)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(code);
        byte[] hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }
}