// DOC: TransazioneCredito - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Model 'TransazioneCredito': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public enum TipoTransazione
{
    RICARICA = 0,
    ACQUISTO = 1,
    RIMBORSO = 2
}

[Table("transazioni_credito")]
public class TransazioneCredito
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UtenteId { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente Utente { get; set; } = null!;

    [Required]
    public TipoTransazione Tipo { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Importo { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal SaldoPrecedente { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal SaldoSuccessivo { get; set; }

    [Required]
    public DateTime DataTransazione { get; set; } = DateTime.UtcNow;

    public int? OperatoreId { get; set; }

    [ForeignKey(nameof(OperatoreId))]
    public Utente? Operatore { get; set; }

    public int? CinemaId { get; set; }

    [ForeignKey(nameof(CinemaId))]
    public Cinema? Cinema { get; set; }

    [MaxLength(500)]
    public string? Descrizione { get; set; }

    public int? AcquistoId { get; set; }

    [ForeignKey(nameof(AcquistoId))]
    public Acquisto? Acquisto { get; set; }
}


