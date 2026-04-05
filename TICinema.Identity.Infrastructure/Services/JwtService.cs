using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TICinema.Identity.Application.Interfaces.Services;
using TICinema.Identity.Infrastructure.Configurations;

namespace TICinema.Identity.Infrastructure.Services
{
    public class JwtService(IOptions<JwtSettings> options) : IJwtService
    {
        private readonly JwtSettings _settings = options.Value;
        public (string AccessToken, string RefreshToken) GenerateTokens(string userId, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
            // Добавляем каждую роль как отдельный Claim
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
                signingCredentials: creds
            );

            string accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            string refreshToken = Guid.NewGuid().ToString().Replace("-", "");

            return (accessToken, refreshToken);
        }
        
        public ClaimsPrincipal? VerifyToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_settings.Secret);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    
                    ValidateIssuer = true,
                    ValidIssuer = _settings.Issuer,
                    
                    ValidateAudience = true,
                    ValidAudience = _settings.Audience,
                    
                    ValidateLifetime = true,
                    // ClockSkew убирает стандартную 5-минутную задержку проверки истечения токена.
                    // Это делает проверку времени жизни токена максимально точной.
                    ClockSkew = TimeSpan.Zero 
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch (Exception)
            {
                // Если токен просрочен, подпись неверна или формат нарушен — возвращаем null.
                return null;
            }
        }
    }
}
