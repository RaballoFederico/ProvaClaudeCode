using FilmAPI.DTO;
using FilmAPI.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FilmAPI.Services;

public class PdfService : IPdfService
{
    static PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneraBigliettiPdf(IReadOnlyCollection<BigliettoPdfDTO> biglietti, string codiceConferma)
    {
        return Document.Create(container =>
        {
            foreach (var b in biglietti)
            {
                container.Page(page =>
                {
                    page.Margin(24);
                    page.Size(PageSizes.A4);
                    page.Header().Text("FilmAPI - Biglietto Elettronico").FontSize(18).Bold();
                    page.Content().Column(col =>
                    {
                        col.Spacing(6);
                        col.Item().Text(b.FilmTitolo).FontSize(20).Bold();
                        col.Item().Text($"Data: {b.Data:dd/MM/yyyy} - {b.OraInizio:hh\\:mm}");
                        col.Item().Text($"Sala: {b.SalaNumero} ({b.TipologiaSala}), {b.Posto}");
                        col.Item().Text($"Nome Locale: {b.NomeCinema}");
                        col.Item().Text($"Codice Locale: {b.CodiceLocaleCinema}");
                        col.Item().Text($"Indirizzo Locale: {b.IndirizzoCinema}");
                        col.Item().Text($"Prezzo Totale: {b.Prezzo:0.00} EUR");
                        col.Item().Text($"Codice Acquisto: {codiceConferma}").Bold();
                        col.Item().Text($"Codice Biglietto: {b.CodiceUnivoco}");
                        col.Item().Text($"QR Hash: {b.CodiceHash}").FontSize(10).FontColor(Colors.Grey.Darken1);
                    });
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Documento generato da FilmAPI").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });
            }
        }).GeneratePdf();
    }
}
