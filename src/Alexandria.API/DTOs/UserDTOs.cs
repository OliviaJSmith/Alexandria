namespace Alexandria.API.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class CreateUserDto
{
    public string GoogleId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class UpdateUserDto
{
    public string? Name { get; set; }
    public string? UserName { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class FriendDto
{
    public int Id { get; set; }
    public UserDto Friend { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
