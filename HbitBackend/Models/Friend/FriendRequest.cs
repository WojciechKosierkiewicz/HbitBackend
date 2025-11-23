namespace HbitBackend.Models.Friend;

public class FriendRequest
{
    public int Id { get; set; }

    public int FromUserId { get; set; }
    public int ToUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RespondedAt { get; set; }

    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

    public HbitBackend.Models.User.User? FromUser { get; set; }
    public HbitBackend.Models.User.User? ToUser { get; set; }
}

public enum FriendRequestStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2
}
