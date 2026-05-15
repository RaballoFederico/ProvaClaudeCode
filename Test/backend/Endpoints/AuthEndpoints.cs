using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace FilmAPI.Endpoints;

public static class AuthEndpoints
{
    private const string RefreshTokenCookieName = "filmapi_refresh_token";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");
        group.RequireRateLimiting("auth");

        group.MapPost("/login", [AllowAnonymous] async (HttpContext context, LoginRequestDTO request, IAuthService authService) =>
        {
            var (response, error) = await authService.LoginAsync(request);
            if (error != null)
            {
                return Results.Unauthorized();
            }

            AppendRefreshTokenCookie(context, response!.RefreshToken);

            return Results.Ok(response);
        });

        group.MapPost("/refresh", [AllowAnonymous] async (HttpContext context, RefreshTokenRequestDTO request, IAuthService authService) =>
        {
            var requestToken = request.RefreshToken;
            if (string.IsNullOrWhiteSpace(requestToken))
            {
                requestToken = context.Request.Cookies[RefreshTokenCookieName] ?? string.Empty;
            }

            var (response, error) = await authService.RefreshAsync(new RefreshTokenRequestDTO
            {
                RefreshToken = requestToken
            });

            if (error != null)
            {
                DeleteRefreshTokenCookie(context);
                return Results.Unauthorized();
            }

            AppendRefreshTokenCookie(context, response!.RefreshToken);

            return Results.Ok(response);
        });

