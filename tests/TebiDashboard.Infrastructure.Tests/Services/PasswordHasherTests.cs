using TebiDashboard.Infrastructure.Services;

namespace TebiDashboard.Infrastructure.Tests.Services;

/// <summary>
/// Password hashes must verify and must not keep the original password text.
/// </summary>
public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_VerifiesTheSamePassword_AndRejectsAnother()
    {
        var hash = PasswordHasher.Hash("admin1234");

        Assert.StartsWith("pbkdf2-sha256$", hash);
        Assert.DoesNotContain("admin1234", hash);
        Assert.True(PasswordHasher.Verify("admin1234", hash, out var upgraded));
        Assert.Null(upgraded);
        Assert.False(PasswordHasher.Verify("wrong-pass", hash, out _));
    }

    [Fact]
    public void Verify_UpgradesALegacyPlaintextPassword()
    {
        var matched = PasswordHasher.Verify("admin1234", "admin1234", out var upgraded);

        Assert.True(matched);
        Assert.NotNull(upgraded);
        Assert.True(PasswordHasher.Verify("admin1234", upgraded, out var secondUpgrade));
        Assert.Null(secondUpgrade);
    }

    [Fact]
    public void Verify_RejectsANullPassword()
    {
        Assert.False(PasswordHasher.Verify(null, "admin1234", out var upgraded));
        Assert.Null(upgraded);
    }
}
