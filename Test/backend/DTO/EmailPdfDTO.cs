// DOC: DTO 'EmailPdfDTO': contratto dati per request/response API.
namespace FilmAPI.DTO;

public class BigliettoPdfDTO
{
    public int BigliettoId { get; set; }
    public string FilmTitolo { get; set; } = string.Empty;
    public DateOnly Data { get; set; }
    public TimeOnly OraInizio { get; set; }
    public string NomeCinema { get; set; } = string.Empty;
    public string CodiceLocaleCinema { get; set; } = string.Empty;
    public string IndirizzoCinema { get; set; } = string.Empty;
    public int SalaNumero { get; set; }
    public string TipologiaSala { get; set; } = string.Empty;
    public string Posto { get; set; } = string.Empty;
    public decimal Prezzo { get; set; }
    public string CodiceUnivoco { get; set; } = string.Empty;
    public string CodiceHash { get; set; } = string.Empty;
    public string QRCodeUrl { get; set; } = string.Empty;
}

