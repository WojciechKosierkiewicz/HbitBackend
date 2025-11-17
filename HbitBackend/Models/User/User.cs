using System.ComponentModel.DataAnnotations;

namespace HbitBackend.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = null!;

    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    public string Surname { get; set; } = null!;
    
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}