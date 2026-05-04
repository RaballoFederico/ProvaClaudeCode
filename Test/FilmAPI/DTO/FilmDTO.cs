namespace FilmAPI.DTO;

public class FilmDTO
{
    public int Id { get; set; }
    public string Titolo { get; set; } = string.Empty;
    public DateTime DataProduzione { get; set; }
    public int RegistaId { get; set; }
    public int Durata { get; set; }
    public string? CopertinaPath { get; set; }
    public string? FilmatoPath { get; set; }
    public List<int> CategorieIds { get; set; } = new();
    public List<CategoriaDTO> Categorie { get; set; } = new();
}

public class FilmCreateDTO
{
    public string Titolo { get; set; } = string.Empty;
    public DateTime DataProduzione { get; set; }
    public int RegistaId { get; set; }
    public int Durata { get; set; }
    public string? CopertinaPath { get; set; }
    public string? FilmatoPath { get; set; }
    public List<int> CategoriaIds { get; set; } = new();
}

public class FilmUpdateDTO
{
    public string Titolo { get; set; } = string.Empty;
    public DateTime DataProduzione { get; set; }
    public int RegistaId { get; set; }
    public int Durata { get; set; }
    public string? CopertinaPath { get; set; }
    public string? FilmatoPath { get; set; }
    public List<int> CategoriaIds { get; set; } = new();
}
