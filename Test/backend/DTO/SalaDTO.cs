// DOC: DTO 'SalaDTO': contratto dati per request/response API.
namespace FilmAPI.DTO;

public class SalaDTO
{
    public int Id { get; set; }
    public int CinemaId { get; set; }
    public int NumeroSala { get; set; }
    public string? Nome { get; set; }
    public string Tipologia { get; set; } = string.Empty;
    public int NumeroFile { get; set; }
    public int? PostiPerFila { get; set; }
    public int PostiTotali { get; set; }
    public string? ConfigurazionePosti { get; set; }
    public bool Attiva { get; set; }
}

public class SalaCreateDTO
{
    public int NumeroSala { get; set; }
    public string? Nome { get; set; }
    public int Tipologia { get; set; }
    public int NumeroFile { get; set; }
    public int? PostiPerFila { get; set; }
    public string? ConfigurazionePosti { get; set; }
}

public class SalaUpdateDTO
{
    public string? Nome { get; set; }
    public int Tipologia { get; set; }
    public int NumeroFile { get; set; }
    public int? PostiPerFila { get; set; }
    public string? ConfigurazionePosti { get; set; }
    public bool Attiva { get; set; }
}

public class PiantinaDTO
{
    public int SalaId { get; set; }
    public int NumeroFile { get; set; }
    public List<PiantinaFilaDTO> File { get; set; } = new();
}

public class PiantinaFilaDTO
{
    public int Fila { get; set; }
    public int Posti { get; set; }
}

public class PiantinaUpdateDTO
{
    public string? ConfigurazionePosti { get; set; }
    public int NumeroFile { get; set; }
    public int? PostiPerFila { get; set; }
}

