using FilmAPI.DTO;

namespace FilmAPI.Services;

public static class EmailComposer
{
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
        var safeBrandName = string.IsNullOrWhiteSpace(brandName) ? "FilmAPI" : Escape(brandName.Trim());
        var safePrimary = string.IsNullOrWhiteSpace(primaryColor) ? "#0f172a" : primaryColor.Trim();
        var safeAccent = string.IsNullOrWhiteSpace(accentColor) ? "#bfdbfe" : accentColor.Trim();
        var safeSupportEmail = Escape((supportEmail ?? string.Empty).Trim());
        var logoMarkup = string.IsNullOrWhiteSpace(logoUrl)
            ? string.Empty
            : $"<img src='{Escape(logoUrl)}' alt='{safeBrandName} logo' style='height:28px;display:block;margin-bottom:8px' />";

        var rows = string.Join("", ticketList.Select(b =>
            $"<tr>" +
            $"<td style='padding:10px 12px;border-bottom:1px solid #e5e7eb'>{Escape(b.FilmTitolo)}</td>" +
            $"<td style='padding:10px 12px;border-bottom:1px solid #e5e7eb'>{b.Data:dd/MM/yyyy} {b.OraInizio:hh\\:mm}</td>" +
            $"<td style='padding:10px 12px;border-bottom:1px solid #e5e7eb'>{Escape(b.NomeCinema)}</td>" +
            $"<td style='padding:10px 12px;border-bottom:1px solid #e5e7eb'>{Escape(b.Posto)}</td>" +
            $"<td style='padding:10px 12px;border-bottom:1px solid #e5e7eb;text-align:right;white-space:nowrap'>{FormatEuro(b.Prezzo)}</td>" +
            $"</tr>"));

        return $@"
<html>
  <body style='margin:0;padding:24px;background:#f3f4f6;font-family:Segoe UI,Arial,sans-serif;color:#111827'>
    <div style='display:none;max-height:0;overflow:hidden;opacity:0'>Pagamento confermato - biglietti allegati in PDF</div>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='max-width:720px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:12px;overflow:hidden;box-shadow:0 10px 30px rgba(15,23,42,.08)'>
      <tr>
        <td style='padding:20px 24px;background:{safePrimary};color:#ffffff'>
          {logoMarkup}
          <div style='font-size:12px;opacity:.85;letter-spacing:.4px'>{safeBrandName.ToUpperInvariant()}</div>
          <div style='font-size:24px;font-weight:700;margin-top:6px'>Conferma acquisto</div>
          <div style='display:inline-block;margin-top:10px;padding:4px 10px;border-radius:999px;background:rgba(255,255,255,.14);font-size:12px'>Pagamento confermato</div>
          <div style='font-size:13px;margin-top:4px;opacity:.9'>Ricevuta inviata il {dataOraInvio}</div>
        </td>
      </tr>

      <tr>
        <td style='padding:20px 24px'>
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
        <td style='padding:0 24px 20px 24px'>
          <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='border:1px solid #e5e7eb;border-radius:10px;border-collapse:separate;border-spacing:0'>
            <thead>
              <tr style='background:#f8fafc'>
                <th align='left' style='padding:10px 12px;border-bottom:1px solid #e5e7eb;font-size:12px;color:#374151'>Film</th>
                <th align='left' style='padding:10px 12px;border-bottom:1px solid #e5e7eb;font-size:12px;color:#374151'>Data / Ora</th>
                <th align='left' style='padding:10px 12px;border-bottom:1px solid #e5e7eb;font-size:12px;color:#374151'>Cinema</th>
                <th align='left' style='padding:10px 12px;border-bottom:1px solid #e5e7eb;font-size:12px;color:#374151'>Posto</th>
                <th align='right' style='padding:10px 12px;border-bottom:1px solid #e5e7eb;font-size:12px;color:#374151'>Prezzo</th>
              </tr>
            </thead>
            <tbody>{rows}</tbody>
          </table>
        </td>
      </tr>

      <tr>
        <td style='padding:0 24px 24px 24px'>
          <div style='padding:12px 14px;background:#f8fafc;border:1px solid #e5e7eb;border-radius:8px;font-size:12px;color:#4b5563;line-height:1.5;border-left:4px solid {safeAccent}'>
            Conservi questa email e il PDF allegato fino all'accesso in sala.
            {(string.IsNullOrWhiteSpace(safeSupportEmail) ? "" : $" Per assistenza contatti {safeSupportEmail} indicando il codice conferma.")}
          </div>
          <div style='margin-top:12px;font-size:11px;color:#6b7280'>Messaggio automatico generato da {safeBrandName}. Non rispondere a questa email se non indicato dal supporto.</div>
        </td>
      </tr>
    </table>
  </body>
</html>";
    }

    private static string Escape(string value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    private static string FormatEuro(decimal amount) => $"{amount:0.00} EUR";
}
