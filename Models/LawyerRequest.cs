namespace LegalCaseAPI.Models;

public class LawyerRequest
{
    public int Id { get; set; }
    public int LawyerId { get; set; }
    public Lawyer Lawyer { get; set; } = null!;
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public string Status { get; set; } = "pending"; // pending | approved | rejected
    public string Message { get; set; } = "";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
