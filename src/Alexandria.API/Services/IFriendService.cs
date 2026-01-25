using Alexandria.API.DTOs;

namespace Alexandria.API.Services;

public interface IFriendService
{
    Task<IEnumerable<FriendDto>> GetFriendsAsync(int userId);
    Task<ServiceResult> SendFriendRequestAsync(int userId, int friendId);
    Task<ServiceResult> AcceptFriendRequestAsync(int userId, int friendshipId);
    Task<ServiceResult> RemoveFriendAsync(int userId, int friendshipId);
}
