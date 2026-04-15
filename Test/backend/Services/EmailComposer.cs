using FilmAPI.DTO;

namespace FilmAPI.Services;

public static class EmailComposer
{
    public static string BuildConfermaAcquistoHtml(
        string nominativo,
        string codiceConferma,
        decimal importoTotale,
        decimal creditoUsato,
        IEnumerable<BigliettoPdfDTO> biglietti)
    {
        var rows = string.Join("", biglietti.Select(b =>
            $"<tr><td>{Escape(b.FilmTitolo)}</td><td>{b.Data:dd/MM/yyyy} {b.OraInizio:hh\\:mm}</td><td>{Escape(b.NomeCinema)}</td><td>{Escape(b.Posto)}</td><td>{b.Prezzo:0.00} EUR</td></tr>"));

        return $@"
<html>
  <body style='font-family:Arial,sans-serif;color:#222'>
    <h2>Conferma acquisto FilmAPI</h2>
    <p>Ciao {Escape(nominativo)}, il tuo acquisto e stato confermato.</p>
    <p><strong>Codice conferma:</strong> {Escape(codiceConferma)}</p>
    <p><strong>Importo totale:</strong> {importoTotale:0.00} EUR<br/>
       <strong>Credito usato:</strong> {creditoUsato:0.00} EUR</p>
    <table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse'>
      <thead><tr><th>Film</th><th>Data/Ora</th><th>Cinema</th><th>Posto</th><th>Prezzo</th></tr></thead>
      <tbody>{rows}</tbody>
    </table>
    <p>In allegato trovi il PDF dei biglietti.</p>
  </body>
</html>";
    }

    private static string Escape(string value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
