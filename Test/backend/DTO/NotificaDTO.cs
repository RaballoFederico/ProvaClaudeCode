// DOC: DTO 'NotificaDTO': contratto dati per request/response API.
namespace FilmAPI.DTO;

public class NotificaDTO
{
    public int Id { get; set; }
    public string Tipo { get; set; } = "info";
    public string Titolo { get; set; } = string.Empty;
    public string? Messaggio { get; set; }
    public string? Url { get; set; }
    public string? DedupeKey { get; set; }
    public bool Letta { get; set; }
    public DateTime DataCreazione { get; set; }
}

public class CreaNotificaRequestDTO
{
    public string Tipo { get; set; } = "info";
    public string Titolo { get; set; } = string.Empty;
    public string? Messaggio { get; set; }
    public string? Url { get; set; }
    public string? DedupeKey { get; set; }
}

