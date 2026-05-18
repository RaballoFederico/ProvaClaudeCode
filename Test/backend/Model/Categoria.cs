// DOC: Model 'Categoria': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("categorie")]
public class Categoria
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descrizione { get; set; }

    public ICollection<FilmCategoria> FilmsCategorie { get; set; } = new List<FilmCategoria>();
}

