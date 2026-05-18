// DOC: Model 'ProiezioneSalvata': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("proiezioni_salvate")]
public class ProiezioneSalvata
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UtenteId { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente Utente { get; set; } = null!;

    [Required]
    public int ProiezioneId { get; set; }

    [ForeignKey(nameof(ProiezioneId))]
    public Proiezione Proiezione { get; set; } = null!;

    [Required]
    public DateTime DataSalvataggio { get; set; } = DateTime.UtcNow;

    public bool Prenotato { get; set; } = false;

    public DateTime? DataPrenotazione { get; set; }

    public int NumeroPosti { get; set; } = 0;
}

