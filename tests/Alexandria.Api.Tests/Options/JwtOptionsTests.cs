using Alexandria.Api.Options;

namespace Alexandria.Api.Tests.Options;

public class JwtOptionsTests
{
    [Fact]
    public void HasValidSigningKey_With32ByteBase64Key_IsTrue()
    {
        Assert.True(TestData.JwtOptions().HasValidSigningKey());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64!")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA==")] // 16 bytes: too short for HMAC-SHA256
    public void HasValidSigningKey_WithMissingMalformedOrShortKey_IsFalse(string signingKey)
    {
        var options = TestData.JwtOptions();
        options.SigningKey = signingKey;

        Assert.False(options.HasValidSigningKey());
    }

    [Fact]
    public void GetSigningKeyBytes_DecodesBase64()
    {
        Assert.Equal(new byte[32], TestData.JwtOptions().GetSigningKeyBytes());
    }
}
