using AttendanceLesson.DataBase.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceLesson.DataBase;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Logbook> Logbooks => Set<Logbook>();
    public DbSet<Schedule> Schedules => Set<Schedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(50);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Group>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(50);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Lesson>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);
            e.Property(l=> l.TeacherId);
            e.HasOne(r=>r.Teacher).WithMany().HasForeignKey(l=>l.TeacherId);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            e.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            e.Property(x => x.RefreshToken)
                .HasMaxLength(500);
            e.Property(x => x.RefreshTokenExpires); 

            e.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            e.HasOne(x => x.Group)
                .WithMany()
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            e.HasIndex(x => x.RoleId);
            e.HasIndex(x => x.GroupId);
        });

        modelBuilder.Entity<Logbook>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.Property(x => x.IsPresent)
                .IsRequired();

            e.Property(x => x.AbsenceDuration);

            e.Property(x => x.AbsenceReason)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired(false);

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            e.HasOne(x => x.Group)
                .WithMany()
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            e.HasOne(x => x.Lesson)
                .WithMany()
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            e.HasIndex(x => new { x.UserId, x.LessonId, x.CreatedAt }).IsUnique();
            e.HasIndex(x => x.GroupId);
            e.HasIndex(x => x.LessonId);
            e.HasIndex(x => x.CreatedAt);

            e.ToTable(t => t.HasCheckConstraint(
                "CK_Logbook_AbsenceReason",
                "\"IsPresent\" = TRUE OR \"AbsenceReason\" IS NOT NULL"));
        });

        modelBuilder.Entity<Schedule>(e =>
        {
            e.HasKey(s=> s.Id);
            e.Property(s=> s.LessonId);
            e.Property(s=> s.GroupId);
            e.Property(s=> s.StartTime);
            e.Property(s=> s.EndTime);

            e.HasOne(s=> s.Group).WithMany().HasForeignKey(s=> s.GroupId);
            e.HasOne(s=> s.Lesson).WithMany().HasForeignKey(s=> s.LessonId);


            e.ToTable(t=> t.HasCheckConstraint
            (
                "ck_time",
                "\"StartTime\" < \"EndTime\""    
            ));
        }); 
    }
}