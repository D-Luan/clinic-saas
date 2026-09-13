using HealthBr.Application.Common.Security;

namespace HealthBr.UnitTests.Security;

public sealed class RefreshTokenGeneratorTests
{
    [Fact]
    public void Generate_ShouldReturnUrlSafeTokenOf256Bits()
    {
        var token = RefreshTokenGenerator.Generate();

        // 32 bytes encode to 43 unpadded base64url characters.
        Assert.Equal(43, token.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", token);
    }

    [Fact]
    public void Generate_ShouldNotRepeat()
    {
        Assert.NotEqual(RefreshTokenGenerator.Generate(), RefreshTokenGenerator.Generate());
    }

    [Fact]
    public void Hash_ShouldBeDeterministicSha256Hex()
    {
        var first = RefreshTokenGenerator.Hash("token-value");
        var second = RefreshTokenGenerator.Hash("token-value");

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
        Assert.Matches("^[0-9A-F]{64}$", first);
        Assert.NotEqual(first, RefreshTokenGenerator.Hash("other-value"));
    }

    [Theory]
    [InlineData("", "...")]
    [InlineData("12345678", "...")]
    [InlineData("123456789", "12345678...")]
    [InlineData("abcdefghijklmnop", "abcdefgh...")]
    public void TokenPreview_ShouldShowAtMostFirstEightCharacters(string token, string expected)
    {
        Assert.Equal(expected, AuthConstants.TokenPreview(token));
    }
}
