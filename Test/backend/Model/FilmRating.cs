using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("film_ratings")]
public class FilmRating
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int FilmId { get; set; }

    [Required]
    public int UtenteId { get; set; }

    [Required]
    [Range(1, 5)]
    public int Valutazione { get; set; }

    [MaxLength(500)]
    public string? Commento { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(FilmId))]
    public Film? Film { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente? Utente { get; set; }
}
