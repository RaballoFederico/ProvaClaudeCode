// DOC: DTO 'ProiezioneDTO': contratto dati per request/response API.
namespace FilmAPI.DTO;

public class ProiezioneDTO
{
    public int Id { get; set; }
    public int? ShowId { get; set; }
    public int CinemaId { get; set; }
    public int FilmId { get; set; }
    public DateTime Data { get; set; }
    public TimeSpan Ora { get; set; }
}

public class ProiezioneCreateDTO
{
    public int? ShowId { get; set; }
    public int CinemaId { get; set; }
    public int FilmId { get; set; }
    public DateTime Data { get; set; }
    public TimeSpan Ora { get; set; }
}

public class ProiezioneUpdateDTO
{
    public int? ShowId { get; set; }
    public int CinemaId { get; set; }
    public int FilmId { get; set; }
    public DateTime Data { get; set; }
    public TimeSpan Ora { get; set; }
}

