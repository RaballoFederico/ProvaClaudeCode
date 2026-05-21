// DOC: PagamentoDTO - file del progetto; contiene logica specifica della feature/modulo.
// DOC: DTO 'PagamentoDTO': contratto dati per request/response API.
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

public class StripeCheckoutSessionDTO
{
    public string SessionId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class CreateCheckoutSessionRequestDTO
{
    public decimal Importo { get; set; }
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string? PaymentMethodType { get; set; }
}

public class StripeCheckoutVerificationDTO
{
    public bool Success { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
}

public class StripeWebhookResultDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string CheckoutSessionId { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public decimal? Amount { get; set; }
    public bool AlreadyProcessed { get; set; }
}


