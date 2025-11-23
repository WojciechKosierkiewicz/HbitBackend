namespace HbitBackend.Models.Friend;

public class FriendRequestDto
{
    public int Id { get; set; }
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public FriendRequestStatus Status { get; set; }
}

