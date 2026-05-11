using System.Net;
using System.Net.Http.Json;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FilmAPI.Tests.Integration;

public class AuthSecurityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthSecurityIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsGenericMessage()
    {
        await _factory.ResetDatabaseAsync();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/forgot-password", new ForgotPasswordRequestDTO
        {
            Email = "missing@example.com",
            ReturnUrl = "http://localhost:5002/login.html"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("Se esiste un account associato");
    }

    [Fact]
    public async Task ResetPassword_Token_IsSingleUse_AndPersistedHashed()
    {
        await _factory.ResetDatabaseAsync();
        var client = await _factory.CreateUserClientAsync("mario", "User123!", "mario@test.local");
        var marioUserId = await _factory.WithDbContextAsync(async db =>
            (await db.Utenti.FirstAsync(u => u.Username == "mario")).Id);

        string rawToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var tokenService = scope.ServiceProvider.GetRequiredService<IAccountActionTokenService>();
            rawToken = await tokenService.CreateAsync("mario@test.local", AccountActionTokenPurpose.PasswordReset, TimeSpan.FromMinutes(30), marioUserId);
        }

        var tokenHash = await _factory.WithDbContextAsync(async db =>
        {
            var token = await db.AccountActionTokens
                .Where(t => t.Email == "mario@test.local" && t.Purpose == AccountActionTokenPurpose.PasswordReset)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();
            token.Should().NotBeNull();
            token!.UsedAt.Should().BeNull();
            return token.TokenHash;
        });

        tokenHash.Should().NotBeNullOrWhiteSpace();
        tokenHash.Should().NotContain("mario");

        var firstReset = await client.PostAsJsonAsync("/auth/reset-password", new ResetPasswordRequestDTO
        {
            Token = rawToken,
            NewPassword = "NewUser123!"
        });
        firstReset.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondReset = await client.PostAsJsonAsync("/auth/reset-password", new ResetPasswordRequestDTO
        {
            Token = rawToken,
            NewPassword = "Another123!"
        });
        secondReset.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AdminRoleChange_WritesSecurityAudit()
    {
        await _factory.ResetDatabaseAsync(async db =>
        {
            var userRole = await db.Ruoli.FirstAsync(r => r.Nome == "User");
            db.Utenti.Add(new Utente
            {
                Username = "targetuser",
                Email = "target@test.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                Attivo = true,
                DataRegistrazione = DateTime.UtcNow,
                UtentiRuoli = new List<UtenteRuolo> { new UtenteRuolo { RuoloId = userRole.Id } }
            });
        });

        var admin = await _factory.CreateAdminClientAsync();

        var targetId = await _factory.WithDbContextAsync(async db =>
            (await db.Utenti.FirstAsync(u => u.Username == "targetuser")).Id);
        var powerRoleId = await _factory.WithDbContextAsync(async db =>
            (await db.Ruoli.FirstAsync(r => r.Nome == "PowerUser")).Id);

        var response = await admin.PutAsJsonAsync($"/admin/utenti/{targetId}/ruoli", new UpdateRuoliRequestDTO
        {
            RuoloIds = new List<int> { powerRoleId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var auditExists = await _factory.WithDbContextAsync(async db =>
            await db.UserSecurityAuditLogs.AnyAsync(l => l.EventType == "admin_role_change" && l.TargetUserId == targetId));

        auditExists.Should().BeTrue();
    }
}
