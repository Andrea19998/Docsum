using Docsum.Application.Abstractions;
using Docsum.Domain;

namespace Docsum.Application.Auth;

public class AuthService(IUserRepository users, IPasswordHasher hasher)
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        if (!email.Contains('@'))
            return AuthResult.Fail("Invalid email.");
        if (req.Password.Length < 8)
            return AuthResult.Fail("Password must be at least 8 characters.");
        if (await users.GetByEmailAsync(email, ct) is not null)
            return AuthResult.Fail("Email is already registered.");

        var user = new User { Email = email, PasswordHash = hasher.Hash(req.Password) };
        await users.AddAsync(user, ct);

        return AuthResult.Ok(new UserDto(user.Id, user.Email));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email, ct);

        if (user is null || !hasher.Verify(req.Password, user.PasswordHash))
            return AuthResult.Fail("Invalid email or password.");

        return AuthResult.Ok(new UserDto(user.Id, user.Email));
    }
}