namespace FilmAPI.DTO;

public class CalcoloImportoRequestDTO
{
    public int ShowId { get; set; }
    public int NumeroBiglietti { get; set; }
    public bool UsaCredito { get; set; }
}

public class CalcoloImportoDTO
{
    public decimal PrezzoUnitario { get; set; }
    public decimal Subtotale { get; set; }
    public decimal CreditoDisponibile { get; set; }
    public decimal CreditoUsato { get; set; }
    public decimal DaPagareCarta { get; set; }
}

public class PagamentoRequestDTO
{
    public int ShowId { get; set; }
    public int NumeroBiglietti { get; set; }
    public bool UsaCredito { get; set; }
    public string? PaymentIntentId { get; set; }
}

public class PagamentoResultDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal ImportoTotale { get; set; }
    public decimal CreditoUsato { get; set; }
    public decimal CartaAddebitata { get; set; }
}

public class RimborsoResultDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class StripePaymentIntentDTO
{
    public string ClientSecret { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
}

public class CreatePaymentIntentRequestDTO
{
    public decimal Importo { get; set; }
}
