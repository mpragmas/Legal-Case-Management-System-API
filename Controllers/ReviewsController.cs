using System.ComponentModel.DataAnnotations;
using LegalCaseAPI.Data;
using LegalCaseAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LegalCaseAPI.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReviewsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("lawyer/{lawyerId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByLawyer(int lawyerId)
    {
        var reviews = await _db.Reviews
            .Include(r => r.Client)
            .Where(r => r.LawyerId == lawyerId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                clientName = r.Client.FullName
            })
            .ToListAsync();
        return Ok(reviews);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        var profileId = int.Parse(User.FindFirst("profileId")?.Value ?? "0");
        var role = User.FindFirst("role")?.Value;

        if (role != "client") return Forbid();
        if (profileId == 0) return BadRequest(new { message = "Invalid client profile" });

        var lawyerExists = await _db.Lawyers.AnyAsync(l => l.Id == dto.LawyerId);
        if (!lawyerExists) return NotFound(new { message = "Lawyer not found" });

        var review = new Review
        {
            LawyerId = dto.LawyerId,
            ClientId = profileId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        var avg = await _db.Reviews
            .Where(r => r.LawyerId == dto.LawyerId)
            .AverageAsync(r => r.Rating);
        var lawyer = await _db.Lawyers.FindAsync(dto.LawyerId);
        if (lawyer != null)
        {
            lawyer.Rating = Math.Round(avg, 1);
            await _db.SaveChangesAsync();
        }

        return Ok(new
        {
            id = review.Id,
            lawyerId = review.LawyerId,
            clientId = review.ClientId,
            rating = review.Rating,
            comment = review.Comment,
            createdAt = review.CreatedAt
        });
    }
}

public record CreateReviewDto(int LawyerId, [Range(1, 5)] int Rating, string Comment);
