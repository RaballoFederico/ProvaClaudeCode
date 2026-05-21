// DOC: Ruolo - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Model 'Ruolo': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("ruoli")]
public class Ruolo
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Nome { get; set; } = string.Empty; // "Admin", "PowerUser", "User"

    [MaxLength(255)]
    public string Descrizione { get; set; } = string.Empty;

    public ICollection<UtenteRuolo> UtentiRuoli { get; set; } = new List<UtenteRuolo>();
}


