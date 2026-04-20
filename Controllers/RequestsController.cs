using LegalCaseAPI.Data;
using LegalCaseAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegalCaseAPI.Controllers;

[ApiController]
[Route("api/requests")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    public RequestsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var requests = await _db.LawyerRequests
            .Select(r => new
            {
                id = r.Id,
                clientId = r.ClientId,
                lawyerId = r.LawyerId,
                status = r.Status,
                message = r.Message,
                createdAt = r.RequestedAt
            }).ToListAsync();
        return Ok(requests);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequestDto dto)
    {
        var req = new LawyerRequest
        {
            LawyerId = dto.LawyerId,
            ClientId = dto.ClientId,
            Status = "pending",
            Message = dto.Message,
            RequestedAt = DateTime.UtcNow
        };
        _db.LawyerRequests.Add(req);
        await _db.SaveChangesAsync();
        return Ok(new { id = req.Id, clientId = req.ClientId, lawyerId = req.LawyerId, status = req.Status, message = req.Message, createdAt = req.RequestedAt });
    }

    [HttpPut("{id}/accept")]
    public async Task<IActionResult> Accept(int id)
    {
        var req = await _db.LawyerRequests.FindAsync(id);
        if (req == null) return NotFound();

        req.Status = "approved";

        var client = await _db.Clients.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == req.ClientId);
        var newCase = new Case
        {
            LawyerId = req.LawyerId,
            ClientId = req.ClientId,
            RequestId = req.Id,
            Title = $"New Case — {client?.FullName ?? "Client"}",
            Description = req.Message,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };
        _db.Cases.Add(newCase);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Accepted", caseId = newCase.Id });
    }

    [HttpPut("{id}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        var req = await _db.LawyerRequests.FindAsync(id);
        if (req == null) return NotFound();
        req.Status = "rejected";
        await _db.SaveChangesAsync();
        return Ok(new { message = "Rejected" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var req = await _db.LawyerRequests.FindAsync(id);
        if (req == null) return NotFound();
        _db.LawyerRequests.Remove(req);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}

public record CreateRequestDto(int LawyerId, int ClientId, string Message);
