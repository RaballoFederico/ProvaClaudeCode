using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("support_ticket_messages")]
public class SupportTicketMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int SupportTicketId { get; set; }

    [Required]
    public int AutoreId { get; set; }

    [Required]
    public bool Staff { get; set; }

    [Required]
    [MaxLength(4000)]
    public string Messaggio { get; set; } = string.Empty;

    public DateTime CreatoIl { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SupportTicketId))]
    public SupportTicket? Ticket { get; set; }

    [ForeignKey(nameof(AutoreId))]
    public Utente? Autore { get; set; }
}
