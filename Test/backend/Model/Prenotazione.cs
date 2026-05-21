// DOC: Prenotazione - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Model 'Prenotazione': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("prenotazioni")]
public class Prenotazione
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
    public DateTime DataPrenotazione { get; set; } = DateTime.UtcNow;

    [Required]
    public int NumeroPosti { get; set; }

    public DateTime? DataAnnullamento { get; set; }
}


