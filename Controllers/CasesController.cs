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
        var cases = await _db.Cases
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
}
