using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("cinemas")]
public class Cinema
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Indirizzo { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Citta { get; set; } = string.Empty;

    [Required]
    public int PostiMassimi { get; set; } = 120;

    [Column(TypeName = "decimal(10,8)")]
    public decimal? Latitudine { get; set; }

    [Column(TypeName = "decimal(11,8)")]
    public decimal? Longitudine { get; set; }

    [MaxLength(20)]
    public string? CodiceLocale { get; set; }

    public ICollection<Proiezione> Proiezioni { get; set; } = new List<Proiezione>();

    public ICollection<Sala> Sale { get; set; } = new List<Sala>();
}
