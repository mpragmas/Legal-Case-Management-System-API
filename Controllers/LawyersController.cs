using LegalCaseAPI.Data;
using LegalCaseAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegalCaseAPI.Controllers;

[ApiController]
[Route("api/lawyers")]
public class LawyersController : ControllerBase
{
    private readonly AppDbContext _db;
    public LawyersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var lawyers = await _db.Lawyers
            .Include(l => l.User)
            .Select(l => new
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
                maxClients = l.MaxClients
            }).ToListAsync();
        return Ok(lawyers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var l = await _db.Lawyers.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (l == null) return NotFound();
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
            maxClients = l.MaxClients
        });
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLawyerDto dto)
    {
        var lawyer = await _db.Lawyers.FindAsync(id);
        if (lawyer == null) return NotFound();

        if (!string.IsNullOrEmpty(dto.FullName)) lawyer.FullName = dto.FullName;
        if (!string.IsNullOrEmpty(dto.Specialization)) lawyer.Specialization = dto.Specialization;
        if (!string.IsNullOrEmpty(dto.Bio)) lawyer.Bio = dto.Bio;
        if (dto.YearsOfExperience.HasValue) lawyer.YearsOfExperience = dto.YearsOfExperience.Value;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Updated" });
    }
}

public record UpdateLawyerDto(string? FullName, string? Specialization, string? Bio, int? YearsOfExperience);
