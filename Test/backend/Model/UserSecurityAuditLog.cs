// DOC: Model 'UserSecurityAuditLog': entita dominio mappata su tabella database.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

[Table("user_security_audit_logs")]
public class UserSecurityAuditLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int? ActorUserId { get; set; }
    public int? TargetUserId { get; set; }

    [MaxLength(80)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Outcome { get; set; } = "success";

    [MaxLength(120)]
    public string? Email { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

