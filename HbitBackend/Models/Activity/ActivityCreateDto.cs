using System.ComponentModel.DataAnnotations;

namespace HbitBackend.Models.Activity;

public class ActivityCreateDto
{
    [Required]
    public ActivityType? ActivityType { get; set; }
    
    [Required]
    public DateTime? Date { get; set; }

    [Required] 
    public string Name { get; set; } = null!;
}