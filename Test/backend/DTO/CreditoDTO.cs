namespace FilmAPI.DTO;

public class RicaricaCreditoDTO
{
    public int UtenteId { get; set; }
    public decimal Importo { get; set; }
    public string? Descrizione { get; set; }
    public int? CinemaId { get; set; }
}

public class TransazioneCreditoDTO
{
    public int Id { get; set; }
    public int UtenteId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Importo { get; set; }
    public decimal SaldoPrecedente { get; set; }
    public decimal SaldoSuccessivo { get; set; }
    public DateTime DataTransazione { get; set; }
    public int? OperatoreId { get; set; }
    public int? CinemaId { get; set; }
    public string? Descrizione { get; set; }
    public int? AcquistoId { get; set; }
}

public class TransazioneFilterDTO
{
    public int? UtenteId { get; set; }
    public int? Tipo { get; set; }
    public DateTime? Dal { get; set; }
    public DateTime? Al { get; set; }
    public int? CinemaId { get; set; }
}
