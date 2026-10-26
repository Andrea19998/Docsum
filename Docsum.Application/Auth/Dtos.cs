namespace Docsum.Application.Auth;

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record UserDto(int Id, string Email);

public record AuthResult(bool Success, UserDto? User, string? Error)
{
    public static AuthResult Ok(UserDto user) => new(true, user, null);
    public static AuthResult Fail(string error) => new(false, null, error);
}