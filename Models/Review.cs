using System.ComponentModel.DataAnnotations;

namespace LegalCaseAPI.Models;

public class Review
{
    public int Id { get; set; }
    public int LawyerId { get; set; }
    public int ClientId { get; set; }
    
    [Range(1, 5)]
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Lawyer? Lawyer { get; set; }
    public Client? Client { get; set; }
}
