namespace FilmAPI.DTO;

public class RegistaDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string? Nazionalita { get; set; }
}

public class RegistaCreateDTO
{
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string? Nazionalita { get; set; }
}

public class RegistaUpdateDTO
{
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string? Nazionalita { get; set; }
}
