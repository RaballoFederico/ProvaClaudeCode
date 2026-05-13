using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("newsletter_campagne")]
public class NewsletterCampagna
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string Oggetto { get; set; } = string.Empty;

    [Required]
    public string HtmlBody { get; set; } = string.Empty;

    [Required]
    public int CreatoDaUtenteId { get; set; }

    [ForeignKey(nameof(CreatoDaUtenteId))]
    public Utente CreatoDaUtente { get; set; } = null!;

    [Required]
    public DateTime DataInvio { get; set; } = DateTime.UtcNow;

    [Required]
    public int DestinatariCount { get; set; }
}
