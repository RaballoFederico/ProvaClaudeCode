// DOC: Sala - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Model 'Sala': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public enum TipologiaSala
{
    ISENSE = 0,
    XL = 1,
    TRE_D = 2,
    DUE_D = 3
}

[Table("sale")]
public class Sala
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int CinemaId { get; set; }

    [ForeignKey(nameof(CinemaId))]
    public Cinema? Cinema { get; set; }

    [Required]
    public int NumeroSala { get; set; }

    [MaxLength(100)]
    public string? Nome { get; set; }

    [Required]
    public TipologiaSala Tipologia { get; set; }

    [Required]
    public int NumeroFile { get; set; }

    public int? PostiPerFila { get; set; }

    [Required]
    public int PostiTotali { get; set; }

    [MaxLength(2000)]
    public string? ConfigurazionePosti { get; set; }

    public bool Attiva { get; set; } = true;

    public ICollection<Show> Shows { get; set; } = new List<Show>();
}


