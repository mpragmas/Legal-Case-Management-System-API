namespace LegalCaseAPI.Models;

public class ApplicationUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FullName { get; set; } = "";
    public string Role { get; set; } = ""; // "lawyer" | "client"
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? PhoneNumber { get; set; }
}
