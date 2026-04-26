using LegalCaseAPI.Data;
using LegalCaseAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegalCaseAPI.Controllers;

[ApiController]
[Route("api/lawyers")]
[Authorize]
public class LawyersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    public DocumentsController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? q,
        [FromQuery] string? specialization,
        [FromQuery] int? minExperience,
        [FromQuery] int? maxExperience)
    {
        var profileId = int.Parse(User.FindFirst("profileId")?.Value ?? "0");
        var role = User.FindFirst("role")?.Value;

        var query = _db.Documents.Include(d => d.Case).AsQueryable();

        if (role == "lawyer")
            query = query.Where(d => d.Case.LawyerId == profileId);
        else
            query = query.Where(d => d.Case.ClientId == profileId);

        var docs = await query.Select(d => new
        {
            id = d.Id,
            caseId = d.CaseId,
            name = d.FileName,
            filePath = d.FilePath,
            size = d.Size,
            uploadedBy = d.UploadedBy,
            uploadedAt = d.UploadedAt
        }).ToListAsync();
        return Ok(docs);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] int caseId, [FromForm] string uploadedBy, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        // Verify the current user is part of this case
        var profileId = int.Parse(User.FindFirst("profileId")?.Value ?? "0");
        var role = User.FindFirst("role")?.Value;
        var caseItem = await _db.Cases.FindAsync(caseId);
        if (caseItem == null) return NotFound(new { message = "Case not found" });

        var isLawyerOnCase = role == "lawyer" && caseItem.LawyerId == profileId;
        var isClientOnCase = role == "client" && caseItem.ClientId == profileId;
        if (!isLawyerOnCase && !isClientOnCase) return Forbid();

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

        var uploadsPath = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(uploadsPath);
        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var sizeLabel = file.Length >= 1_000_000
            ? $"{file.Length / 1_000_000.0:0.0} MB"
            : $"{file.Length / 1000} KB";

        var userId = User.FindFirst("sub")?.Value ?? uploadedBy;
        var doc = new Document
        {
            CaseId = caseId,
            FileName = file.FileName,
            FilePath = $"/uploads/{fileName}",
            Size = sizeLabel,
            UploadedBy = userId,
            UploadedAt = DateTime.UtcNow
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        return Ok(new { id = doc.Id, caseId = doc.CaseId, name = doc.FileName, size = doc.Size, uploadedBy = doc.UploadedBy, uploadedAt = doc.UploadedAt });
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var doc = await _db.Documents.Include(d => d.Case).FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) return NotFound(new { message = "Document not found" });

        var profileId = int.Parse(User.FindFirst("profileId")?.Value ?? "0");
        var role = User.FindFirst("role")?.Value;
        var isLawyerOnCase = role == "lawyer" && doc.Case.LawyerId == profileId;
        var isClientOnCase = role == "client" && doc.Case.ClientId == profileId;
        if (!isLawyerOnCase && !isClientOnCase) return Forbid();

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

        var fullPath = Path.Combine(webRoot, doc.FilePath.TrimStart('/'));
        if (!System.IO.File.Exists(fullPath))
            return NotFound(new { message = "File not found on server" });

        var contentType = "application/octet-stream";
        var ext = Path.GetExtension(doc.FileName).ToLowerInvariant();
        if (ext == ".pdf") contentType = "application/pdf";
        else if (ext is ".jpg" or ".jpeg") contentType = "image/jpeg";
        else if (ext == ".png") contentType = "image/png";
        else if (ext is ".doc" or ".docx") contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        var stream = System.IO.File.OpenRead(fullPath);
        return File(stream, contentType, doc.FileName);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var doc = await _db.Documents.Include(d => d.Case).FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) return NotFound();

        // Only a user who is part of this case can delete documents
        var profileId = int.Parse(User.FindFirst("profileId")?.Value ?? "0");
        var role = User.FindFirst("role")?.Value;
        var isLawyerOnCase = role == "lawyer" && doc.Case.LawyerId == profileId;
        var isClientOnCase = role == "client" && doc.Case.ClientId == profileId;
        if (!isLawyerOnCase && !isClientOnCase) return Forbid();

        if (!string.IsNullOrEmpty(doc.FilePath))
        {
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
                webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

            var fullPath = Path.Combine(webRoot, doc.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}
