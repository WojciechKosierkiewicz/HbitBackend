using System.ComponentModel.DataAnnotations;

namespace HbitBackend.Models.Activity;

public class Activity
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = null!;
    public DateTime Date { get; set; }
    public ActivityType Type { get; set; }
    
    
    // Klucz obcy do User.Id (int)
    public int UserId { get; set; }
    public User.User User { get; set; } = null!;

    // Nawigacyjna kolekcja próbek tętna powiązana z tą aktywnością
    public List<HbitBackend.Models.HeartRateSample.HeartRateSample> HeartRateSamples { get; set; } = new();
}