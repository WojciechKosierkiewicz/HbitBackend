using System.ComponentModel.DataAnnotations.Schema;

namespace HbitBackend.Models.Friend;

public class Friend
{
    // Composite key (UserAId, UserBId) enforced in DbContext
    public int UserAId { get; set; }
    public int UserBId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // optional navigation properties
    [ForeignKey(nameof(UserAId))]
    public HbitBackend.Models.User.User? UserA { get; set; }

    [ForeignKey(nameof(UserBId))]
    public HbitBackend.Models.User.User? UserB { get; set; }
}

