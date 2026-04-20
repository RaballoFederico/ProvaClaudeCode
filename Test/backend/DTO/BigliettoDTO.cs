namespace FilmAPI.DTO;

public class PostoDTO
{
    public int Fila { get; set; }
    public int Numero { get; set; }

    public override string ToString() => $"Fila {Fila}, Posto {Numero}";
}

public class PostoStatoDTO
{
    public string Posto { get; set; } = string.Empty;
    public string Stato { get; set; } = string.Empty;
}

public class PrenotazioneTempDTO
{
    public string CodiceTemporaneo { get; set; } = string.Empty;
    public int ShowId { get; set; }
    public List<PostoDTO> Posti { get; set; } = new();
    public DateTime DataScadenza { get; set; }
}

public class LockPostiRequestDTO
{
    public int ShowId { get; set; }
    public List<PostoDTO> Posti { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
}

public class RinnovaLockRequestDTO
{
    public string CodiceTemporaneo { get; set; } = string.Empty;
}

public class LockDettaglioDTO
{
    public string CodiceTemporaneo { get; set; } = string.Empty;
    public int ShowId { get; set; }
    public List<PostoDTO> Posti { get; set; } = new();
    public DateTime DataScadenza { get; set; }
}

public class ConfermaAcquistoDTO
{
    public string CodiceTemporaneo { get; set; } = string.Empty;
    public bool UsaCredito { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? PaymentMethodType { get; set; }
    public string? PaymentMethodLabel { get; set; }
    public bool SavePaymentMethodForFuture { get; set; }
}

public class AcquistoResultDTO
{
    public int AcquistoId { get; set; }
    public string CodiceConferma { get; set; } = string.Empty;
    public decimal ImportoTotale { get; set; }
    public decimal CreditoUsato { get; set; }
    public List<BigliettoDTO> Biglietti { get; set; } = new();
}

public class BigliettoDTO
{
    public int Id { get; set; }
    public int AcquistoId { get; set; }
    public int ShowId { get; set; }
    public string Posto { get; set; } = string.Empty;
    public int SalaNumero { get; set; }
    public string TipologiaSala { get; set; } = string.Empty;
    public decimal Prezzo { get; set; }
    public string CodiceUnivoco { get; set; } = string.Empty;
    public string CodiceHash { get; set; } = string.Empty;
    public bool Validato { get; set; }
    public DateTime? DataValidazione { get; set; }
    public int CinemaId { get; set; }
    public string QRCodeUrl { get; set; } = string.Empty;
}

public class BigliettoValidazioneDTO
{
    public int BigliettoId { get; set; }
    public string FilmTitolo { get; set; } = string.Empty;
    public string CinemaNome { get; set; } = string.Empty;
    public int SalaNumero { get; set; }
    public string TipologiaSala { get; set; } = string.Empty;
    public DateOnly Data { get; set; }
    public TimeOnly OraInizio { get; set; }
    public string Posto { get; set; } = string.Empty;
    public string CodiceHash { get; set; } = string.Empty;
    public bool GiaValidato { get; set; }
}
