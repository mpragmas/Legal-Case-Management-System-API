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
        var apts = await _db.Appointments.Select(a => new
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

        if (dto.ClientId.HasValue) apt.ClientId = dto.ClientId;
        if (dto.CaseId.HasValue) apt.CaseId = dto.CaseId;
        if (dto.Status != null) apt.Status = dto.Status;
        if (dto.Notes != null) apt.Notes = dto.Notes;
        if (dto.Date != null) apt.Date = dto.Date;
        if (dto.Time != null) apt.Time = dto.Time;
        if (dto.Duration.HasValue) apt.Duration = dto.Duration.Value;
        if (dto.ClearClient == true) { apt.ClientId = null; apt.CaseId = null; }

        await _db.SaveChangesAsync();
        return Ok(new { id = apt.Id, lawyerId = apt.LawyerId, clientId = apt.ClientId, caseId = apt.CaseId, date = apt.Date, time = apt.Time, duration = apt.Duration, status = apt.Status, notes = apt.Notes });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var apt = await _db.Appointments.FindAsync(id);
        if (apt == null) return NotFound();
        _db.Appointments.Remove(apt);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}

public record CreateAppointmentDto(int LawyerId, int? ClientId, int? CaseId, string Date, string Time, int Duration, string? Status, string? Notes);
public record UpdateAppointmentDto(int? ClientId, int? CaseId, string? Status, string? Notes, string? Date, string? Time, int? Duration, bool? ClearClient);
