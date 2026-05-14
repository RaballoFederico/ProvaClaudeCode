using FilmAPI.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net.Sockets;

namespace FilmAPI.Services;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    private static string? NormalizeSetting(string? value, bool removeAllSpaces = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length >= 2 && trimmed.StartsWith('"') && trimmed.EndsWith('"'))
        {
            trimmed = trimmed[1..^1].Trim();
        }

        if (removeAllSpaces)
        {
            trimmed = trimmed.Replace(" ", string.Empty);
        }

        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static bool IsPlaceholder(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var v = value.Trim();
        return v.Contains("your-", StringComparison.OrdinalIgnoreCase)
               || v.Contains("example", StringComparison.OrdinalIgnoreCase)
               || v.Contains("placeholder", StringComparison.OrdinalIgnoreCase);
    }

    public async Task InviaConfermaAcquistoAsync(string toEmail, string soggetto, string htmlBody, byte[]? allegatoPdf = null, string? nomeFile = null)
        => await InviaEmailCoreAsync(toEmail, soggetto, htmlBody, allegatoPdf, nomeFile, strict: false);

    public async Task InviaEmailStrictAsync(string toEmail, string soggetto, string htmlBody, byte[]? allegatoPdf = null, string? nomeFile = null)
        => await InviaEmailCoreAsync(toEmail, soggetto, htmlBody, allegatoPdf, nomeFile, strict: true);

    private async Task InviaEmailCoreAsync(string toEmail, string soggetto, string htmlBody, byte[]? allegatoPdf, string? nomeFile, bool strict)
    {
        var host = NormalizeSetting(Environment.GetEnvironmentVariable("SMTP_HOST")) ?? NormalizeSetting(configuration["SMTP:Host"]);
        var portRaw = NormalizeSetting(Environment.GetEnvironmentVariable("SMTP_PORT")) ?? NormalizeSetting(configuration["SMTP:Port"]);
        var user = NormalizeSetting(Environment.GetEnvironmentVariable("SMTP_USER")) ?? NormalizeSetting(configuration["SMTP:User"]);
        var pass = NormalizeSetting(Environment.GetEnvironmentVariable("SMTP_PASSWORD"), removeAllSpaces: true) ?? NormalizeSetting(configuration["SMTP:Password"], removeAllSpaces: true);
        var from = NormalizeSetting(Environment.GetEnvironmentVariable("SMTP_FROM")) ?? NormalizeSetting(configuration["SMTP:From"]) ?? user;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(portRaw) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass) || string.IsNullOrWhiteSpace(from))
        {
            logger.LogWarning(
                "Configurazione SMTP mancante: invio email saltato per {Email}. Host:{HasHost} Port:{HasPort} User:{HasUser} Pass:{HasPass} From:{HasFrom}",
                toEmail,
                !string.IsNullOrWhiteSpace(host),
                !string.IsNullOrWhiteSpace(portRaw),
                !string.IsNullOrWhiteSpace(user),
                !string.IsNullOrWhiteSpace(pass),
                !string.IsNullOrWhiteSpace(from));
            if (strict) throw new InvalidOperationException("Configurazione SMTP mancante");
            return;
        }

        if (IsPlaceholder(host) || IsPlaceholder(user) || IsPlaceholder(pass) || IsPlaceholder(from))
        {
            logger.LogInformation("Configurazione SMTP placeholder rilevata: invio email saltato per {Email}", toEmail);
            if (strict) throw new InvalidOperationException("Configurazione SMTP placeholder/non valida");
            return;
        }

        logger.LogInformation(
            "SMTP diagnostica -> Host:{Host} Port:{Port} User:{User} From:{From} PasswordLength:{PasswordLength}",
            host,
            portRaw,
            user,
            from,
            pass?.Length ?? 0);

        if (!int.TryParse(portRaw, out var port))
        {
            logger.LogWarning("SMTP_PORT non valido: {Port}", portRaw);
            if (strict) throw new InvalidOperationException("SMTP_PORT non valido");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = soggetto;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        if (allegatoPdf is not null && allegatoPdf.Length > 0)
        {
            bodyBuilder.Attachments.Add(nomeFile ?? "biglietti.pdf", allegatoPdf, ContentType.Parse("application/pdf"));
        }
        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            client.Timeout = 15000;

            var attempts = new List<(int Port, SecureSocketOptions Security)>
            {
                (port, SecureSocketOptions.StartTls)
            };

            if (port != 465)
            {
                attempts.Add((465, SecureSocketOptions.SslOnConnect));
            }

            if (port != 587)
            {
                attempts.Add((587, SecureSocketOptions.StartTls));
            }

            Exception? lastException = null;
            var connected = false;

            foreach (var attempt in attempts.Distinct())
            {
                try
                {
                    await client.ConnectAsync(host, attempt.Port, attempt.Security);
                    connected = true;
                    break;
                }
                catch (SocketException ex)
                {
                    lastException = ex;
                    logger.LogWarning(
                        "Tentativo SMTP fallito verso {Host}:{Port} ({Security}) per {Email}. SocketError: {ErrorCode}",
                        host, attempt.Port, attempt.Security, toEmail, ex.SocketErrorCode);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    logger.LogWarning(
                        ex,
                        "Tentativo SMTP fallito verso {Host}:{Port} ({Security}) per {Email}",
                        host, attempt.Port, attempt.Security, toEmail);
                }
            }

            if (!connected)
            {
                throw lastException ?? new InvalidOperationException("Connessione SMTP non riuscita");
            }

            await client.AuthenticateAsync(user, pass!);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            logger.LogInformation("Email inviata con successo verso {Email} (oggetto: {Subject})", toEmail, soggetto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invio email fallito verso {Email}", toEmail);
            if (strict) throw;
        }
    }
}
