// DOC: Show - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Model 'Show': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public enum StatoShow
{
    PROGRAMMATO = 0,
    IN_CORSO = 1,
    TERMINATO = 2,
    CANCELLATO = 3
}

[Table("shows")]
public class Show
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int SalaId { get; set; }

    [ForeignKey(nameof(SalaId))]
    public Sala? Sala { get; set; }

    [Required]
    public int FilmId { get; set; }

    [ForeignKey(nameof(FilmId))]
    public Film? Film { get; set; }

    [Required]
    public DateOnly Data { get; set; }

    [Required]
    public TimeOnly OraInizio { get; set; }

    [Required]
    public TimeOnly OraFine { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PrezzoBase { get; set; }

    [Required]
    public StatoShow Stato { get; set; } = StatoShow.PROGRAMMATO;

    public ICollection<Biglietto> Biglietti { get; set; } = new List<Biglietto>();
    public ICollection<PrenotazioneTemporanea> PrenotazioniTemporanee { get; set; } = new List<PrenotazioneTemporanea>();
    public ICollection<ProiezioneSalvata> ProiezioniSalvateLegacy { get; set; } = new List<ProiezioneSalvata>();
    public ICollection<Prenotazione> PrenotazioniLegacy { get; set; } = new List<Prenotazione>();
}


