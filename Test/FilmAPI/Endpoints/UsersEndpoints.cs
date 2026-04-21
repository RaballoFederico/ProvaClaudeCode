using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Endpoints;

public static class UsersEndpoints
{
    public static RouteGroupBuilder MapUsersEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (UserManager<AppUser> userManager) =>
        {
            var users = await userManager.Users.ToListAsync();
            var result = new List<UserResponse>();

            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                var fullName = await userManager.GetAuthenticationTokenAsync(user, "local", "full_name");
                result.Add(new UserResponse(user.Id, user.Email ?? string.Empty, fullName, roles));
            }

            return Results.Ok(result);
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/{id}", async (string id, UserManager<AppUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null)
            {
                return Results.NotFound();
            }

            var roles = await userManager.GetRolesAsync(user);
            var fullName = await userManager.GetAuthenticationTokenAsync(user, "local", "full_name");
            return Results.Ok(new UserResponse(user.Id, user.Email ?? string.Empty, fullName, roles));
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapPost("/roles", async (
            AssignRoleRequest request,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Results.NotFound("User not found");
            }

            if (!await roleManager.RoleExistsAsync(request.Role))
            {
                await roleManager.CreateAsync(new IdentityRole(request.Role));
            }

            var alreadyInRole = await userManager.IsInRoleAsync(user, request.Role);
            if (alreadyInRole)
            {
                return Results.NoContent();
            }

            var result = await userManager.AddToRoleAsync(user, request.Role);
            if (!result.Succeeded)
            {
                return Results.BadRequest(result.Errors.Select(e => e.Description));
            }

            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapDelete("/roles", async (
            AssignRoleRequest request,
            UserManager<AppUser> userManager) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Results.NotFound("User not found");
            }

            var result = await userManager.RemoveFromRoleAsync(user, request.Role);
            if (!result.Succeeded)
            {
                return Results.BadRequest(result.Errors.Select(e => e.Description));
            }

            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        return group;
    }
}
