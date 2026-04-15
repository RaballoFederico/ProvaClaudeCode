using FilmAPI.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FilmAPI.Services;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    private static bool IsPlaceholder(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var v = value.Trim();
        return v.Contains("your-", StringComparison.OrdinalIgnoreCase)
               || v.Contains("example", StringComparison.OrdinalIgnoreCase)
               || v.Contains("placeholder", StringComparison.OrdinalIgnoreCase);
    }

    public async Task InviaConfermaAcquistoAsync(string toEmail, string soggetto, string htmlBody, byte[]? allegatoPdf = null, string? nomeFile = null)
    {
        var host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? configuration["SMTP:Host"];
        var portRaw = Environment.GetEnvironmentVariable("SMTP_PORT") ?? configuration["SMTP:Port"];
        var user = Environment.GetEnvironmentVariable("SMTP_USER") ?? configuration["SMTP:User"];
        var pass = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? configuration["SMTP:Password"];
        var from = Environment.GetEnvironmentVariable("SMTP_FROM") ?? configuration["SMTP:From"] ?? user;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(portRaw) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass) || string.IsNullOrWhiteSpace(from))
        {
            logger.LogWarning("Configurazione SMTP mancante: invio email saltato per {Email}", toEmail);
            return;
        }

        if (IsPlaceholder(host) || IsPlaceholder(user) || IsPlaceholder(pass) || IsPlaceholder(from))
        {
            logger.LogInformation("Configurazione SMTP placeholder rilevata: invio email saltato per {Email}", toEmail);
            return;
        }

        if (!int.TryParse(portRaw, out var port))
        {
            logger.LogWarning("SMTP_PORT non valido: {Port}", portRaw);
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
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(user, pass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Invio email fallito verso {Email}", toEmail);
        }
    }
}
