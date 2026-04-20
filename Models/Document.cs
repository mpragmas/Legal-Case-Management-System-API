namespace LegalCaseAPI.Models;

public class Document
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string Size { get; set; } = "";
    public string UploadedBy { get; set; } = ""; // userId
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
