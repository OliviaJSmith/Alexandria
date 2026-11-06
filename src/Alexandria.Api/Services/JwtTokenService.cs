using System.Security.Claims;
using Alexandria.Api.Models;
using Alexandria.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Alexandria.Api.Services;

public class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : IJwtTokenService
{
    public IssuedToken CreateToken(User user)
    {
        var jwt = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(jwt.TokenLifetime);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(jwt.GetSigningKeyBytes()),
                SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new IssuedToken(token, expiresAt);
    }
}
