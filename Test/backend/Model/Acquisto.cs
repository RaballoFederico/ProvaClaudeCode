// DOC: Model 'Acquisto': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public enum StatoAcquisto
{
    PAGATO = 0,
    CANCELLED = 1,
    REFUNDED = 2
}

[Table("acquisti")]
public class Acquisto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UtenteId { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente Utente { get; set; } = null!;

    [Required]
    public int ShowId { get; set; }

    [ForeignKey(nameof(ShowId))]
    public Show Show { get; set; } = null!;

    [Required]
    public DateTime DataAcquisto { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ImportoTotale { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal CreditoUsato { get; set; }

    [MaxLength(100)]
    public string? StripeChargeId { get; set; }

    [MaxLength(50)]
    public string? MetodoPagamento { get; set; }

    [MaxLength(120)]
    public string? MetodoPagamentoEtichetta { get; set; }

    public bool MetodoPagamentoSalvato { get; set; }

    [Required]
    public StatoAcquisto Stato { get; set; } = StatoAcquisto.PAGATO;

    [Required]
    [MaxLength(36)]
    public string CodiceConferma { get; set; } = Guid.NewGuid().ToString();

    public ICollection<Biglietto> Biglietti { get; set; } = new List<Biglietto>();
}

