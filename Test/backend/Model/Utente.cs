// DOC: Utente - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Model 'Utente': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("utenti")]
public class Utente
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    [MaxLength(30)]
    public string? ExternalProvider { get; set; }

    [MaxLength(200)]
    public string? ExternalProviderUserId { get; set; }

    [MaxLength(100)]
    public string? Nome { get; set; }

    [MaxLength(100)]
    public string? Cognome { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [Required]
    public DateTime DataRegistrazione { get; set; } = DateTime.UtcNow;

    public DateTime? DataUltimoAccesso { get; set; }

    public bool Attivo { get; set; } = true;

    public bool ConsensoNewsletter { get; set; } = false;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<Prenotazione> Prenotazioni { get; set; } = new List<Prenotazione>();

    public ICollection<UtenteRuolo> UtentiRuoli { get; set; } = new List<UtenteRuolo>();

    public ICollection<ProiezioneSalvata> ProiezioniSalvate { get; set; } = new List<ProiezioneSalvata>();

    public int? PreferredCinemaId { get; set; }

    [MaxLength(50)]
    public string? PreferredPaymentMethod { get; set; }

    [MaxLength(120)]
    public string? PreferredPaymentMethodLabel { get; set; }

    [ForeignKey(nameof(PreferredCinemaId))]
    public Cinema? PreferredCinema { get; set; }

    public CreditoUtente? Credito { get; set; }

    public ICollection<Acquisto> Acquisti { get; set; } = new List<Acquisto>();

    public ICollection<NotificaUtente> Notifiche { get; set; } = new List<NotificaUtente>();

    public ICollection<AbbonamentoUtente> Abbonamenti { get; set; } = new List<AbbonamentoUtente>();

    public ICollection<FilmRating> FilmRatings { get; set; } = new List<FilmRating>();

    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();

    public ICollection<SupportTicketMessage> SupportTicketMessages { get; set; } = new List<SupportTicketMessage>();
}


