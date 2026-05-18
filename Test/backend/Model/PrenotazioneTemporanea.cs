// DOC: Model 'PrenotazioneTemporanea': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public enum StatoPrenotazioneTemp
{
    ATTIVA = 0,
    CONFERMATA = 1,
    SCADUTA = 2,
    CANCELLATA = 3
}

[Table("prenotazioni_temporanee")]
public class PrenotazioneTemporanea
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(36)]
    public string CodiceTemporaneo { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public int ShowId { get; set; }

    [ForeignKey(nameof(ShowId))]
    public Show Show { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Posto { get; set; } = string.Empty;

    [Required]
    public int UtenteId { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente Utente { get; set; } = null!;

    [Required]
    public DateTime DataCreazione { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime DataScadenza { get; set; }

    [Required]
    public StatoPrenotazioneTemp Stato { get; set; } = StatoPrenotazioneTemp.ATTIVA;

    [Required]
    [MaxLength(50)]
    public string SessionId { get; set; } = string.Empty;
}

