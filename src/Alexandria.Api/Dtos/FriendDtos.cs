using System.ComponentModel.DataAnnotations;
using Alexandria.Api.Models;

namespace Alexandria.Api.Dtos;

/// <summary>Public face of another user: no email.</summary>
public sealed record UserSummaryDto(Guid Id, string UserName, string DisplayName, string? ProfilePictureUrl)
{
    public static UserSummaryDto FromUser(User user) => new(user.Id, user.UserName, user.DisplayName, user.ProfilePictureUrl);
}

/// <summary>
/// A friendship or friend request, seen from the signed-in user's side.
/// <see cref="IsIncoming"/> is true when the other user sent the request.
/// </summary>
public sealed record FriendshipDto(
    Guid FriendshipId,
    UserSummaryDto User,
    FriendshipStatus Status,
    bool IsIncoming,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>Maps a friendship whose Requester and Addressee are loaded, from <paramref name="viewerId"/>'s point of view.</summary>
    public static FriendshipDto For(Friendship friendship, Guid viewerId)
    {
        var isIncoming = friendship.AddresseeId == viewerId;
        var other = isIncoming ? friendship.Requester : friendship.Addressee;
        return new FriendshipDto(
            friendship.Id,
            UserSummaryDto.FromUser(other ?? throw new InvalidOperationException("Friendship users must be loaded before mapping.")),
            friendship.Status,
            isIncoming,
            friendship.CreatedAt,
            friendship.UpdatedAt);
    }
}

public sealed record FriendRequestsDto(IReadOnlyList<FriendshipDto> Incoming, IReadOnlyList<FriendshipDto> Outgoing);

public enum UserRelationship
{
    None,
    Friends,
    RequestSent,
    RequestReceived,
}

public sealed record UserSearchResultDto(UserSummaryDto User, UserRelationship Relationship, Guid? FriendshipId);

public class SendFriendRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    /// <summary>3–30 letters, digits or underscores; must be unique (case-insensitive).</summary>
    [Required]
    [StringLength(30, MinimumLength = 3)]
    [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "Usernames can only contain letters, numbers and underscores.")]
    public string UserName { get; set; } = string.Empty;
}

public sealed record UserNameAvailabilityDto(string UserName, bool IsValid, bool IsAvailable);
