namespace TICinema.Identity.Application.Interfaces.Services;

public interface IOtpService
{
    Task<(string Code, string Hash)> SendAsync(string identifier, string type);
    Task<bool> VerifyAsync(string identifier, string type, string code);
    string HashCode(string code);
}