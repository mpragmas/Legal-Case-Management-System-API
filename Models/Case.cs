namespace LegalCaseAPI.Models;

public class Case
{
    public int Id { get; set; }
    public int LawyerId { get; set; }
    public Lawyer Lawyer { get; set; } = null!;
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public int? RequestId { get; set; }
    public LawyerRequest? Request { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "active"; // active | closed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
