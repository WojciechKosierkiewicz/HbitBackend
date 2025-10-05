using Microsoft.VisualBasic.CompilerServices;

namespace HbitBackend;

public class Race
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
    public bool IsActive { get; set; }

    public int UsersPosition(string userId)
    {
        return int.TryParse(userId, out var position) ? position : 0;
    }
}