using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public enum SupportTicketCategory
{
    Account = 0,
    Pagamento = 1,
    Prenotazione = 2,
    Biglietto = 3,
    Bug = 4,
    Altro = 5
}

public enum SupportTicketPriority
{
    Bassa = 0,
    Media = 1,
    Alta = 2,
    Urgente = 3
}

public enum SupportTicketStatus
{
    Aperto = 0,
    InLavorazione = 1,
    InAttesaUtente = 2,
    Chiuso = 3
}

[Table("support_tickets")]
public class SupportTicket
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UtenteId { get; set; }

    public int? AssegnatoAId { get; set; }

    [Required]
    [MaxLength(160)]
    public string Oggetto { get; set; } = string.Empty;

    [Required]
    public SupportTicketCategory Categoria { get; set; } = SupportTicketCategory.Altro;

    [Required]
    public SupportTicketPriority Priorita { get; set; } = SupportTicketPriority.Media;

    [Required]
    public SupportTicketStatus Stato { get; set; } = SupportTicketStatus.Aperto;

    public DateTime CreatoIl { get; set; } = DateTime.UtcNow;

    public DateTime AggiornatoIl { get; set; } = DateTime.UtcNow;

    public DateTime? ChiusoIl { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente? Utente { get; set; }

    [ForeignKey(nameof(AssegnatoAId))]
    public Utente? AssegnatoA { get; set; }

    public ICollection<SupportTicketMessage> Messaggi { get; set; } = new List<SupportTicketMessage>();
}
