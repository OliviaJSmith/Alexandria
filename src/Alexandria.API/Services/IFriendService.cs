using Alexandria.API.DTOs;

namespace Alexandria.API.Services;

public interface IFriendService
{
    Task<IEnumerable<FriendDto>> GetFriendsAsync(int userId);
    Task<IEnumerable<FriendRequestDto>> GetPendingRequestsAsync(int userId);
    Task<UserDto?> SearchUserByEmailAsync(int currentUserId, string email);
    Task<ServiceResult> SendFriendRequestAsync(int userId, int friendId);
    Task<ServiceResult> AcceptFriendRequestAsync(int userId, int friendshipId);
    Task<ServiceResult> RemoveFriendAsync(int userId, int friendshipId);
}
