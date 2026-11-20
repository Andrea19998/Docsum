using Docsum.Application.Abstractions;

namespace Docsum.Application.Tests.Fakes;


public class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hashed:" + password;
    public bool Verify(string password, string hash) => hash == "hashed:" + password;
}