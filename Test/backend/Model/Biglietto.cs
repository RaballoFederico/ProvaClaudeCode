// DOC: Model 'Biglietto': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("biglietti")]
public class Biglietto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int AcquistoId { get; set; }

    [ForeignKey(nameof(AcquistoId))]
    public Acquisto Acquisto { get; set; } = null!;

    [Required]
    public int ShowId { get; set; }

    [ForeignKey(nameof(ShowId))]
    public Show Show { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Posto { get; set; } = string.Empty;

    [Required]
    public int SalaNumero { get; set; }

    [Required]
    [MaxLength(20)]
    public string TipologiaSala { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Prezzo { get; set; }

    [Required]
    [MaxLength(20)]
    public string CodiceUnivoco { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string CodiceHash { get; set; } = string.Empty;

    public bool Validato { get; set; } = false;

    public DateTime? DataValidazione { get; set; }

    [Required]
    public int CinemaId { get; set; }

    [ForeignKey(nameof(CinemaId))]
    public Cinema? Cinema { get; set; }

    [Required]
    [MaxLength(500)]
    public string QRCodeUrl { get; set; } = string.Empty;
}

