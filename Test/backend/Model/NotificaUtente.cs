// DOC: Model 'NotificaUtente': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("notifiche_utente")]
public class NotificaUtente
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UtenteId { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente Utente { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Tipo { get; set; } = "info";

    [Required]
    [MaxLength(140)]
    public string Titolo { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Messaggio { get; set; }

    [MaxLength(500)]
    public string? Url { get; set; }

    [MaxLength(120)]
    public string? DedupeKey { get; set; }

    public bool Letta { get; set; }

    public DateTime DataCreazione { get; set; } = DateTime.UtcNow;
}

