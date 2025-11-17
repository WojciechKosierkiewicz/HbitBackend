using System.ComponentModel.DataAnnotations;

namespace HbitBackend.Models;

public class UserCreateDto
{
    [Required]
    public string? UserName { get; set; }
    
    [Required]
    public string? Name { get; set; }
    
    [Required]
    public string? Surname { get; set; }
    
    [Required]
    public string? Email { get; set;}
}