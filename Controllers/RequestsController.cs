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
        var profileId = int.Parse(User.FindFirst("profileId")?.Value ?? "0");
        var role = User.FindFirst("role")?.Value;

        var query = _db.LawyerRequests.AsQueryable();

        if (role == "lawyer")
            query = query.Where(r => r.LawyerId == profileId);
        else
            query = query.Where(r => r.ClientId == profileId);

        var requests = await query
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
        // Block duplicate pending requests
        var hasPending = await _db.LawyerRequests.AnyAsync(r =>
            r.LawyerId == dto.LawyerId && r.ClientId == dto.ClientId && r.Status == "pending");
        if (hasPending)
            return BadRequest(new { message = "You already have a pending request with this lawyer" });

        // Block if client already has an active case with this lawyer
        var hasActiveCase = await _db.Cases.AnyAsync(c =>
            c.LawyerId == dto.LawyerId && c.ClientId == dto.ClientId && c.Status == "active");
        if (hasActiveCase)
            return BadRequest(new { message = "You already have an active case with this lawyer" });

        // Block if lawyer has reached their active case limit
        var lawyer = await _db.Lawyers.FindAsync(dto.LawyerId);
        if (lawyer != null)
        {
            var activeCount = await _db.Cases.CountAsync(c => c.LawyerId == dto.LawyerId && c.Status == "active");
            if (activeCount >= lawyer.MaxClients)
                return BadRequest(new { message = $"This lawyer is currently at full capacity ({lawyer.MaxClients} active cases)" });
        }

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

        // Notify the lawyer
        lawyer = await _db.Lawyers.FirstOrDefaultAsync(l => l.Id == dto.LawyerId);
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == dto.ClientId);
        if (lawyer != null)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = lawyer.UserId,
                Message = $"New representation request from {client?.FullName ?? "a client"}.",
                SentAt = DateTime.UtcNow,
                IsRead = false
            });
            await _db.SaveChangesAsync();
        }

        return Ok(new { id = req.Id, clientId = req.ClientId, lawyerId = req.LawyerId, status = req.Status, message = req.Message, createdAt = req.RequestedAt });
    }

    [HttpPut("{id}/accept")]
    public async Task<IActionResult> Accept(int id)
    {
        var profileId = int.Parse(User.FindFirst("profileId")?.Value ?? "0");
        var role = User.FindFirst("role")?.Value;

        var req = await _db.LawyerRequests.FindAsync(id);
        if (req == null) return NotFound();
        if (role != "lawyer" || req.LawyerId != profileId) return Forbid();
        if (req.Status == "approved") return BadRequest(new { message = "Request already accepted" });

        // Enforce active case cap at accept time
        var lawyer = await _db.Lawyers.FindAsync(req.LawyerId);
        if (lawyer != null)
        {
            var activeCount = await _db.Cases.CountAsync(c => c.LawyerId == req.LawyerId && c.Status == "active");
            if (activeCount >= lawyer.MaxClients)
                return BadRequest(new { message = $"You have reached your limit of {lawyer.MaxClients} active cases" });
        }

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

        // Notify the client
        if (client != null)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = client.UserId,
                Message = "Your representation request was accepted. A new case has been opened.",
                SentAt = DateTime.UtcNow,
                IsRead = false
            });
            await _db.SaveChangesAsync();
        }

        return Ok(new { message = "Accepted", caseId = newCase.Id });
    }

    [HttpPut("{id}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        var profileId = int.Parse(User.FindFirst("profileId")?.Value ?? "0");
        var role = User.FindFirst("role")?.Value;

        var req = await _db.LawyerRequests.FindAsync(id);
        if (req == null) return NotFound();
        if (role != "lawyer" || req.LawyerId != profileId) return Forbid();

        req.Status = "rejected";
        await _db.SaveChangesAsync();

        // Notify the client
        var client = await _db.Clients.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == req.ClientId);
        if (client != null)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = client.UserId,
                Message = "Your representation request was declined.",
                SentAt = DateTime.UtcNow,
                IsRead = false
            });
            await _db.SaveChangesAsync();
        }

        return Ok(new { message = "Rejected" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var req = await _db.LawyerRequests.FindAsync(id);
        if (req == null) return NotFound();

        var profileId = int.Parse(User.FindFirst("profileId")?.Value ?? "0");
        var role = User.FindFirst("role")?.Value;

        if (role != "client" || req.ClientId != profileId)
            return Forbid();

        _db.LawyerRequests.Remove(req);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}

public record CreateRequestDto(int LawyerId, int ClientId, string Message);
