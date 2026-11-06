namespace Alexandria.Api.Models;

public enum FriendshipStatus
{
    Pending,
    Accepted,
    Rejected,
    Blocked,
}

public class Friendship
{
    public Guid Id { get; set; }

    public Guid RequesterId { get; set; }

    public User? Requester { get; set; }

    public Guid AddresseeId { get; set; }

    public User? Addressee { get; set; }

    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
