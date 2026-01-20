using System.ComponentModel.DataAnnotations.Schema;

namespace TICinema.Identity.Domain.Entities;

[Table("pending_contact_changes")]
public class PendingContactChange
{
    public Guid Id { get; set; }

    [Column("type")]
    public string Type { get; set; } = string.Empty; // "Email" или "Phone"

    [Column("value")]
    public string Value { get; set; } = string.Empty; // Новое значение (новый email или телефон)

    [Column("code_hash")]
    public string CodeHash { get; set; } = string.Empty; // Хэш кода подтверждения

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    // Добавь навигационное свойство (опционально, но полезно)
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}