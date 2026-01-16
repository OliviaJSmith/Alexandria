namespace Alexandria.API.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

public class FriendDto
{
    public int Id { get; set; }
    public UserDto Friend { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
