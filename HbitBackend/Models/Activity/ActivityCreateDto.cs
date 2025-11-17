using System.ComponentModel.DataAnnotations;

namespace HbitBackend.Models;

public class ActivityCreateDto
{
    [Required]
    public int? UserId { get; set; }
    
    [Required]
    public ActivityType? ActivityType { get; set; }
    
    [Required]
    public DateTime? Date { get; set; }

    [Required] 
    public string Name { get; set; } = null!;
}