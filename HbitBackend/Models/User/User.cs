using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HbitBackend.Models.User;

public class User : IdentityUser<int>
{
    // IdentityUser (string key) zawiera Id (string), UserName i Email

    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    public string Surname { get; set; } = null!;
    
    public DateTimeOffset? DateOfBirth { get; set; }
    
    public ICollection<Activity.Activity> Activities { get; set; } = new List<Activity.Activity>();

    public Models.HeartRateZones.HeartRateZones? HeartRateZones { get; set; }
}