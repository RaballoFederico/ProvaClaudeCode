namespace FilmAPI.DTO;

public class CategoriaDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descrizione { get; set; }
}

public class CategoriaCreateDTO
{
    public string Nome { get; set; } = string.Empty;
    public string? Descrizione { get; set; }
}

public class FilmWithCategorieDTO : FilmDTO
{
    public new List<CategoriaDTO> Categorie { get; set; } = new();
}

public class FilmCreateWithCategorieDTO
{
    public string Titolo { get; set; } = string.Empty;
    public DateTime DataProduzione { get; set; }
    public int RegistaId { get; set; }
    public int Durata { get; set; }
    public string? CopertinaPath { get; set; }
    public string? FilmatoPath { get; set; }
    public List<int> CategoriaIds { get; set; } = new();
}

public class FilmUpdateWithCategorieDTO
{
    public string Titolo { get; set; } = string.Empty;
    public DateTime DataProduzione { get; set; }
    public int RegistaId { get; set; }
    public int Durata { get; set; }
    public string? CopertinaPath { get; set; }
    public string? FilmatoPath { get; set; }
    public List<int> CategoriaIds { get; set; } = new();
}
