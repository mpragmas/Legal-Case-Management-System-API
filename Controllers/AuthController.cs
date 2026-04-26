using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LegalCaseAPI.Data;
using LegalCaseAPI.DTOs;
using LegalCaseAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using LegalCaseAPI.Services;

namespace LegalCaseAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;

    public AuthController(AppDbContext db, IConfiguration config, IEmailService emailService)
    {
        _db = db;
        _config = config;
        _emailService = emailService;
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

        if (user.TwoFactorEnabled)
        {
            var code = new Random().Next(100000, 999999).ToString();
            user.TwoFactorCode = code;
            user.TwoFactorExpiry = DateTime.UtcNow.AddMinutes(10);
            await _db.SaveChangesAsync();

            await _emailService.SendTwoFactorCodeAsync(user.Email, code);

            return Ok(new { twoFactorRequired = true, email = user.Email });
        }

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
        return Ok(new AuthResponse(token, user.Id, profileId, user.Role, user.FullName, user.Email, user.TwoFactorEnabled));
    }

    [HttpPost("login-2fa")]
    public async Task<IActionResult> Login2FA(TwoFactorLoginDto dto)
    {
        var user = await _db.ApplicationUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

        if (user == null || user.TwoFactorCode == null || user.TwoFactorExpiry < DateTime.UtcNow)
            return Unauthorized(new { message = "Invalid or expired 2FA code" });

        if (user.TwoFactorCode != dto.Code)
            return Unauthorized(new { message = "Invalid 2FA code" });

        // Clear code after successful verification
        user.TwoFactorCode = null;
        user.TwoFactorExpiry = null;
        await _db.SaveChangesAsync();

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
        return Ok(new AuthResponse(token, user.Id, profileId, user.Role, user.FullName, user.Email, user.TwoFactorEnabled));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) return Ok(new { message = "If the email exists, a reset link has been sent." });

        user.ResetToken = Guid.NewGuid().ToString("N");
        user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();

        await _emailService.SendPasswordResetAsync(user.Email, user.ResetToken);

        return Ok(new { message = "If the email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.ResetToken == dto.Token && u.ResetTokenExpiry > DateTime.UtcNow);
        if (user == null) return BadRequest(new { message = "Invalid or expired token" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password reset successfully" });
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userId = User.FindFirst("sub")?.Value;
        var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized();

        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully" });
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("2fa/enable")]
    public async Task<IActionResult> Enable2FA()
    {
        var userId = User.FindFirst("sub")?.Value;
        var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized();

        var code = new Random().Next(100000, 999999).ToString();
        user.TwoFactorCode = code;
        user.TwoFactorExpiry = DateTime.UtcNow.AddMinutes(10);
        await _db.SaveChangesAsync();

        await _emailService.SendTwoFactorCodeAsync(user.Email, code);

        return Ok(new { message = "Verification code sent to your email" });
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> Disable2FA()
    {
        var userId = User.FindFirst("sub")?.Value;
        var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized();
        user.TwoFactorEnabled = false;
        user.TwoFactorCode = null;
        user.TwoFactorExpiry = null;
        await _db.SaveChangesAsync();
        return Ok(new { message = "2FA disabled successfully" });
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("2fa/verify")]
    public async Task<IActionResult> Verify2FA(TwoFactorVerifyDto dto)
    {
        var userId = User.FindFirst("sub")?.Value;
        var user = await _db.ApplicationUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized();

        if (user.TwoFactorCode == null || user.TwoFactorExpiry < DateTime.UtcNow)
            return BadRequest(new { message = "Code expired or not requested" });

        if (user.TwoFactorCode == dto.Code)
        {
            user.TwoFactorEnabled = true;
            user.TwoFactorCode = null;
            user.TwoFactorExpiry = null;
            await _db.SaveChangesAsync();
            return Ok(new { message = "2FA enabled successfully" });
        }

        return BadRequest(new { message = "Invalid code" });
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
