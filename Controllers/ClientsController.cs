using LegalCaseAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegalCaseAPI.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClientsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clients = await _db.Clients
            .Include(c => c.User)
            .Select(c => new
            {
                id = c.Id,
                name = c.FullName,
                email = c.User.Email,
                avatar = c.Avatar,
                phone = c.Phone,
                address = c.Address
            }).ToListAsync();
        return Ok(clients);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _db.Clients.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        return Ok(new
        {
            id = c.Id,
            name = c.FullName,
            email = c.User.Email,
            avatar = c.Avatar,
            phone = c.Phone,
            address = c.Address
        });
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClientDto dto)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null) return NotFound();

        if (!string.IsNullOrEmpty(dto.FullName)) client.FullName = dto.FullName;
        if (dto.Phone != null) client.Phone = dto.Phone;
        if (dto.Address != null) client.Address = dto.Address;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Updated" });
    }
}

public record UpdateClientDto(string? FullName, string? Phone, string? Address);
