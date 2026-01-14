namespace Alexandria.API.Models;

public class Friendship
{
    public int Id { get; set; }
    public int RequesterId { get; set; }
    public int AddresseeId { get; set; }
    public FriendshipStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User Requester { get; set; } = null!;
    public User Addressee { get; set; } = null!;
}

public enum FriendshipStatus
{
    Pending,
    Accepted,
    Rejected,
    Blocked
}
