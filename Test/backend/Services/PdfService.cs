using FilmAPI.DTO;
using FilmAPI.Services.Interfaces;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FilmAPI.Services;

public class PdfService(IConfiguration configuration) : IPdfService
{
    static PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneraBigliettiPdf(IReadOnlyCollection<BigliettoPdfDTO> biglietti, string codiceConferma)
    {
        var brandName = configuration["Branding:Name"] ?? "FilmAPI";
        var primaryColor = configuration["Branding:PrimaryColor"] ?? "#0f172a";
        var accentColor = configuration["Branding:AccentColor"] ?? "#bfdbfe";
        var logoBytes = TryLoadLogo(configuration["Branding:PdfLogoUrl"]);

        return Document.Create(container =>
        {
            foreach (var b in biglietti)
            {
                var qrPayload = string.IsNullOrWhiteSpace(b.QRCodeUrl) ? b.CodiceHash : b.QRCodeUrl;
                var qrPng = CreateQrCodePng(qrPayload);

                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Grey.Darken4));

                    page.Header().Element(header =>
                    {
                        header.Background(primaryColor).Padding(16).Column(col =>
                        {
                            col.Spacing(4);
                            if (logoBytes is not null)
                                col.Item().Width(120).Height(28).Image(logoBytes);
                            col.Item().Text(brandName.ToUpperInvariant()).FontColor(Colors.White).Bold().FontSize(10);
                            col.Item().Text("Biglietto Elettronico").FontColor(Colors.White).Bold().FontSize(20);
                            col.Item().Text($"Codice acquisto: {codiceConferma}").FontColor(accentColor).FontSize(10);
                        });
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(12);

                        col.Item().PaddingTop(8).Text(b.FilmTitolo).FontSize(22).Bold().FontColor(Colors.Grey.Darken4);

                        col.Item().Row(meta =>
                        {
                            meta.RelativeItem().AlignLeft().Text($"Ticket ID: {b.CodiceUnivoco}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            meta.RelativeItem().AlignRight().Text($"Emissione: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });

                        col.Item().Row(row =>
                        {
                            row.RelativeItem(4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(14).Column(info =>
                            {
                                info.Spacing(8);
                                info.Item().Text($"Data e ora: {b.Data:dd/MM/yyyy} - {b.OraInizio:hh\\:mm}").SemiBold();
                                info.Item().Text($"Cinema: {b.NomeCinema} ({b.CodiceLocaleCinema})");
                                info.Item().Text($"Indirizzo: {b.IndirizzoCinema}");
                                info.Item().Text($"Sala: {b.SalaNumero} ({b.TipologiaSala})");
                                info.Item().Text($"Posto: {b.Posto}").SemiBold();
                            });

                            row.ConstantItem(140).PaddingLeft(10).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(qr =>
                            {
                                qr.Spacing(6);
                                qr.Item().AlignCenter().Text("Verifica").SemiBold().FontSize(10);
                                qr.Item().AlignCenter().Width(110).Height(110).Image(qrPng);
                                qr.Item().AlignCenter().Text("Scansiona all'ingresso").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                            });

                            table.Cell().Element(CellHeader).Text("Prezzo");
                            table.Cell().Element(CellHeader).Text("Codice biglietto");
                            table.Cell().Element(CellHeader).Text("Hash validazione");

                            table.Cell().Element(CellValue).Text($"{b.Prezzo:0.00} EUR").SemiBold();
                            table.Cell().Element(CellValue).Text(b.CodiceUnivoco);
                            table.Cell().Element(CellValue).Text(b.CodiceHash).FontSize(9);
                        });

                        col.Item().Background("#f8fafc").Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Text(t =>
                        {
                            t.Span("Nota: ").SemiBold();
                            t.Span("presentare questo biglietto (digitale o stampato) all'ingresso della sala.");
                        });

                        col.Item().PaddingTop(6).Text($"Supporto: {brandName}").FontSize(9).FontColor(accentColor);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span($"Documento emesso da {brandName} - valido come titolo di accesso").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });
            }
        }).GeneratePdf();
    }

    private static byte[] CreateQrCodePng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(qrData);
        return png.GetGraphic(8);
    }

    private static byte[]? TryLoadLogo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();

        try
        {
            if (trimmed.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                var idx = trimmed.IndexOf(",", StringComparison.Ordinal);
                if (idx > 0)
                {
                    var base64 = trimmed[(idx + 1)..];
                    return Convert.FromBase64String(base64);
                }
            }

            if (File.Exists(trimmed))
            {
                return File.ReadAllBytes(trimmed);
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static IContainer CellHeader(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background("#f1f5f9")
            .Padding(8)
            .DefaultTextStyle(x => x.SemiBold().FontSize(10));
    }

    private static IContainer CellValue(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8)
            .DefaultTextStyle(x => x.FontSize(10));
    }
}
