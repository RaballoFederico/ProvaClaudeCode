using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface IPdfService
{
    byte[] GeneraBigliettiPdf(IReadOnlyCollection<BigliettoPdfDTO> biglietti, string codiceConferma);
}
