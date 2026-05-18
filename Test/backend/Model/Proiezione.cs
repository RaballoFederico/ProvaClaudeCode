// DOC: Model 'Proiezione': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("proiezioni")]
public class Proiezione
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int? ShowId { get; set; }

    [ForeignKey(nameof(ShowId))]
    public Show? Show { get; set; }

    [Required]
    public int CinemaId { get; set; }

    [ForeignKey(nameof(CinemaId))]
    public Cinema? Cinema { get; set; }

    [Required]
    public int FilmId { get; set; }

    [ForeignKey(nameof(FilmId))]
    public Film? Film { get; set; }

    [Required]
    public DateTime Data { get; set; }

    [Required]
    public TimeSpan Ora { get; set; }
}

