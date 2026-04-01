using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("utenti")]
public class Utente
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Nome { get; set; }

    [MaxLength(100)]
    public string? Cognome { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [Required]
    public DateTime DataRegistrazione { get; set; } = DateTime.UtcNow;

    public DateTime? DataUltimoAccesso { get; set; }

    public bool Attivo { get; set; } = true;

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }

    public ICollection<UtenteRuolo> UtentiRuoli { get; set; } = new List<UtenteRuolo>();

    public ICollection<ProiezioneSalvata> ProiezioniSalvate { get; set; } = new List<ProiezioneSalvata>();
}
