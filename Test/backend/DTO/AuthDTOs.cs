// DOC: AuthDTOs - file del progetto; contiene logica specifica della feature/modulo.
// DOC: DTO 'AuthDTOs': contratto dati per request/response API.
namespace FilmAPI.DTO;

public class LoginRequestDTO
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDTO
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UtenteDTO Utente { get; set; } = null!;
}

public class RefreshTokenRequestDTO
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordRequestDTO
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class RegistrazioneRequestDTO
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Nome { get; set; }
    public string? Cognome { get; set; }
    public string? Telefono { get; set; }
    public bool ConsensoNewsletter { get; set; }
}

public class UtenteDTO
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Nome { get; set; }
    public string? Cognome { get; set; }
    public List<string> Ruoli { get; set; } = new();
}

public class ProfiloUtenteDTO
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Nome { get; set; }
    public string? Cognome { get; set; }
    public string? Telefono { get; set; }
    public DateTime DataRegistrazione { get; set; }
    public string? MetodoPagamentoPreferito { get; set; }
    public string? MetodoPagamentoPreferitoEtichetta { get; set; }
    public List<string> Ruoli { get; set; } = new();
    public List<ProiezioneSalvataDTO> ProiezioniSalvate { get; set; } = new();
}

public class UpdateProfiloRequestDTO
{
    public string? Nome { get; set; }
    public string? Cognome { get; set; }
    public string? Telefono { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class UpdatePreferredPaymentMethodDTO
{
    public string? Metodo { get; set; }
    public string? Etichetta { get; set; }
}

public class ProiezioneSalvataDTO
{
    public int Id { get; set; }
    public int ProiezioneId { get; set; }
    public int? ShowId { get; set; }
    public string FilmTitolo { get; set; } = string.Empty;
    public string CinemaNome { get; set; } = string.Empty;
    public DateTime DataProiezione { get; set; }
    public TimeSpan OraProiezione { get; set; }
    public DateTime DataSalvataggio { get; set; }
    public bool Prenotato { get; set; }
    public int NumeroPosti { get; set; }
}

public class SalvaProiezioneRequestDTO
{
    public int ProiezioneId { get; set; }
}

public class PrenotazioneRequestDTO
{
    public int ProiezioneSalvataId { get; set; }
    public int NumeroPosti { get; set; }
}

public class UpdateRuoliRequestDTO
{
    public List<int> RuoloIds { get; set; } = new();
}

public class ExternalAuthProviderDTO
{
    public string Provider { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class ExternalAuthStartResponseDTO
{
    public string RedirectUrl { get; set; } = string.Empty;
}

public class ExternalAuthCompleteRequestDTO
{
    public string Provider { get; set; } = string.Empty;
    public string AuthCode { get; set; } = string.Empty;
}

public class ForgotPasswordRequestDTO
{
    public string Email { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public class RecoverAccountRequestDTO
{
    public string Email { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public class CompleteRecoverAccountRequestDTO
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ResetPasswordRequestDTO
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class CreateInviteRequestDTO
{
    public string Email { get; set; } = string.Empty;
    public string Ruolo { get; set; } = "PowerUser";
    public string? ReturnUrl { get; set; }
}

public class CompleteInviteRequestDTO
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}


