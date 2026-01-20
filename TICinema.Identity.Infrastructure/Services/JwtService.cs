using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime;
using System.Security.Claims;
using System.Text;
using TICinema.Identity.Application.Interfaces;
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

            // 1. Генерируем Access Token
            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
                signingCredentials: creds
            );

            string accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            // 2. Генерируем Refresh Token (просто случайная строка)
            string refreshToken = Guid.NewGuid().ToString().Replace("-", "");

            return (accessToken, refreshToken);
        }
    }
}
