using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Model;

[Table("films_categorie")]
[PrimaryKey(nameof(FilmId), nameof(CategoriaId))]
public class FilmCategoria
{
    public int FilmId { get; set; }

    [ForeignKey(nameof(FilmId))]
    public Film Film { get; set; } = null!;

    public int CategoriaId { get; set; }

    [ForeignKey(nameof(CategoriaId))]
    public Categoria Categoria { get; set; } = null!;
}
