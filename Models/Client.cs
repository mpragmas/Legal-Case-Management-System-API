namespace LegalCaseAPI.Models;

public class Client
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Avatar { get; set; } = "";
    public ICollection<LawyerRequest> Requests { get; set; } = new List<LawyerRequest>();
    public ICollection<Case> Cases { get; set; } = new List<Case>();
}
