// DOC: Model 'UtenteRuolo': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Model;

[Table("utenti_ruoli")]
[PrimaryKey(nameof(UtenteId), nameof(RuoloId))]
public class UtenteRuolo
{
    public int UtenteId { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente Utente { get; set; } = null!;

    public int RuoloId { get; set; }

    [ForeignKey(nameof(RuoloId))]
    public Ruolo Ruolo { get; set; } = null!;
}

