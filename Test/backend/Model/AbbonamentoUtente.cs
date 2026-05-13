using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public enum TipoPianoAbbonamento
{
    Base = 0,
    Plus = 1,
    Premium = 2
}

public enum StatoAbbonamento
{
    Attivo = 0,
    Disdetto = 1,
    Scaduto = 2
}

[Table("abbonamenti_utente")]
public class AbbonamentoUtente
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UtenteId { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente Utente { get; set; } = null!;

    [Required]
    public TipoPianoAbbonamento Piano { get; set; }

    [Required]
    public StatoAbbonamento Stato { get; set; } = StatoAbbonamento.Attivo;

    [Required]
    public DateTime DataInizio { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ProssimoRinnovo { get; set; } = DateTime.UtcNow.AddMonths(1);

    public DateTime? DataDisdetta { get; set; }

    public ICollection<UtilizzoAbbonamento> Utilizzi { get; set; } = new List<UtilizzoAbbonamento>();
}

[Table("utilizzi_abbonamento")]
public class UtilizzoAbbonamento
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int AbbonamentoUtenteId { get; set; }

    [ForeignKey(nameof(AbbonamentoUtenteId))]
    public AbbonamentoUtente AbbonamentoUtente { get; set; } = null!;

    [Required]
    public DateTime DataUtilizzo { get; set; } = DateTime.UtcNow;

    public int? ShowId { get; set; }

    [MaxLength(250)]
    public string? Note { get; set; }
}
