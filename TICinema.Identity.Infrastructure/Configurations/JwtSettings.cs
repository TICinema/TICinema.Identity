using System;
using System.Collections.Generic;
using System.Text;

namespace TICinema.Identity.Infrastructure.Configurations
{
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";
        public string Secret { get; init; } = string.Empty;
        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public int ExpiryMinutes
        {
            get; init;
        }
    }
}
