using System.Text.Json;

namespace FilmAPI.Services;

public class TMDBService : ITMDBService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly IConfiguration _configuration;
    private const string BaseUrl = "https://api.themoviedb.org/3";

    public TMDBService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _apiKey = Environment.GetEnvironmentVariable("TMDB_API_KEY")
            ?? _configuration["TMDB:ApiKey"]
            ?? string.Empty;
    }

    private bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<string?> SearchMovieAsync(string query, int page = 1)
    {
        if (!IsConfigured) return null;

        var url = $"{BaseUrl}/search/movie?api_key={_apiKey}&query={Uri.EscapeDataString(query)}&page={page}&language=it-IT";
        return await _httpClient.GetStringAsync(url);
    }

    public async Task<string?> GetMovieDetailAsync(int tmdbId, string language = "it-IT")
    {
        if (!IsConfigured) return null;

        var url = $"{BaseUrl}/movie/{tmdbId}?api_key={_apiKey}&language={language}";
        return await _httpClient.GetStringAsync(url);
    }

    public async Task<string?> GetMovieCreditsAsync(int tmdbId, string language = "it-IT")
    {
        if (!IsConfigured) return null;

        var url = $"{BaseUrl}/movie/{tmdbId}/credits?api_key={_apiKey}&language={language}";
        return await _httpClient.GetStringAsync(url);
    }

    public async Task<string?> GetPersonDetailAsync(int personId, string language = "it-IT")
    {
        if (!IsConfigured) return null;

        var url = $"{BaseUrl}/person/{personId}?api_key={_apiKey}&language={language}";
        return await _httpClient.GetStringAsync(url);
    }

    public async Task<string?> GetPersonMovieCreditsAsync(int personId, string language = "it-IT")
    {
        if (!IsConfigured) return null;

        var url = $"{BaseUrl}/person/{personId}/movie_credits?api_key={_apiKey}&language={language}";
        return await _httpClient.GetStringAsync(url);
    }

    public async Task<string?> GetPopularMoviesAsync(int page = 1, string language = "it-IT")
    {
        if (!IsConfigured) return null;

        var url = $"{BaseUrl}/movie/popular?api_key={_apiKey}&page={page}&language={language}";
        return await _httpClient.GetStringAsync(url);
    }

    public string GetPosterUrl(string? posterPath, string size = "w500")
    {
        if (string.IsNullOrWhiteSpace(posterPath)) return string.Empty;
        return $"https://image.tmdb.org/t/p/{size}{posterPath}";
    }

    public string GetBackdropUrl(string? backdropPath, string size = "w1280")
    {
        if (string.IsNullOrWhiteSpace(backdropPath)) return string.Empty;
        return $"https://image.tmdb.org/t/p/{size}{backdropPath}";
    }

    public string GetProfileUrl(string? profilePath, string size = "w185")
    {
        if (string.IsNullOrWhiteSpace(profilePath)) return string.Empty;
        return $"https://image.tmdb.org/t/p/{size}{profilePath}";
    }
}

public interface ITMDBService
{
    Task<string?> SearchMovieAsync(string query, int page = 1);
    Task<string?> GetMovieDetailAsync(int tmdbId, string language = "it-IT");
    Task<string?> GetMovieCreditsAsync(int tmdbId, string language = "it-IT");
    Task<string?> GetPersonDetailAsync(int personId, string language = "it-IT");
    Task<string?> GetPersonMovieCreditsAsync(int personId, string language = "it-IT");
    Task<string?> GetPopularMoviesAsync(int page = 1, string language = "it-IT");
    string GetPosterUrl(string? posterPath, string size = "w500");
    string GetBackdropUrl(string? backdropPath, string size = "w1280");
    string GetProfileUrl(string? profilePath, string size = "w185");
    static string MapTmdbGenreToItalian(int genreId)
    {
        return genreId switch
        {
            28 => "Azione",
            12 => "Avventura",
            16 => "Animazione",
            35 => "Commedia",
            80 => "Criminale",
            99 => "Documentario",
            18 => "Dramma",
            10751 => "Famiglia",
            14 => "Fantasy",
            36 => "Storia",
            27 => "Horror",
            10402 => "Musica",
            9648 => "Mistero",
            10749 => "Romance",
            878 => "Fantascienza",
            10770 => "TV Movie",
            53 => "Thriller",
            10752 => "Guerra",
            37 => "Western",
            _ => "Altro"
        };
    }
}
