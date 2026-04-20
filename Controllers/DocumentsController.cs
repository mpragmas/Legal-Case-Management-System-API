using LegalCaseAPI.Data;
using LegalCaseAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegalCaseAPI.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    public DocumentsController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var docs = await _db.Documents.Select(d => new
        {
            id = d.Id,
            caseId = d.CaseId,
            name = d.FileName,
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

        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsPath);
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var sizeLabel = file.Length >= 1_000_000
            ? $"{file.Length / 1_000_000.0:0.0} MB"
            : $"{file.Length / 1000} KB";

        var doc = new Document
        {
            CaseId = caseId,
            FileName = file.FileName,
            FilePath = $"/uploads/{fileName}",
            Size = sizeLabel,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        return Ok(new { id = doc.Id, caseId = doc.CaseId, name = doc.FileName, size = doc.Size, uploadedBy = doc.UploadedBy, uploadedAt = doc.UploadedAt });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var doc = await _db.Documents.FindAsync(id);
        if (doc == null) return NotFound();
        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}
