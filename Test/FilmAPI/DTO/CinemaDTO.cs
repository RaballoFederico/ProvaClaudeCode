namespace FilmAPI.DTO;

public class CinemaDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Indirizzo { get; set; } = string.Empty;
    public string Citta { get; set; } = string.Empty;
    public int PostiMassimi { get; set; }
}

public class CinemaCreateDTO
{
    public string Nome { get; set; } = string.Empty;
    public string Indirizzo { get; set; } = string.Empty;
    public string Citta { get; set; } = string.Empty;
    public int PostiMassimi { get; set; } = 120;
}

public class CinemaUpdateDTO
{
    public string Nome { get; set; } = string.Empty;
    public string Indirizzo { get; set; } = string.Empty;
    public string Citta { get; set; } = string.Empty;
    public int PostiMassimi { get; set; } = 120;
}
