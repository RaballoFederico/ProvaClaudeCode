using FilmAPI.Data;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;

namespace FilmAPI.Services;

public class UserSecurityAuditService(FilmDbContext db) : IUserSecurityAuditService
{
    public async Task LogAsync(string eventType, string outcome, int? actorUserId = null, int? targetUserId = null, string? email = null, string? ipAddress = null, string? details = null)
    {
        db.UserSecurityAuditLogs.Add(new UserSecurityAuditLog
        {
            EventType = (eventType ?? "unknown").Trim(),
            Outcome = string.IsNullOrWhiteSpace(outcome) ? "success" : outcome.Trim(),
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(),
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            Details = string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
