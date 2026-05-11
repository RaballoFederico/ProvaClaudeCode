using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public enum AccountActionTokenPurpose
{
    PasswordReset = 1,
    AccountInvite = 2
}

[Table("account_action_tokens")]
public class AccountActionToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int? UtenteId { get; set; }

    [MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    public AccountActionTokenPurpose Purpose { get; set; }

    [MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente? Utente { get; set; }
}
