// DOC: DTO 'AbbonamentiNewsletterDTO': contratto dati per request/response API.
namespace FilmAPI.DTO;

public class PianoAbbonamentoDTO
{
    public string Codice { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public decimal PrezzoMensile { get; set; }
    public int IngressiSettimanali { get; set; }
    public bool Include3D { get; set; }
    public bool IncludeScontoSnack { get; set; }
}

public class AttivaAbbonamentoRequestDTO
{
    public string Piano { get; set; } = string.Empty;
}

public class AbbonamentoCheckoutSessionRequestDTO
{
    public string Piano { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public string? PaymentMethodType { get; set; }
}

public class ConfermaAbbonamentoCheckoutRequestDTO
{
    public string Piano { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}

public class RegistraUtilizzoAbbonamentoRequestDTO
{
    public int? ShowId { get; set; }
    public string? Note { get; set; }
}

public class NewsletterPreferenceRequestDTO
{
    public bool Consenso { get; set; }
}

public class NewsletterPublicSubscribeRequestDTO
{
    public string Email { get; set; } = string.Empty;
}

public class NewsletterCampagnaRequestDTO
{
    public string Oggetto { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
}

