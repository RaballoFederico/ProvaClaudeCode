using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("films")]
public class Film
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Titolo { get; set; } = string.Empty;

    [Required]
    public DateTime DataProduzione { get; set; }

    [Required]
    public int RegistaId { get; set; }

    [ForeignKey(nameof(RegistaId))]
    public Regista? Regista { get; set; }

    [Required]
    public int Durata { get; set; }

    [MaxLength(500)]
    public string? CopertinaPath { get; set; }

    [MaxLength(500)]
    public string? FilmatoPath { get; set; }

    public ICollection<Proiezione> Proiezioni { get; set; } = new List<Proiezione>();
}
