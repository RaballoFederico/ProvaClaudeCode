namespace FilmAPI.DTO;

public record RegisterRequest(string Email, string Password, string? FullName);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record AssignRoleRequest(string Email, string Role);
