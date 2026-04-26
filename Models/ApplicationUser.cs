namespace LegalCaseAPI.Models;

public class ApplicationUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FullName { get; set; } = "";
    public string Role { get; set; } = ""; // "lawyer" | "client"
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? PhoneNumber { get; set; }

    // 2FA
    public bool TwoFactorEnabled { get; set; } = false;
    public string? TwoFactorCode { get; set; }
    public DateTime? TwoFactorExpiry { get; set; }

    // Password Reset
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }
}
