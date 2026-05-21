// DOC: EmailComposer - file del progetto; contiene logica specifica della feature/modulo.
// DOC: Service 'EmailComposer': implementa logica di business e integrazioni esterne (DB/TMDB/Stripe).
using FilmAPI.DTO;

namespace FilmAPI.Services;

public static class EmailComposer
{
    // DOC-METHOD: 'BuildRicevutaRicaricaCreditoHtml' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public static string BuildRicevutaRicaricaCreditoHtml(
        string nominativo,
        decimal importo,
        decimal saldoPrecedente,
        decimal saldoSuccessivo,
        DateTime dataOperazione,
        string causale,
        string brandName,
        string primaryColor,
        string accentColor,
        string? logoUrl,
        string? supportEmail)
    {
        var safeBrandName = string.IsNullOrWhiteSpace(brandName) ? "FilmHub" : Escape(brandName.Trim());
        var safePrimary = string.IsNullOrWhiteSpace(primaryColor) ? "#0f172a" : primaryColor.Trim();
        var safeAccent = string.IsNullOrWhiteSpace(accentColor) ? "#bfdbfe" : accentColor.Trim();
        var safeSupportEmail = Escape((supportEmail ?? string.Empty).Trim());
        var logoMarkup = string.IsNullOrWhiteSpace(logoUrl)
            ? string.Empty
            : $"<img src='{Escape(logoUrl)}' alt='{safeBrandName} logo' style='height:28px;display:block;margin-bottom:8px' />";

        return $@"
<html>
  <body style='margin:0;padding:0;background:#f3f4f6;font-family:Segoe UI,Arial,sans-serif;color:#111827'>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='width:100%;background:#f3f4f6'>
      <tr><td align='center' style='padding:16px 8px'>
        <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='max-width:680px;background:#ffffff;border:1px solid #e5e7eb;border-radius:12px;overflow:hidden;box-shadow:0 10px 30px rgba(15,23,42,.08)'>
          <tr><td style='padding:20px 18px;background:{safePrimary};color:#ffffff'>
            {logoMarkup}
            <div style='font-size:12px;opacity:.85;letter-spacing:.4px'>{safeBrandName.ToUpperInvariant()}</div>
            <div style='font-size:24px;font-weight:700;margin-top:6px'>Ricevuta ricarica credito</div>
            <div style='font-size:13px;margin-top:4px;opacity:.9'>Operazione del {dataOperazione:dd/MM/yyyy HH:mm}</div>
          </td></tr>
          <tr><td style='padding:20px 18px'>
            <p style='margin:0 0 12px 0;font-size:15px'>Gentile <strong>{Escape(nominativo)}</strong>,</p>
            <p style='margin:0 0 16px 0;font-size:14px;line-height:1.5'>la ricarica del credito e stata registrata correttamente.</p>
            <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='border:1px solid #e5e7eb;border-radius:10px'>
              <tr><td style='padding:14px 16px;border-bottom:1px solid #e5e7eb;background:#f8fafc'><strong>Dettaglio operazione</strong></td></tr>
              <tr><td style='padding:10px 16px'><strong>Importo ricarica:</strong> {FormatEuro(importo)}</td></tr>
              <tr><td style='padding:0 16px 10px 16px'><strong>Saldo precedente:</strong> {FormatEuro(saldoPrecedente)}</td></tr>
              <tr><td style='padding:0 16px 10px 16px'><strong>Nuovo saldo:</strong> {FormatEuro(saldoSuccessivo)}</td></tr>
              <tr><td style='padding:0 16px 14px 16px'><strong>Causale:</strong> {Escape(causale)}</td></tr>
            </table>
          </td></tr>
          <tr><td style='padding:0 18px 24px 18px'>
            <div style='padding:12px 14px;background:#f8fafc;border:1px solid #e5e7eb;border-radius:8px;font-size:12px;color:#4b5563;line-height:1.5;border-left:4px solid {safeAccent}'>
              Questa email conferma l'accredito sul tuo conto FilmHub.
              {(string.IsNullOrWhiteSpace(safeSupportEmail) ? "" : $" Per assistenza contatta {safeSupportEmail}.")}
            </div>
          </td></tr>
        </table>
      </td></tr>
    </table>
  </body>
</html>";
    }

    // DOC-METHOD: 'BuildConfermaAcquistoHtml' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    public static string BuildConfermaAcquistoHtml(
        string nominativo,
        string codiceConferma,
        decimal importoTotale,
        decimal creditoUsato,
        IEnumerable<BigliettoPdfDTO> biglietti,
        string brandName,
        string primaryColor,
        string accentColor,
        string? logoUrl,
        string? supportEmail)
    {
        var ticketList = biglietti.ToList();
        var dataOraInvio = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        var cartaAddebitata = Math.Max(0m, importoTotale - creditoUsato);
        var safeBrandName = string.IsNullOrWhiteSpace(brandName) ? "FilmHub" : Escape(brandName.Trim());
        var safePrimary = string.IsNullOrWhiteSpace(primaryColor) ? "#0f172a" : primaryColor.Trim();
        var safeAccent = string.IsNullOrWhiteSpace(accentColor) ? "#bfdbfe" : accentColor.Trim();
        var safeSupportEmail = Escape((supportEmail ?? string.Empty).Trim());
        var logoMarkup = string.IsNullOrWhiteSpace(logoUrl)
            ? string.Empty
            : $"<img src='{Escape(logoUrl)}' alt='{safeBrandName} logo' style='height:28px;display:block;margin-bottom:8px' />";

        var ticketCards = string.Join("", ticketList.Select(b =>
            "<div style='border:1px solid #e5e7eb;border-radius:10px;padding:12px;margin-bottom:10px;background:#ffffff'>" +
            $"<div style='font-size:15px;font-weight:700;color:#111827;margin-bottom:6px'>{Escape(b.FilmTitolo)}</div>" +
            $"<div style='font-size:13px;color:#374151;line-height:1.55'><strong>Data/Ora:</strong> {b.Data:dd/MM/yyyy} {b.OraInizio:hh\\:mm}</div>" +
            $"<div style='font-size:13px;color:#374151;line-height:1.55'><strong>Cinema:</strong> {Escape(b.NomeCinema)}</div>" +
            $"<div style='font-size:13px;color:#374151;line-height:1.55'><strong>Posto:</strong> {Escape(b.Posto)}</div>" +
            $"<div style='font-size:13px;color:#111827;line-height:1.55'><strong>Prezzo:</strong> {FormatEuro(b.Prezzo)}</div>" +
            "</div>"));

        return $@"
<html>
  <body style='margin:0;padding:0;background:#f3f4f6;font-family:Segoe UI,Arial,sans-serif;color:#111827'>
    <div style='display:none;max-height:0;overflow:hidden;opacity:0'>Pagamento confermato - biglietti allegati in PDF</div>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='width:100%;background:#f3f4f6'>
      <tr>
        <td align='center' style='padding:16px 8px'>
          <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='max-width:680px;background:#ffffff;border:1px solid #e5e7eb;border-radius:12px;overflow:hidden;box-shadow:0 10px 30px rgba(15,23,42,.08)'>
            <tr>
              <td style='padding:20px 18px;background:{safePrimary};color:#ffffff'>
          {logoMarkup}
          <div style='font-size:12px;opacity:.85;letter-spacing:.4px'>{safeBrandName.ToUpperInvariant()}</div>
          <div style='font-size:24px;font-weight:700;margin-top:6px'>Conferma acquisto</div>
          <div style='display:inline-block;margin-top:10px;padding:4px 10px;border-radius:999px;background:rgba(255,255,255,.14);font-size:12px'>Pagamento confermato</div>
          <div style='font-size:13px;margin-top:4px;opacity:.9'>Ricevuta inviata il {dataOraInvio}</div>
              </td>
            </tr>

            <tr>
              <td style='padding:20px 18px'>
          <p style='margin:0 0 12px 0;font-size:15px'>Gentile <strong>{Escape(nominativo)}</strong>,</p>
          <p style='margin:0 0 16px 0;font-size:14px;line-height:1.5'>
            il pagamento e stato completato con successo. In allegato trova i biglietti in formato PDF, pronti per l'accesso in sala.
          </p>

          <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='border:1px solid #e5e7eb;border-radius:10px'>
            <tr>
              <td style='padding:14px 16px;border-bottom:1px solid #e5e7eb;background:#f8fafc'>
                <strong>Riepilogo ordine</strong>
              </td>
            </tr>
            <tr><td style='padding:10px 16px'><strong>Codice conferma:</strong> {Escape(codiceConferma)}</td></tr>
            <tr><td style='padding:0 16px 10px 16px'><strong>Numero biglietti:</strong> {ticketList.Count}</td></tr>
            <tr><td style='padding:0 16px 10px 16px'><strong>Importo totale:</strong> {FormatEuro(importoTotale)}</td></tr>
            <tr><td style='padding:0 16px 10px 16px'><strong>Credito usato:</strong> {FormatEuro(creditoUsato)}</td></tr>
            <tr><td style='padding:0 16px 14px 16px'><strong>Addebito carta:</strong> {FormatEuro(cartaAddebitata)}</td></tr>
          </table>
              </td>
            </tr>

            <tr>
              <td style='padding:0 18px 20px 18px'>
                <div style='border:1px solid #e5e7eb;border-radius:10px;background:#f8fafc;padding:12px 12px 2px 12px'>
                  <div style='font-size:13px;font-weight:700;color:#374151;margin-bottom:10px'>Biglietti acquistati</div>
                  {ticketCards}
                </div>
              </td>
            </tr>

            <tr>
              <td style='padding:0 18px 24px 18px'>
          <div style='padding:12px 14px;background:#f8fafc;border:1px solid #e5e7eb;border-radius:8px;font-size:12px;color:#4b5563;line-height:1.5;border-left:4px solid {safeAccent}'>
            Conservi questa email e il PDF allegato fino all'accesso in sala.
            {(string.IsNullOrWhiteSpace(safeSupportEmail) ? "" : $" Per assistenza contatti {safeSupportEmail} indicando il codice conferma.")}
          </div>
          <div style='margin-top:12px;font-size:11px;color:#6b7280'>Messaggio automatico generato da {safeBrandName}. Non rispondere a questa email se non indicato dal supporto.</div>
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";
    }

    // DOC-METHOD: 'Escape' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static string Escape(string value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    // DOC-METHOD: 'FormatEuro' implementa una parte della logica backend (validazione, orchestrazione, persistenza o mapping).
    private static string FormatEuro(decimal amount) => $"{amount:0.00} EUR";
}


