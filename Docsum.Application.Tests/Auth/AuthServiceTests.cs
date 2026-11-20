using Docsum.Application.Auth;
using Docsum.Application.Tests.Fakes;

namespace Docsum.Application.Tests.Auth;

public class AuthServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_users, new FakePasswordHasher());
    }

    [Fact]
    public async Task Register_WithValidData_CreatesUser()
    {
        var request = new RegisterRequest("test@example.com", "supersecret123");
        
        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(_users.Users);
    }

    [Fact]
    public async Task Register_NormalizesEmailToLowercase()
    {
        await _sut.RegisterAsync(new RegisterRequest("  Test@Example.COM ", "supersecret123"), CancellationToken.None);

        Assert.Equal("test@example.com", _users.Users[0].Email);
    }

    [Fact]
    public async Task Register_StoresHashNotPlainPassword()
    {
        await _sut.RegisterAsync(new RegisterRequest("test@example.com", "supersecret123"), CancellationToken.None);

        Assert.NotEqual("supersecret123", _users.Users[0].PasswordHash);
    }

    [Fact]
    public async Task Register_WithShortPassword_Fails()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest("test@example.com", "short"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Empty(_users.Users);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Fails()
    {
        var request = new RegisterRequest("test@example.com", "supersecret123");
        await _sut.RegisterAsync(request, CancellationToken.None);

        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Single(_users.Users);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_Succeeds()
    {
        await _sut.RegisterAsync(new RegisterRequest("test@example.com", "supersecret123"), CancellationToken.None);

        var result = await _sut.LoginAsync(new LoginRequest("test@example.com", "supersecret123"), CancellationToken.None);

        Assert.True(result.Success);
    }

    [Theory]
    [InlineData("test@example.com", "wrongpassword")]
    [InlineData("nobody@example.com", "supersecret123")]
    public async Task Login_WithBadCredentials_FailsWithSameMessage(string email, string password)
    {
        await _sut.RegisterAsync(new RegisterRequest("test@example.com", "supersecret123"), CancellationToken.None);

        var result = await _sut.LoginAsync(new LoginRequest(email, password), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Invalid email or password.", result.Error);
    }
}