using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("refresh_tokens")]
public class RefreshToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public int UtenteId { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente Utente { get; set; } = null!;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }
}
