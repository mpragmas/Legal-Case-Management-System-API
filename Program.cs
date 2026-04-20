using System.Text;
using LegalCaseAPI.Data;
using LegalCaseAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Auto-migrate and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
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
    var u1 = new ApplicationUser { Id = "u-l1", FullName = "Sarah Mitchell", Email = "sarah.mitchell@legaldesk.com", Role = "lawyer", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
    var u2 = new ApplicationUser { Id = "u-l2", FullName = "David Hernandez", Email = "david.hernandez@legaldesk.com", Role = "lawyer", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
    var u3 = new ApplicationUser { Id = "u-l3", FullName = "Olivia Chen", Email = "olivia.chen@legaldesk.com", Role = "lawyer", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
    var u4 = new ApplicationUser { Id = "u-l4", FullName = "James Kowalski", Email = "james.kowalski@legaldesk.com", Role = "lawyer", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
    var u5 = new ApplicationUser { Id = "u-c1", FullName = "Michael Torres", Email = "michael.torres@email.com", Role = "client", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };
    var u6 = new ApplicationUser { Id = "u-c2", FullName = "Emily Watson", Email = "emily.watson@email.com", Role = "client", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };

    db.ApplicationUsers.AddRange(uLD, uCD, u1, u2, u3, u4, u5, u6);
    db.SaveChanges();

    // Demo lawyer gets id=1, demo client gets id=1
    var lDemo = new Lawyer { UserId = "u-lawyer-demo", FullName = "Sarah Mitchell", YearsOfExperience = 12, Specialization = "Corporate Law", Bio = "Sarah is a seasoned corporate attorney with over a decade of experience advising Fortune 500 companies on mergers, acquisitions, and regulatory compliance.", Avatar = "SM", Rating = 4.9, CasesWon = 142, MaxClients = 2 };
    db.Lawyers.Add(lDemo);
    db.SaveChanges();

    var cDemo = new Client { UserId = "u-client-demo", FullName = "Michael Torres", Phone = "+1 555-0101", Address = "742 Maple Avenue, Suite 200, New York, NY 10001", Avatar = "MT" };
    db.Clients.Add(cDemo);
    db.SaveChanges();

    // Other lawyers (for display in lawyers list)
    var l2 = new Lawyer { UserId = "u-l2", FullName = "David Hernandez", YearsOfExperience = 8, Specialization = "Criminal Defense", Bio = "David is a passionate criminal defense lawyer who believes in justice for all. He specializes in federal cases and white-collar crime defense.", Avatar = "DH", Rating = 4.7, CasesWon = 98, MaxClients = 2 };
    var l3 = new Lawyer { UserId = "u-l3", FullName = "Olivia Chen", YearsOfExperience = 15, Specialization = "Family Law", Bio = "Olivia is one of the most respected family law attorneys in the state. She handles divorce, custody, and adoption cases with empathy and precision.", Avatar = "OC", Rating = 4.8, CasesWon = 210, MaxClients = 2 };
    var l4 = new Lawyer { UserId = "u-l4", FullName = "James Kowalski", YearsOfExperience = 10, Specialization = "Civil Litigation", Bio = "James brings a strategic, analytical approach to civil litigation. He excels in contract disputes, personal injury, and property law.", Avatar = "JK", Rating = 4.6, CasesWon = 115, MaxClients = 2 };
    // Named sarah account also gets a lawyer profile
    var l1Named = new Lawyer { UserId = "u-l1", FullName = "Sarah Mitchell", YearsOfExperience = 12, Specialization = "Corporate Law", Bio = "Sarah is a seasoned corporate attorney with over a decade of experience.", Avatar = "SM", Rating = 4.9, CasesWon = 142, MaxClients = 2 };

    db.Lawyers.AddRange(l2, l3, l4, l1Named);
    db.SaveChanges();

    // Other clients
    var c2 = new Client { UserId = "u-c2", FullName = "Emily Watson", Phone = "+1 555-0202", Address = "1580 Oak Lane, Apt 4B, Los Angeles, CA 90015", Avatar = "EW" };
    var c1Named = new Client { UserId = "u-c1", FullName = "Michael Torres", Phone = "+1 555-0101", Address = "742 Maple Avenue, Suite 200, New York, NY 10001", Avatar = "MT" };
    db.Clients.AddRange(c2, c1Named);
    db.SaveChanges();

    // Requests — use demo accounts' IDs (lDemo.Id=1, cDemo.Id=1)
    var r1 = new LawyerRequest { LawyerId = lDemo.Id, ClientId = cDemo.Id, Status = "approved", Message = "I need legal counsel for a corporate merger involving two subsidiaries.", RequestedAt = new DateTime(2026, 4, 1, 10, 0, 0) };
    var r2 = new LawyerRequest { LawyerId = l3.Id, ClientId = c2.Id, Status = "approved", Message = "Seeking representation for a custody arrangement modification.", RequestedAt = new DateTime(2026, 4, 3, 14, 30, 0) };
    var r3 = new LawyerRequest { LawyerId = l2.Id, ClientId = cDemo.Id, Status = "pending", Message = "I would like to discuss a federal investigation related to my business.", RequestedAt = new DateTime(2026, 4, 10, 9, 15, 0) };
    var r4 = new LawyerRequest { LawyerId = l4.Id, ClientId = c2.Id, Status = "pending", Message = "I have a contract dispute with a former business partner.", RequestedAt = new DateTime(2026, 4, 12, 16, 45, 0) };

    db.LawyerRequests.AddRange(r1, r2, r3, r4);
    db.SaveChanges();

    // Cases — use demo accounts
    var case1 = new Case { LawyerId = lDemo.Id, ClientId = cDemo.Id, RequestId = r1.Id, Title = "TechCorp Merger Advisory", Description = "Providing legal counsel and documentation for the merger of TechCorp's North American and European divisions.", Status = "active", CreatedAt = new DateTime(2026, 4, 2, 8, 0, 0) };
    var case2 = new Case { LawyerId = l3.Id, ClientId = c2.Id, RequestId = r2.Id, Title = "Watson Custody Modification", Description = "Representing the client in modifying the existing custody arrangement.", Status = "active", CreatedAt = new DateTime(2026, 4, 4, 10, 0, 0) };

    db.Cases.AddRange(case1, case2);
    db.SaveChanges();

    var today = DateTime.Today.ToString("yyyy-MM-dd");
    var tomorrow = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
    var dayAfter = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd");

    db.Appointments.AddRange(
        new Appointment { LawyerId = lDemo.Id, ClientId = cDemo.Id, CaseId = case1.Id, Date = today, Time = "23:00", Duration = 60, Status = "confirmed", Notes = "Reviewed merger timeline and key milestones. Client approved the preliminary due-diligence checklist." },
        new Appointment { LawyerId = lDemo.Id, ClientId = null, CaseId = null, Date = tomorrow, Time = "14:00", Duration = 60, Status = "available", Notes = "" },
        new Appointment { LawyerId = l3.Id, ClientId = c2.Id, CaseId = case2.Id, Date = tomorrow, Time = "11:00", Duration = 45, Status = "confirmed", Notes = "" },
        new Appointment { LawyerId = lDemo.Id, ClientId = null, CaseId = null, Date = dayAfter, Time = "09:00", Duration = 60, Status = "available", Notes = "" },
        new Appointment { LawyerId = l3.Id, ClientId = null, CaseId = null, Date = dayAfter, Time = "15:00", Duration = 45, Status = "available", Notes = "" },
        new Appointment { LawyerId = l2.Id, ClientId = null, CaseId = null, Date = tomorrow, Time = "10:00", Duration = 60, Status = "available", Notes = "" },
        new Appointment { LawyerId = lDemo.Id, ClientId = cDemo.Id, CaseId = case1.Id, Date = "2026-04-10", Time = "10:00", Duration = 60, Status = "completed", Notes = "Initial consultation completed. Discussed scope of the merger and legal requirements." }
    );
    db.SaveChanges();

    db.Documents.AddRange(
        new Document { CaseId = case1.Id, FileName = "Merger_Agreement_Draft_v1.pdf", FilePath = "", Size = "2.4 MB", UploadedBy = "1", UploadedAt = new DateTime(2026, 4, 5, 12, 0, 0) },
        new Document { CaseId = case1.Id, FileName = "Due_Diligence_Checklist.xlsx", FilePath = "", Size = "540 KB", UploadedBy = "1", UploadedAt = new DateTime(2026, 4, 6, 9, 30, 0) },
        new Document { CaseId = case2.Id, FileName = "Custody_Modification_Petition.pdf", FilePath = "", Size = "1.1 MB", UploadedBy = "3", UploadedAt = new DateTime(2026, 4, 7, 14, 0, 0) },
        new Document { CaseId = case1.Id, FileName = "Financial_Statements_Q1.pdf", FilePath = "", Size = "3.8 MB", UploadedBy = "1", UploadedAt = new DateTime(2026, 4, 8, 11, 0, 0) }
    );
    db.SaveChanges();
}
