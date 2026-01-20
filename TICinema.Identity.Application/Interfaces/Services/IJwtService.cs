namespace TICinema.Identity.Application.Interfaces.Services
{
    public interface IJwtService
    {
        (string AccessToken, string RefreshToken) GenerateTokens(string userId, IEnumerable<string> roles);
    }
}
