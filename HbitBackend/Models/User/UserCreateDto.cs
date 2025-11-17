using System.ComponentModel.DataAnnotations;

namespace HbitBackend.Models.User;

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

    [Required]
    public string? Password { get; set; }
}