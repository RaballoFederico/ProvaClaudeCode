namespace FilmAPI.DTO;

public class TMDBMovieSearchResultDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public string? ReleaseDate { get; set; }
    public double VoteAverage { get; set; }
    public List<int> GenreIds { get; set; } = new();
}

public class TMDBMovieDetailDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public string? ReleaseDate { get; set; }
    public double VoteAverage { get; set; }
    public int Runtime { get; set; }
    public List<TMDBGenreDTO> Genres { get; set; } = new();
    public List<TMDBCastMemberDTO> Credits { get; set; } = new();
}

public class TMDBGenreDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TMDBCastMemberDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Character { get; set; } = string.Empty;
    public string? ProfilePath { get; set; }
    public int Order { get; set; }
}

public class TMDBImportResultDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? FilmId { get; set; }
    public string? ExistingOrNew { get; set; }
}

public class TMDBImportRequestDTO
{
    public int TmdbMovieId { get; set; }
}