        group.MapPost("/logout", [Authorize] async (HttpContext context, IAuthService authService) =>
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out var userIdValue))
            {
                return Results.Unauthorized();
            }

            await authService.LogoutAsync(userIdValue);
            DeleteRefreshTokenCookie(context);
            return Results.Ok(new { message = "Logout effettuato con successo" });
        });

        group.MapPost("/change-password", [Authorize] async (HttpContext context, ChangePasswordRequestDTO request, IAuthService authService, IUserSecurityAuditService auditService) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var result = await authService.ChangePasswordAsync(userId.Value, request);
            if (!result.success)
            {
                await auditService.LogAsync("change_password", "failed", userId.Value, userId.Value, ipAddress: context.Connection.RemoteIpAddress?.ToString(), details: result.error);
                return result.error switch
                {
                    "NOT_FOUND" => Results.NotFound(new { message = "Utente non trovato" }),
                    "PASSWORD_NOT_SET" => Results.BadRequest(new { message = "Password locale non configurata" }),
                    "INVALID_CURRENT_PASSWORD" => Results.BadRequest(new { message = "Password attuale non corretta" }),
                    "WEAK_PASSWORD" => Results.BadRequest(new { message = "Password troppo debole: usa almeno 8 caratteri, con maiuscola, minuscola, numero e simbolo" }),
                    _ => Results.BadRequest(new { message = "Impossibile cambiare password" })
                };
            }

            await auditService.LogAsync("change_password", "success", userId.Value, userId.Value, ipAddress: context.Connection.RemoteIpAddress?.ToString());
            return Results.Ok(new { message = "Password aggiornata con successo" });
        }).RequireRateLimiting("auth-sensitive");

        group.MapPost("/register", [AllowAnonymous] async (RegistrazioneRequestDTO request, IAuthService authService) =>
        {
            var (utenteId, error) = await authService.RegisterAsync(request);
            if (error != null)
            {
                if (error.Contains("gia' in uso", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Conflict(new { message = error });
                }

                if (error.Contains("Ruolo User", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Problem(error);
                }

                return Results.BadRequest(new { message = error });
            }

            return Results.Created($"/auth/me", new { message = "Registrazione completata con successo", utenteId });
        });

        group.MapGet("/me", [Authorize] async (HttpContext context, IAuthService authService) =>
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !int.TryParse(userId, out var userIdValue))
            {
                return Results.Unauthorized();
            }

            var utente = await authService.GetMeAsync(userIdValue);
            if (utente is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(utente);
        });

        group.MapGet("/external/providers", [AllowAnonymous] (IExternalAuthService externalAuthService) =>
        {
            return Results.Ok(externalAuthService.GetEnabledProviders());
        });

        group.MapGet("/external/{provider}/start", [AllowAnonymous] (string provider, HttpContext context, IConfiguration configuration, IExternalAuthService externalAuthService) =>
        {
            var backendBaseUrl = ResolveBackendBaseUrl(context, configuration);
            var returnUrl = context.Request.Query["returnUrl"].FirstOrDefault();

            var (redirectUrl, error) = externalAuthService.CreateAuthorizationUrl(provider, returnUrl, backendBaseUrl);
            if (redirectUrl == null)
            {
                return Results.BadRequest(new { message = error ?? "Provider non configurato" });
            }

            return Results.Ok(new ExternalAuthStartResponseDTO { RedirectUrl = redirectUrl });
        });

        group.MapGet("/external/{provider}/callback", [AllowAnonymous] async (string provider, HttpContext context, IConfiguration configuration, IExternalAuthService externalAuthService) =>
        {
            var backendBaseUrl = ResolveBackendBaseUrl(context, configuration);
            var code = context.Request.Query["code"].FirstOrDefault();
            var state = context.Request.Query["state"].FirstOrDefault();
            var oauthError = context.Request.Query["error"].FirstOrDefault();
            var redirectUrl = await externalAuthService.HandleCallbackAsync(provider, backendBaseUrl, code, state, oauthError);
            return Results.Redirect(redirectUrl);
        });

        group.MapPost("/external/complete", [AllowAnonymous] async (HttpContext context, ExternalAuthCompleteRequestDTO request, IExternalAuthService externalAuthService) =>
        {
            var (response, error) = await externalAuthService.CompleteAsync(request);
            if (response == null)
            {
                return Results.BadRequest(new { message = error ?? "Completamento login esterno non riuscito" });
            }

            AppendRefreshTokenCookie(context, response.RefreshToken);

            return Results.Ok(response);
        });

        group.MapPost("/forgot-password", [AllowAnonymous] async (
            ForgotPasswordRequestDTO request,
            FilmDbContext db,
            IAccountActionTokenService accountActionTokenService,
            IEmailService emailService,
            IUserSecurityAuditService auditService,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            HttpContext context) =>
        {
            var logger = loggerFactory.CreateLogger("Security.Auth");
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            var genericMessage = "Se esiste un account associato, riceverai una email con le istruzioni.";
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                logger.LogWarning("ForgotPassword generic response for invalid email format from {Ip}", context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                await auditService.LogAsync("forgot_password", "generic_invalid_email", email: email, ipAddress: context.Connection.RemoteIpAddress?.ToString());
                return Results.Ok(new { message = genericMessage });
            }

            var user = await db.Utenti.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Attivo);
            if (user == null)
            {
                logger.LogInformation("ForgotPassword generic response for unknown email from {Ip}", context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                await auditService.LogAsync("forgot_password", "generic_unknown_email", email: email, ipAddress: context.Connection.RemoteIpAddress?.ToString());
                return Results.Ok(new { message = genericMessage });
            }

            var token = await accountActionTokenService.CreateAsync(
                user.Email,
                AccountActionTokenPurpose.PasswordReset,
                TimeSpan.FromMinutes(30),
                user.Id);

            var frontendBaseUrl = ResolveFrontendBaseUrl(configuration, request.ReturnUrl);
            var resetUrl = $"{frontendBaseUrl.TrimEnd('/')}/reimposta-password.html?token={Uri.EscapeDataString(token)}";

            var html = $@"
                    <div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.45;color:#1f2937'>
                        <h2 style='margin-bottom:8px'>Reset password FilmHub</h2>
                        <p>Abbiamo ricevuto una richiesta di reimpostazione password.</p>
                        <p>Per impostare una nuova password clicca qui:</p>
                        <p><a href='{resetUrl}' style='display:inline-block;padding:10px 16px;background:#e50914;color:#fff;text-decoration:none;border-radius:8px'>Reimposta password</a></p>
                        <p>Il link scade tra 30 minuti.</p>
                        <p>Se non hai richiesto tu il reset, puoi ignorare questa email.</p>
                    </div>";

            await emailService.InviaConfermaAcquistoAsync(
                user.Email,
                "FilmHub - Reimpostazione password",
                html);

            logger.LogInformation("ForgotPassword issued for userId={UserId} from {Ip}", user.Id, context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            await auditService.LogAsync("forgot_password", "token_issued", targetUserId: user.Id, email: user.Email, ipAddress: context.Connection.RemoteIpAddress?.ToString());

            return Results.Ok(new { message = genericMessage });
        }).RequireRateLimiting("auth-sensitive");

        group.MapPost("/recover-account", [AllowAnonymous] async (
            RecoverAccountRequestDTO request,
            FilmDbContext db,
            IEmailService emailService,
            IConfiguration configuration,
            IMemoryCache cache,
            ILoggerFactory loggerFactory,
            HttpContext context) =>
        {
            var logger = loggerFactory.CreateLogger("Security.Auth");
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                logger.LogWarning("RecoverAccount rejected: invalid email format from {Ip}", context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                return Results.BadRequest(new { message = "Inserisci una email valida" });
            }

            var user = await db.Utenti.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Attivo);
            if (user == null)
            {
                logger.LogWarning("RecoverAccount rejected: email not found for {Email} from {Ip}", email, context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                return Results.BadRequest(new
                {
                    message = "Nessun account trovato con questa email"
                });
            }

            var token = CreateRandomUrlSafeToken();
            cache.Set($"acct-recover:{token}", user.Id, TimeSpan.FromMinutes(30));
            var frontendBaseUrl = ResolveFrontendBaseUrl(configuration, request.ReturnUrl);
            var recoverUrl = $"{frontendBaseUrl.TrimEnd('/')}/login.html?recoverToken={Uri.EscapeDataString(token)}";

            var html = $@"
                    <div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.45;color:#1f2937'>
                        <h2 style='margin-bottom:8px'>Recupero account FilmHub</h2>
                        <p>Per motivi di sicurezza, per recuperare il tuo account devi prima impostare una nuova password.</p>
                        <p>Clicca qui per continuare:</p>
                        <p><a href='{recoverUrl}' style='display:inline-block;padding:10px 16px;background:#e50914;color:#fff;text-decoration:none;border-radius:8px'>Recupera account</a></p>
                        <p>Se non hai fatto tu questa richiesta, puoi ignorare questa email.</p>
                    </div>";

            await emailService.InviaConfermaAcquistoAsync(
                user.Email,
                "FilmHub - Recupero account",
                html);

            logger.LogInformation("RecoverAccount issued for userId={UserId} from {Ip}", user.Id, context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

            return Results.Ok(new
            {
                message = "Email trovata: ti abbiamo inviato il link per il recupero account."
            });
        }).RequireRateLimiting("auth-sensitive");

        group.MapPost("/recover-account/complete", [AllowAnonymous] async (
            CompleteRecoverAccountRequestDTO request,
            IMemoryCache cache,
            IAuthService authService,
            FilmDbContext db,
            IEmailService emailService,
            IUserSecurityAuditService auditService,
            ILoggerFactory loggerFactory,
            HttpContext context) =>
        {
            var logger = loggerFactory.CreateLogger("Security.Auth");
            var token = (request.Token ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                logger.LogWarning("RecoverAccountComplete rejected: missing token from {Ip}", context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                await auditService.LogAsync("recover_account_complete", "missing_token", ipAddress: context.Connection.RemoteIpAddress?.ToString());
                return Results.BadRequest(new { message = "Token non valido" });
            }

            if (!cache.TryGetValue($"acct-recover:{token}", out int userId) || userId <= 0)
            {
                logger.LogWarning("RecoverAccountComplete rejected: invalid/expired token from {Ip}", context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                await auditService.LogAsync("recover_account_complete", "invalid_token", ipAddress: context.Connection.RemoteIpAddress?.ToString());
                return Results.BadRequest(new { message = "Token scaduto o non valido" });
            }

            var (success, error) = await authService.SetPasswordAsync(userId, request.NewPassword);
            if (!success)
            {
                logger.LogWarning("RecoverAccountComplete failed for userId={UserId}: {Error}", userId, error ?? "UNKNOWN");
                await auditService.LogAsync("recover_account_complete", "failed", targetUserId: userId, ipAddress: context.Connection.RemoteIpAddress?.ToString(), details: error);
                if (string.Equals(error, "WEAK_PASSWORD", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { message = "Password troppo debole: usa almeno 8 caratteri, con maiuscola, minuscola, numero e simbolo" });
                }

                return Results.BadRequest(new { message = "Impossibile completare il recupero account" });
            }

            var user = await db.Utenti.FirstOrDefaultAsync(u => u.Id == userId && u.Attivo);
            if (user == null)
            {
                return Results.BadRequest(new { message = "Account non trovato" });
            }

            cache.Remove($"acct-recover:{token}");
            var html = $@"
                    <div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.45;color:#1f2937'>
                        <h2 style='margin-bottom:8px'>Credenziali aggiornate FilmHub</h2>
                        <p>Il recupero account e stato completato con successo.</p>
                        <p>Username: <strong>{System.Net.WebUtility.HtmlEncode(user.Username)}</strong></p>
                        <p>Nuova password: <strong>{System.Net.WebUtility.HtmlEncode(request.NewPassword)}</strong></p>
                        <p>Per sicurezza, ti consigliamo di cambiare nuovamente password dopo il primo accesso.</p>
                    </div>";

            await emailService.InviaConfermaAcquistoAsync(
                user.Email,
                "FilmHub - Credenziali recupero account",
                html);

            logger.LogInformation("RecoverAccountComplete success for userId={UserId} from {Ip}", userId, context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            await auditService.LogAsync("recover_account_complete", "success", targetUserId: userId, email: user.Email, ipAddress: context.Connection.RemoteIpAddress?.ToString());
            return Results.Ok(new
            {
                message = "Recupero account completato: credenziali inviate via email"
            });
        }).RequireRateLimiting("auth-sensitive");

        group.MapPost("/reset-password", [AllowAnonymous] async (
            ResetPasswordRequestDTO request,
            IAccountActionTokenService accountActionTokenService,
            IAuthService authService,
            IUserSecurityAuditService auditService,
            ILoggerFactory loggerFactory,
            HttpContext context) =>
        {
            var logger = loggerFactory.CreateLogger("Security.Auth");
            var token = (request.Token ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                logger.LogWarning("ResetPassword rejected: missing token from {Ip}", context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                await auditService.LogAsync("reset_password", "missing_token", ipAddress: context.Connection.RemoteIpAddress?.ToString());
                return Results.BadRequest(new { message = "Token non valido" });
            }

            var consumedToken = await accountActionTokenService.ConsumeAsync(token, AccountActionTokenPurpose.PasswordReset);
            if (consumedToken is null || consumedToken.UtenteId is null || consumedToken.UtenteId <= 0)
            {
                logger.LogWarning("ResetPassword rejected: invalid/expired token from {Ip}", context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                await auditService.LogAsync("reset_password", "invalid_or_reused_token", ipAddress: context.Connection.RemoteIpAddress?.ToString());
                return Results.BadRequest(new { message = "Token scaduto o non valido" });
            }

            var userId = consumedToken.UtenteId.Value;
            var (success, error) = await authService.SetPasswordAsync(userId, request.NewPassword);
            if (!success)
            {
                logger.LogWarning("ResetPassword failed for userId={UserId}: {Error}", userId, error ?? "UNKNOWN");
                await auditService.LogAsync("reset_password", "failed", targetUserId: userId, email: consumedToken.Email, ipAddress: context.Connection.RemoteIpAddress?.ToString(), details: error);
                if (string.Equals(error, "WEAK_PASSWORD", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { message = "Password troppo debole: usa almeno 8 caratteri, con maiuscola, minuscola, numero e simbolo" });
                }

                return Results.BadRequest(new { message = "Impossibile aggiornare la password" });
            }

            logger.LogInformation("ResetPassword success for userId={UserId} from {Ip}", userId, context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            await auditService.LogAsync("reset_password", "success", targetUserId: userId, email: consumedToken.Email, ipAddress: context.Connection.RemoteIpAddress?.ToString());
            return Results.Ok(new { message = "Password aggiornata con successo" });
        }).RequireRateLimiting("auth-sensitive");

        group.MapPost("/complete-invite", [AllowAnonymous] async (
            CompleteInviteRequestDTO request,
            FilmDbContext db,
            IAccountActionTokenService accountActionTokenService,
            IUserSecurityAuditService auditService) =>
        {
            var consumed = await accountActionTokenService.ConsumeAsync(request.Token ?? string.Empty, AccountActionTokenPurpose.AccountInvite);
            if (consumed is null)
            {
                await auditService.LogAsync("complete_invite", "invalid_or_reused_token", email: request.Email);
                return Results.BadRequest(new { message = "Token invito non valido o scaduto" });
            }

            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (!string.Equals(email, consumed.Email, StringComparison.OrdinalIgnoreCase))
            {
                await auditService.LogAsync("complete_invite", "email_mismatch", email: email);
                return Results.BadRequest(new { message = "Email non coerente con invito" });
            }

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { message = "Username e password obbligatori" });
            }

            var roleName = string.IsNullOrWhiteSpace(consumed.MetadataJson) ? "PowerUser" : consumed.MetadataJson.Trim();
            if (!string.Equals(roleName, "PowerUser", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { message = "Ruolo invito non valido" });
            }

            var role = await db.Ruoli.FirstOrDefaultAsync(r => r.Nome == roleName);
            if (role is null) return Results.BadRequest(new { message = "Ruolo non trovato" });

            var user = await db.Utenti.Include(u => u.UtentiRuoli).FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user is null)
            {
                if (await db.Utenti.AnyAsync(u => u.Username == request.Username))
                {
                    return Results.Conflict(new { message = "Username gia in uso" });
                }

                user = new Utente
                {
                    Username = request.Username.Trim(),
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Attivo = true,
                    DataRegistrazione = DateTime.UtcNow
                };
                db.Utenti.Add(user);
                await db.SaveChangesAsync();
                user = await db.Utenti.Include(u => u.UtentiRuoli).FirstAsync(u => u.Id == user.Id);
            }
            else
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            user.UtentiRuoli.Clear();
            user.UtentiRuoli.Add(new UtenteRuolo { RuoloId = role.Id, UtenteId = user.Id });
            await db.SaveChangesAsync();

            await auditService.LogAsync("complete_invite", "success", targetUserId: user.Id, email: email, details: $"role={roleName}");
            return Results.Ok(new { message = "Invito completato con successo" });
        }).RequireRateLimiting("auth-sensitive");

        return app;
    }

    private static string ResolveBackendBaseUrl(HttpContext context, IConfiguration configuration)
    {
        var configured = Environment.GetEnvironmentVariable("EXTERNAL_AUTH_BACKEND_BASE_URL")
            ?? configuration["ExternalAuth:BackendBaseUrl"];

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        return $"{context.Request.Scheme}://{context.Request.Host}";
    }

    private static string ResolveFrontendBaseUrl(IConfiguration configuration, string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            try
            {
                var parsed = new Uri(returnUrl);
                return $"{parsed.Scheme}://{parsed.Authority}";
            }
            catch
            {
                // fallback below
            }
        }

        var configured = Environment.GetEnvironmentVariable("EXTERNAL_AUTH_FRONTEND_BASE_URL")
            ?? configuration["ExternalAuth:DefaultReturnUrl"];

        if (!string.IsNullOrWhiteSpace(configured))
        {
            try
            {
                var parsed = new Uri(configured);
                return $"{parsed.Scheme}://{parsed.Authority}";
            }
            catch
            {
                return configured.TrimEnd('/');
            }
        }

        return "https://filmhub-frontend.delightfuldune-f7916078.francecentral.azurecontainerapps.io";
    }

    private static string CreateRandomUrlSafeToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static void AppendRefreshTokenCookie(HttpContext context, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var isHttps = context.Request.IsHttps || context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction();

        context.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromDays(7)
        });
    }

    private static void DeleteRefreshTokenCookie(HttpContext context)
    {
        var isHttps = context.Request.IsHttps || context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction();

        context.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }

    private static int? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && int.TryParse(userIdClaim, out var userId)) return userId;
        return null;
    }
}
