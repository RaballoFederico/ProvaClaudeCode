using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FilmAPI.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/register", async (
            RegisterRequest request,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager) =>
        {
            var existing = await userManager.FindByEmailAsync(request.Email);
            if (existing is not null)
            {
                return Results.Conflict("Email already registered");
            }

            var user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email
            };

            var created = await userManager.CreateAsync(user, request.Password);
            if (!created.Succeeded)
            {
                return Results.BadRequest(created.Errors.Select(e => e.Description));
            }

            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                await userManager.SetAuthenticationTokenAsync(user, "local", "full_name", request.FullName);
            }

            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            await userManager.AddToRoleAsync(user, "User");

            return Results.Created($"/auth/users/{user.Id}", new { user.Id, user.Email });
        });

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<AppUser> userManager,
            TokenService tokenService,
            FilmDbContext db,
            IOptions<AuthSettings> options) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var valid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!valid)
            {
                return Results.Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);
            var (token, expiresAtUtc) = tokenService.CreateAccessToken(user, roles);
            var refreshToken = tokenService.CreateRefreshToken();
            var refreshTokenHash = tokenService.HashRefreshToken(refreshToken);

            db.RefreshTokens.Add(new RefreshToken
            {
                TokenHash = refreshTokenHash,
                UserId = user.Id,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(options.Value.RefreshTokenDays)
            });
            await db.SaveChangesAsync();

            return Results.Ok(new AuthResponse(token, refreshToken, expiresAtUtc));
        });

        group.MapPost("/refresh", async (
            RefreshRequest request,
            UserManager<AppUser> userManager,
            TokenService tokenService,
            FilmDbContext db,
            IOptions<AuthSettings> options) =>
        {
            var refreshTokenHash = tokenService.HashRefreshToken(request.RefreshToken);

            var stored = await db.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.TokenHash == refreshTokenHash);

            if (stored is null || stored.RevokedAtUtc is not null || stored.ExpiresAtUtc < DateTime.UtcNow)
            {
                return Results.Unauthorized();
            }

            if (stored.User is null)
            {
                return Results.Unauthorized();
            }

            stored.RevokedAtUtc = DateTime.UtcNow;

            var roles = await userManager.GetRolesAsync(stored.User);
            var (token, expiresAtUtc) = tokenService.CreateAccessToken(stored.User, roles);
            var newRefreshToken = tokenService.CreateRefreshToken();
            var newHash = tokenService.HashRefreshToken(newRefreshToken);

            db.RefreshTokens.Add(new RefreshToken
            {
                TokenHash = newHash,
                UserId = stored.User.Id,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(options.Value.RefreshTokenDays)
            });

            await db.SaveChangesAsync();

            return Results.Ok(new AuthResponse(token, newRefreshToken, expiresAtUtc));
        });

        group.MapPost("/logout", async (
            RefreshRequest request,
            TokenService tokenService,
            FilmDbContext db) =>
        {
            var refreshTokenHash = tokenService.HashRefreshToken(request.RefreshToken);
            var stored = await db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == refreshTokenHash);
            if (stored is null)
            {
                return Results.NoContent();
            }

            stored.RevokedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPost("/change-password", async (
            ChangePasswordRequest request,
            UserManager<AppUser> userManager,
            HttpContext httpContext) =>
        {
            var userId = httpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return Results.BadRequest(result.Errors.Select(e => e.Description));
            }

            return Results.NoContent();
        }).RequireAuthorization();

        group.MapGet("/me", async (
            UserManager<AppUser> userManager,
            HttpContext httpContext) =>
        {
            var userId = httpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);
            var fullName = await userManager.GetAuthenticationTokenAsync(user, "local", "full_name");
            return Results.Ok(new UserResponse(user.Id, user.Email ?? string.Empty, fullName, roles));
        }).RequireAuthorization();

        return group;
    }
}
