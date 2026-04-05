using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;


namespace TICinema.Identity.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Column("is_phone_verified")]
        public bool IsPhoneVerified { get; set; } = false;

        [Column("is_email_verified")]
        public bool IsEmailVerified { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? TelegramId { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<RefreshToken> RefreshTokens { get; set; } = null!;
        
        public virtual ICollection<PendingContactChange> PendingContactChanges { get; set; } = new List<PendingContactChange>();
    }
}
