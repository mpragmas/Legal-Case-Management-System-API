namespace LegalCaseAPI.Models;

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}
