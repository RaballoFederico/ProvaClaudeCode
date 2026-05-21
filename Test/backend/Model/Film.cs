// DOC: Film - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Model 'Film': entita dominio mappata su tabella database.
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

    [MaxLength(2000)]
    public string? Descrizione { get; set; }

    [MaxLength(100)]
    public string? RegistaNome { get; set; }

    [MaxLength(1000)]
    public string? Cast { get; set; }

    public bool Featured { get; set; }

    public DateTime? DataRilascio { get; set; }

    [MaxLength(200)]
    public string? Genere { get; set; }

    public int? TmdbId { get; set; }

    public ICollection<Proiezione> Proiezioni { get; set; } = new List<Proiezione>();

    public ICollection<Show> Shows { get; set; } = new List<Show>();

    public ICollection<FilmCategoria> FilmsCategorie { get; set; } = new List<FilmCategoria>();
}


