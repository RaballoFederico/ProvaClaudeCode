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

    public ICollection<Proiezione> Proiezioni { get; set; } = new List<Proiezione>();
}
