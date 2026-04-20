using LegalCaseAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LegalCaseAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<Lawyer> Lawyers => Set<Lawyer>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<LawyerRequest> LawyerRequests => Set<LawyerRequest>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>().HasKey(u => u.Id);

        modelBuilder.Entity<Lawyer>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Client>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LawyerRequest>()
            .HasOne(r => r.Lawyer)
            .WithMany(l => l.Requests)
            .HasForeignKey(r => r.LawyerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LawyerRequest>()
            .HasOne(r => r.Client)
            .WithMany(c => c.Requests)
            .HasForeignKey(r => r.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Case>()
            .HasOne(c => c.Lawyer)
            .WithMany(l => l.Cases)
            .HasForeignKey(c => c.LawyerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Case>()
            .HasOne(c => c.Client)
            .WithMany(cl => cl.Cases)
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Case>()
            .HasOne(c => c.Request)
            .WithMany()
            .HasForeignKey(c => c.RequestId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Case)
            .WithMany(c => c.Appointments)
            .HasForeignKey(a => a.CaseId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Case)
            .WithMany(c => c.Documents)
            .HasForeignKey(d => d.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
