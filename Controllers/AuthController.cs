using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LegalCaseAPI.Data;
using LegalCaseAPI.DTOs;
using LegalCaseAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LegalCaseAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (await _db.ApplicationUsers.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Email already registered" });

        var role = dto.Role.ToLower() == "lawyer" ? "lawyer" : "client";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            FullName = dto.FullName,
            Email = dto.Email,
            Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };
        _db.ApplicationUsers.Add(user);

        int profileId;
        if (role == "lawyer")
        {
            var lawyer = new Lawyer
            {
                UserId = user.Id,
                FullName = dto.FullName,
                Specialization = "General",
                Bio = "",
                Avatar = GetInitials(dto.FullName),
                Rating = 0,
                CasesWon = 0,
                MaxClients = 5
            };
            _db.Lawyers.Add(lawyer);
            await _db.SaveChangesAsync();
            profileId = lawyer.Id;
        }
        else
        {
            var client = new Client
            {
                UserId = user.Id,
                FullName = dto.FullName,
                Avatar = GetInitials(dto.FullName)
            };
            _db.Clients.Add(client);
            await _db.SaveChangesAsync();
            profileId = client.Id;
        }

        var token = GenerateToken(user.Id, role, profileId, user.Email, user.FullName);
        return Ok(new AuthResponse(token, user.Id, profileId, role, user.FullName, user.Email));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _db.ApplicationUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password" });

        int profileId = 0;
        if (user.Role == "lawyer")
        {
            var lawyer = await _db.Lawyers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            profileId = lawyer?.Id ?? 0;
        }
        else
        {
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == user.Id);
            profileId = client?.Id ?? 0;
        }

        var token = GenerateToken(user.Id, user.Role, profileId, user.Email, user.FullName);
        return Ok(new AuthResponse(token, user.Id, profileId, user.Role, user.FullName, user.Email));
    }

    private string GenerateToken(string userId, string role, int profileId, string email, string fullName)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim("role", role),
            new Claim("profileId", profileId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("fullName", fullName)
        };
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GetInitials(string name)
    {
        var parts = name.Trim().Split(' ');
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        return name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
    }
}
