using FilmAPI.DTO;

namespace FilmAPI.Services.Interfaces;

public interface IEmailService
{
    Task InviaConfermaAcquistoAsync(string toEmail, string soggetto, string htmlBody, byte[]? allegatoPdf = null, string? nomeFile = null);
    Task InviaEmailStrictAsync(string toEmail, string soggetto, string htmlBody, byte[]? allegatoPdf = null, string? nomeFile = null);
}
