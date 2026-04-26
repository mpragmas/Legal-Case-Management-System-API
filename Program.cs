using System.Text;
using LegalCaseAPI.Data;
using LegalCaseAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using LegalCaseAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Services
builder.Services.AddScoped<IEmailService, EmailService>();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // Keep JWT claim names as-is (sub, role, email)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Auto-migrate/create and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Add Status column to Cases if missing (schema patch for existing databases)
    try
    {
        db.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (
                SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'Cases') AND name = N'Status'
            )
            BEGIN
                ALTER TABLE Cases ADD Status nvarchar(50) NOT NULL DEFAULT 'active'
            END

            -- ApplicationUsers 2FA patch
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'ApplicationUsers') AND name = N'TwoFactorCode')
            BEGIN
                ALTER TABLE ApplicationUsers ADD TwoFactorCode nvarchar(max) NULL
            END
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'ApplicationUsers') AND name = N'TwoFactorExpiry')
            BEGIN
                ALTER TABLE ApplicationUsers ADD TwoFactorExpiry datetime2 NULL
            END
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Schema patch] Could not add Status column: {ex.Message}");
    }

    SeedData(db);
}

app.Run();

static void SeedData(AppDbContext db)
{
    if (db.ApplicationUsers.Any()) return;

    // Demo accounts come FIRST so they get the lowest IDs (profileId 1, 1)
    var uLD = new ApplicationUser { Id = "u-lawyer-demo", FullName = "Sarah Mitchell", Email = "lawyer@example.com", Role = "lawyer", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
    var uCD = new ApplicationUser { Id = "u-client-demo", FullName = "Michael Torres", Email = "client@example.com", Role = "client", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
    // Named accounts
    var u2 = new ApplicationUser { Id = "u-l2", FullName = "David Hernandez", Email = "david.hernandez@legaldesk.com", Role = "lawyer", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
    var u3 = new ApplicationUser { Id = "u-l3", FullName = "Olivia Chen", Email = "olivia.chen@legaldesk.com", Role = "lawyer", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
    var u4 = new ApplicationUser { Id = "u-l4", FullName = "James Kowalski", Email = "james.kowalski@legaldesk.com", Role = "lawyer", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
    var u6 = new ApplicationUser { Id = "u-c2", FullName = "Emily Watson", Email = "emily.watson@email.com", Role = "client", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };

    // Remove orphaned u1 (u-l1) and u5 (u-c1) — they were duplicates with no profiles
    db.ApplicationUsers.AddRange(uLD, uCD, u2, u3, u4, u6);
    db.SaveChanges();

    // Demo lawyer gets id=1, demo client gets id=1
    var lDemo = new Lawyer { UserId = "u-lawyer-demo", FullName = "Sarah Mitchell", YearsOfExperience = 12, Specialization = "Corporate Law", Bio = "Sarah is a seasoned corporate attorney with over a decade of experience advising Fortune 500 companies on mergers, acquisitions, and regulatory compliance.", Avatar = "SM", Rating = 4.9, CasesWon = 142, MaxClients = 2 };
    db.Lawyers.Add(lDemo);
    db.SaveChanges();

    var cDemo = new Client { UserId = "u-client-demo", FullName = "Michael Torres", Phone = "+1 555-0101", Address = "742 Maple Avenue, Suite 200, New York, NY 10001", Avatar = "MT" };
    db.Clients.Add(cDemo);
    db.SaveChanges();

    // Other lawyers
    var l2 = new Lawyer { UserId = "u-l2", FullName = "David Hernandez", YearsOfExperience = 8, Specialization = "Criminal Defense", Bio = "David is a passionate criminal defense lawyer who specializes in federal cases and white-collar crime defense.", Avatar = "DH", Rating = 4.7, CasesWon = 98, MaxClients = 2 };
    var l3 = new Lawyer { UserId = "u-l3", FullName = "Olivia Chen", YearsOfExperience = 15, Specialization = "Family Law", Bio = "Olivia is respected for her precision in handle divorce and custody cases.", Avatar = "OC", Rating = 4.8, CasesWon = 210, MaxClients = 2 };
    var l4 = new Lawyer { UserId = "u-l4", FullName = "James Kowalski", YearsOfExperience = 10, Specialization = "Civil Litigation", Bio = "James excels in contract disputes and property law.", Avatar = "JK", Rating = 4.6, CasesWon = 115, MaxClients = 2 };

    db.Lawyers.AddRange(l2, l3, l4);
    db.SaveChanges();

    // Other clients
    var c2 = new Client { UserId = "u-c2", FullName = "Emily Watson", Phone = "+1 555-0202", Address = "1580 Oak Lane, Apt 4B, Los Angeles, CA 90015", Avatar = "EW" };
    db.Clients.Add(c2);
    db.SaveChanges();

    // Requests
    var r1 = new LawyerRequest { LawyerId = lDemo.Id, ClientId = cDemo.Id, Status = "approved", Message = "I need legal counsel for a corporate merger.", RequestedAt = DateTime.UtcNow.AddDays(-20) };
    var r2 = new LawyerRequest { LawyerId = l3.Id, ClientId = c2.Id, Status = "approved", Message = "Seeking representation for a custody arrangement.", RequestedAt = DateTime.UtcNow.AddDays(-15) };
    
    db.LawyerRequests.AddRange(r1, r2);
    db.SaveChanges();

    // Cases
    var case1 = new Case { LawyerId = lDemo.Id, ClientId = cDemo.Id, RequestId = r1.Id, Title = "TechCorp Merger Advisory", Description = "Legal counsel for the merger of TechCorp's divisions.", Status = "active", CreatedAt = DateTime.UtcNow.AddDays(-19) };
    var case2 = new Case { LawyerId = l3.Id, ClientId = c2.Id, RequestId = r2.Id, Title = "Watson Custody Modification", Description = "Modifying the existing custody arrangement.", Status = "active", CreatedAt = DateTime.UtcNow.AddDays(-14) };

    db.Cases.AddRange(case1, case2);
    db.SaveChanges();

    // Appointments
    var today = DateTime.Today.ToString("yyyy-MM-dd");
    db.Appointments.AddRange(
        new Appointment { LawyerId = lDemo.Id, ClientId = cDemo.Id, CaseId = case1.Id, Date = today, Time = "10:00", Duration = 60, Status = "confirmed", Notes = "Reviewing merger docs." },
        new Appointment { LawyerId = lDemo.Id, ClientId = null, CaseId = null, Date = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"), Time = "14:00", Duration = 60, Status = "available", Notes = "" }
    );
    db.SaveChanges();

    // Documents
    db.Documents.AddRange(
        new Document { CaseId = case1.Id, FileName = "Merger_Draft.pdf", FilePath = "/uploads/merger.pdf", Size = "2.4 MB", UploadedBy = "u-lawyer-demo", UploadedAt = DateTime.UtcNow.AddDays(-5) },
        new Document { CaseId = case2.Id, FileName = "Petition.pdf", FilePath = "/uploads/petition.pdf", Size = "1.1 MB", UploadedBy = "u-l3", UploadedAt = DateTime.UtcNow.AddDays(-3) }
    );
    db.SaveChanges();

    // Reviews
    db.Reviews.AddRange(
        new Review { LawyerId = lDemo.Id, ClientId = cDemo.Id, Rating = 5, Comment = "Excellent service! Sarah is extremely knowledgeable.", CreatedAt = DateTime.UtcNow.AddDays(-10) },
        new Review { LawyerId = l3.Id, ClientId = c2.Id, Rating = 4, Comment = "Very professional and helpful.", CreatedAt = DateTime.UtcNow.AddDays(-5) }
    );
    db.SaveChanges();
}
