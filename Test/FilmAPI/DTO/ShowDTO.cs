namespace FilmAPI.DTO;

public class ShowDTO
{
    public int Id { get; set; }
    public int SalaId { get; set; }
    public int CinemaId { get; set; }
    public int FilmId { get; set; }
    public string FilmTitolo { get; set; } = string.Empty;
    public string SalaNome { get; set; } = string.Empty;
    public string TipologiaSala { get; set; } = string.Empty;
    public DateOnly Data { get; set; }
    public TimeOnly OraInizio { get; set; }
    public TimeOnly OraFine { get; set; }
    public decimal PrezzoBase { get; set; }
    public string Stato { get; set; } = string.Empty;
}

public class ShowCreateDTO
{
    public int SalaId { get; set; }
    public int FilmId { get; set; }
    public DateOnly Data { get; set; }
    public TimeOnly OraInizio { get; set; }
    public decimal? PrezzoBase { get; set; }
}

public class ShowUpdateDTO
{
    public int SalaId { get; set; }
    public int FilmId { get; set; }
    public DateOnly Data { get; set; }
    public TimeOnly OraInizio { get; set; }
    public decimal PrezzoBase { get; set; }
    public int Stato { get; set; }
}

public class ShowFilterDTO
{
    public int? CinemaId { get; set; }
    public int? SalaId { get; set; }
    public int? FilmId { get; set; }
    public DateOnly? Data { get; set; }
    public int? Stato { get; set; }
}

public class DisponibilitaPostiDTO
{
    public int ShowId { get; set; }
    public int Totali { get; set; }
    public int Occupati { get; set; }
    public int PrenotatiTemporanei { get; set; }
    public int Disponibili { get; set; }
}

public class ShowBulkCreateDTO
{
    public int CinemaId { get; set; }
    public int FilmId { get; set; }
    public DateOnly Dal { get; set; }
    public DateOnly Al { get; set; }
    public List<ShowBulkSlotDTO> Slots { get; set; } = new();
}

public class ShowBulkSlotDTO
{
    public int SalaId { get; set; }
    public List<TimeOnly> Orari { get; set; } = new();
    public List<int> GiorniSettimana { get; set; } = new();
}
