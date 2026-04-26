using LegalCaseAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegalCaseAPI.Controllers;

[ApiController]
[Route("api/cases")]
[Authorize]
public class CasesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CasesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var profileId = int.Parse(User.FindFirst("profileId")?.Value ?? "0");
        var role = User.FindFirst("role")?.Value;

        var query = _db.Cases.AsQueryable();

        if (role == "lawyer")
            query = query.Where(c => c.LawyerId == profileId);
        else
            query = query.Where(c => c.ClientId == profileId);

        var cases = await query
            .Select(c => new
            {
                id = c.Id,
                title = c.Title,
                description = c.Description,
                status = c.Status,
                clientId = c.ClientId,
                lawyerId = c.LawyerId,
                requestId = c.RequestId,
                createdAt = c.CreatedAt
            }).ToListAsync();
        return Ok(cases);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _db.Cases.FindAsync(id);
        if (c == null) return NotFound();
        return Ok(new
        {
            id = c.Id,
            title = c.Title,
            description = c.Description,
            status = c.Status,
            clientId = c.ClientId,
            lawyerId = c.LawyerId,
            requestId = c.RequestId,
            createdAt = c.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCaseDto dto)
    {
        var profileId = int.Parse(User.FindFirst("profileId")?.Value ?? "0");
        var role = User.FindFirst("role")?.Value;

        if (role != "lawyer")
            return StatusCode(403, new { message = "Only lawyers can update case status" });

        var c = await _db.Cases.FindAsync(id);
        if (c == null)
            return NotFound(new { message = "Case not found" });

        if (c.LawyerId != profileId)
            return StatusCode(403, new { message = "You are not assigned to this case" });

        if (!string.IsNullOrEmpty(dto.Status))
        {
            // Use raw SQL to avoid any EF Core column-mapping issues
            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE Cases SET Status = {0} WHERE Id = {1}",
                dto.Status, id);
            c.Status = dto.Status;
        }

        return Ok(new { id = c.Id, title = c.Title, description = c.Description, status = c.Status, clientId = c.ClientId, lawyerId = c.LawyerId, requestId = c.RequestId, createdAt = c.CreatedAt });
    }
}

public record UpdateCaseDto(string? Status);
