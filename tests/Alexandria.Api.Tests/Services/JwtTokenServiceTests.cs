using Alexandria.Api.Options;
using Alexandria.Api.Services;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Alexandria.Api.Tests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtOptions _options = TestData.JwtOptions();

    [Fact]
    public async Task CreateToken_ProducesTokenThatValidatesWithConfiguredIssuerAudienceAndKey()
    {
        // Real "now" so the token is within its validity window when validated.
        var now = DateTimeOffset.UtcNow;
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(_options), TestData.TimeProviderAt(now).Object);
        var user = TestData.User();

        var issued = service.CreateToken(user);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(issued.Token, new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(_options.GetSigningKeyBytes()),
        });

        Assert.True(validation.IsValid, validation.Exception?.Message);
        Assert.Equal(user.Id.ToString(), validation.Claims[JwtRegisteredClaimNames.Sub]);
        Assert.Equal(user.Email, validation.Claims[JwtRegisteredClaimNames.Email]);
        Assert.True(validation.Claims.ContainsKey(JwtRegisteredClaimNames.Jti));
    }

    [Fact]
    public void CreateToken_ExpiresAfterConfiguredLifetime()
    {
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(_options), TestData.TimeProviderAt(TestData.Now).Object);

        var issued = service.CreateToken(TestData.User());

        Assert.Equal(TestData.Now.AddDays(7), issued.ExpiresAt);
        var token = new JsonWebToken(issued.Token);
        Assert.Equal(TestData.Now.AddDays(7).UtcDateTime, token.ValidTo);
        Assert.Equal(TestData.Now.UtcDateTime, token.IssuedAt);
    }

    [Fact]
    public async Task CreateToken_IsRejectedWhenValidatedWithADifferentKey()
    {
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(_options), TestData.TimeProviderAt(DateTimeOffset.UtcNow).Object);
        var issued = service.CreateToken(TestData.User());
        var otherKey = Enumerable.Repeat((byte)7, 32).ToArray();

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(issued.Token, new TokenValidationParameters
        {
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(otherKey),
        });

        Assert.False(validation.IsValid);
    }
}
