using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TICinema.Identity.Infrastructure.Configurations
{
    public class IdentitySettings
    {
        public const string SectionName = "IdentitySettings";

        [Required, MinLength(32)]
        public string JwtSecret { get; set; } = string.Empty;

        [Required]
        public string RedisConnection { get; init; } = string.Empty;

        [Range(1, 1000)]
        public int TokenExpirationDays { get; init; }
    }
}
