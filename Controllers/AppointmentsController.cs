using LegalCaseAPI.Data;
using LegalCaseAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegalCaseAPI.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AppointmentsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var profileId = int.Parse(User.FindFirst("profileId")?.Value ?? "0");
        var role = User.FindFirst("role")?.Value;

        var query = _db.Appointments.AsQueryable();

        if (role == "lawyer")
            query = query.Where(a => a.LawyerId == profileId);
        else
            // Clients see their own booked appointments OR available slots
            query = query.Where(a => a.ClientId == profileId || a.Status == "available");

        var apts = await query.Select(a => new
        {
            id = a.Id,
            lawyerId = a.LawyerId,
            clientId = a.ClientId,
            caseId = a.CaseId,
            date = a.Date,
            time = a.Time,
            duration = a.Duration,
            status = a.Status,
            notes = a.Notes
        }).ToListAsync();
        return Ok(apts);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        var apt = new Appointment
        {
            LawyerId = dto.LawyerId,
            ClientId = dto.ClientId,
            CaseId = dto.CaseId,
            Date = dto.Date,
            Time = dto.Time,
            Duration = dto.Duration,
            Status = dto.Status ?? "available",
            Notes = dto.Notes ?? ""
        };
        _db.Appointments.Add(apt);
        await _db.SaveChangesAsync();
        return Ok(new { id = apt.Id, lawyerId = apt.LawyerId, clientId = apt.ClientId, caseId = apt.CaseId, date = apt.Date, time = apt.Time, duration = apt.Duration, status = apt.Status, notes = apt.Notes });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAppointmentDto dto)
    {
        var apt = await _db.Appointments.FindAsync(id);
        if (apt == null) return NotFound();

        var profileId = int.Parse(User.FindFirst("profileId")?.Value ?? "0");
        var role = User.FindFirst("role")?.Value;

        if (role == "lawyer" && apt.LawyerId != profileId) return Forbid();
        if (role == "client" && dto.Status != null && dto.Status != "confirmed") return Forbid();

        var wasAvailable = apt.Status == "available";

        if (dto.ClientId.HasValue) apt.ClientId = dto.ClientId;
        if (dto.CaseId.HasValue) apt.CaseId = dto.CaseId;
        if (dto.Status != null) apt.Status = dto.Status;
        if (dto.Notes != null) apt.Notes = dto.Notes;
        if (dto.Date != null) apt.Date = dto.Date;
        if (dto.Time != null) apt.Time = dto.Time;
        if (dto.Duration.HasValue) apt.Duration = dto.Duration.Value;
        if (dto.ClearClient == true) { apt.ClientId = null; apt.CaseId = null; }

        await _db.SaveChangesAsync();

        // Notify lawyer when a slot is booked by a client
        if (wasAvailable && apt.Status == "confirmed")
        {
            var lawyer = await _db.Lawyers.FirstOrDefaultAsync(l => l.Id == apt.LawyerId);
            var client = apt.ClientId.HasValue
                ? await _db.Clients.FirstOrDefaultAsync(c => c.Id == apt.ClientId)
                : null;
            if (lawyer != null)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = lawyer.UserId,
                    Message = $"Appointment on {apt.Date} at {apt.Time} was booked by {client?.FullName ?? "a client"}.",
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                });
                await _db.SaveChangesAsync();
            }
        }

        // Notify client when their appointment is marked completed
        if (dto.Status == "completed" && apt.ClientId.HasValue)
        {
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == apt.ClientId);
            if (client != null)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = client.UserId,
                    Message = $"Your appointment on {apt.Date} has been completed.",
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                });
                await _db.SaveChangesAsync();
            }
        }

        return Ok(new { id = apt.Id, lawyerId = apt.LawyerId, clientId = apt.ClientId, caseId = apt.CaseId, date = apt.Date, time = apt.Time, duration = apt.Duration, status = apt.Status, notes = apt.Notes });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var apt = await _db.Appointments.FindAsync(id);
        if (apt == null) return NotFound();

        var profileId = int.Parse(User.FindFirst("profileId")?.Value ?? "0");
        var role = User.FindFirst("role")?.Value;

        if (role != "lawyer" || apt.LawyerId != profileId) return Forbid();

        _db.Appointments.Remove(apt);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}

public record CreateAppointmentDto(int LawyerId, int? ClientId, int? CaseId, string Date, string Time, int Duration, string? Status, string? Notes);
public record UpdateAppointmentDto(int? ClientId, int? CaseId, string? Status, string? Notes, string? Date, string? Time, int? Duration, bool? ClearClient);
