namespace LegalCaseAPI.Models;

public class Appointment
{
    public int Id { get; set; }
    public int? CaseId { get; set; }
    public Case? Case { get; set; }
    public int LawyerId { get; set; }
    public int? ClientId { get; set; }
    public string Date { get; set; } = ""; // YYYY-MM-DD
    public string Time { get; set; } = ""; // HH:mm
    public int Duration { get; set; } = 60;
    public string Status { get; set; } = "available"; // available | confirmed | completed
    public bool ReminderSent { get; set; } = false;
    public string Notes { get; set; } = "";
}
