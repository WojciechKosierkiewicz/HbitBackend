namespace HbitBackend.Models;

public class UserGetDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Name { get; set; }
    public string? Surname { get; set; }

    // count of related activities to avoid sending full collection
    public int ActivitiesCount { get; set; }
}