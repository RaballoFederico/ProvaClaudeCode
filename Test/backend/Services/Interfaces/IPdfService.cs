using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface IPdfService
{
    byte[] GeneraBigliettiPdf(IReadOnlyCollection<BigliettoPdfDTO> biglietti, string codiceConferma);
    byte[] GeneraRicevutaRicaricaPdf(
        string nominativo,
        decimal importo,
        decimal saldoPrecedente,
        decimal saldoSuccessivo,
        DateTime dataOperazione,
        string causale);
}
