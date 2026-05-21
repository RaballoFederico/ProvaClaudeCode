// DOC: CreditoUtente - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Model 'CreditoUtente': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("crediti_utente")]
public class CreditoUtente
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UtenteId { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente Utente { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Saldo { get; set; } = 0;

    public DateTime DataUltimoAggiornamento { get; set; } = DateTime.UtcNow;

    public ICollection<TransazioneCredito> Transazioni { get; set; } = new List<TransazioneCredito>();
}


