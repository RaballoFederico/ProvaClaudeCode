// DOC: Interfaccia service 'IUserSecurityAuditService': contratto della logica applicativa usata dagli endpoint.
namespace FilmAPI.Services.Interfaces;

public interface IUserSecurityAuditService
{
    Task LogAsync(string eventType, string outcome, int? actorUserId = null, int? targetUserId = null, string? email = null, string? ipAddress = null, string? details = null);
}

