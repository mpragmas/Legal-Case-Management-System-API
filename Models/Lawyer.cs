namespace LegalCaseAPI.Models;

public class Lawyer
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    public string FullName { get; set; } = "";
    public int YearsOfExperience { get; set; }
    public string Specialization { get; set; } = "";
    public string Bio { get; set; } = "";
    public string Avatar { get; set; } = "";
    public double Rating { get; set; }
    public int CasesWon { get; set; }
    public int MaxClients { get; set; } = 2;
    public ICollection<LawyerRequest> Requests { get; set; } = new List<LawyerRequest>();
    public ICollection<Case> Cases { get; set; } = new List<Case>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
