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

    // ✅ FIXED constructor name
    public LawyersController(AppDbContext db, IWebHostEnvironment env)
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
        var query = _db.Lawyers
            .Include(l => l.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(l =>
                l.FullName.ToLower().Contains(term) ||
                l.Specialization.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(specialization) && specialization != "all")
        {
            query = query.Where(l => l.Specialization == specialization);
        }

        if (minExperience.HasValue)
            query = query.Where(l => l.YearsOfExperience >= minExperience.Value);

        if (maxExperience.HasValue)
            query = query.Where(l => l.YearsOfExperience <= maxExperience.Value);

        query = query.OrderBy(l => l.Id);

        var projected = query.Select(l => new
        {
            id = l.Id,
            name = l.FullName,
            email = l.User.Email,
            avatar = l.Avatar,
            specialization = l.Specialization,
            experience = l.YearsOfExperience,
            bio = l.Bio,
            rating = l.Rating,
            casesWon = l.CasesWon,
            maxClients = l.MaxClients,
            activeClients = _db.Cases.Count(c => c.LawyerId == l.Id && c.Status == "active")
        });

        if (!page.HasValue && !pageSize.HasValue)
            return Ok(await projected.ToListAsync());

        var p = page.GetValueOrDefault(1);
        var ps = pageSize.GetValueOrDefault(6);

        var totalCount = await projected.CountAsync();
        var items = await projected.Skip((p - 1) * ps).Take(ps).ToListAsync();

        return Ok(new { items, page = p, pageSize = ps, totalCount });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var l = await _db.Lawyers.Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (l == null) return NotFound();

        var activeClients = await _db.Cases
            .CountAsync(c => c.LawyerId == l.Id && c.Status == "active");

        return Ok(new
        {
            id = l.Id,
            name = l.FullName,
            email = l.User.Email,
            avatar = l.Avatar,
            specialization = l.Specialization,
            experience = l.YearsOfExperience,
            bio = l.Bio,
            rating = l.Rating,
            casesWon = l.CasesWon,
            maxClients = l.MaxClients,
            activeClients
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLawyerDto dto)
    {
        var lawyer = await _db.Lawyers
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lawyer == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.FullName))
        {
            lawyer.FullName = dto.FullName;
            lawyer.User.FullName = dto.FullName;
        }

        if (!string.IsNullOrWhiteSpace(dto.Specialization))
            lawyer.Specialization = dto.Specialization;

        if (!string.IsNullOrWhiteSpace(dto.Bio))
            lawyer.Bio = dto.Bio;

        if (dto.YearsOfExperience.HasValue)
            lawyer.YearsOfExperience = dto.YearsOfExperience.Value;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Lawyer updated successfully" });
    }
}

public record UpdateLawyerDto(
    string? FullName,
    int? YearsOfExperience,
    string? Specialization,
    string? Bio,
    string? Avatar,
    int? MaxClients
);