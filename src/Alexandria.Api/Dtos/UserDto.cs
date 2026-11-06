using Alexandria.Api.Models;

namespace Alexandria.Api.Dtos;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string UserName,
    string? ProfilePictureUrl,
    DateTimeOffset? TermsAcceptedAt)
{
    public static UserDto FromUser(User user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.UserName,
        user.ProfilePictureUrl,
        user.TermsAcceptedAt);
}
