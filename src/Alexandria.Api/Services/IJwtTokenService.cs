using Alexandria.Api.Models;

namespace Alexandria.Api.Services;

public sealed record IssuedToken(string Token, DateTimeOffset ExpiresAt);

public interface IJwtTokenService
{
    IssuedToken CreateToken(User user);
}
