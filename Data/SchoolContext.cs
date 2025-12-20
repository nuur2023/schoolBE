using Microsoft.EntityFrameworkCore;
using schoolBE.Models;

namespace schoolBE.Data;

public class SchoolContext : DbContext
{
    public SchoolContext(DbContextOptions<SchoolContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<ChartOfAccount> ChartOfAccounts { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<ClassSubject> ClassSubjects { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamParticipants> ExamParticipants { get; set; }
    public DbSet<ExamResult> ExamResults { get; set; }
    public DbSet<ExamRoom> ExamRooms { get; set; }
    public DbSet<ExamSchedule> ExamSchedules { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<FiscalYear> FiscalYears { get; set; }
    public DbSet<JournalDetail> JournalDetails { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<Level> Levels { get; set; }
    public DbSet<LevelSubject> LevelSubjects { get; set; }
    public DbSet<Menu> Menus { get; set; }
    public DbSet<PayRoll> PayRolls { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Person> Persons { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<School> Schools { get; set; }
    public DbSet<SchoolAccount> SchoolAccounts { get; set; }
    public DbSet<SchoolUser> SchoolUsers { get; set; }
    public DbSet<Stage> Stages { get; set; }
    public DbSet<StudentEnrollment> StudentEnrollments { get; set; }
    public DbSet<StudentExit> StudentExits { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Submenu> Submenus { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<YearEndClosingEntry> YearEndClosingEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships based on migration snapshot
        modelBuilder.Entity<Class>()
            .HasOne(c => c.Level)
            .WithMany()
            .HasForeignKey(c => c.LevelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClassSubject>()
            .HasOne(cs => cs.Class)
            .WithMany()
            .HasForeignKey(cs => cs.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClassSubject>()
            .HasOne(cs => cs.Subject)
            .WithMany()
            .HasForeignKey(cs => cs.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClassSubject>()
            .HasOne(cs => cs.Person)
            .WithMany()
            .HasForeignKey(cs => cs.PersonId);

        modelBuilder.Entity<ExamParticipants>()
            .HasOne(ep => ep.ExamSchedule)
            .WithMany()
            .HasForeignKey(ep => ep.ExamScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExamParticipants>()
            .HasOne(ep => ep.Student)
            .WithMany()
            .HasForeignKey(ep => ep.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExamParticipants>()
            .HasOne(ep => ep.Class)
            .WithMany()
            .HasForeignKey(ep => ep.ClassId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ExamParticipants>()
            .HasOne(ep => ep.Level)
            .WithMany()
            .HasForeignKey(ep => ep.LevelId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ExamParticipants>()
            .HasOne(ep => ep.Room)
            .WithMany()
            .HasForeignKey(ep => ep.RoomId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ExamParticipants>()
            .HasOne(ep => ep.Creator)
            .WithMany()
            .HasForeignKey(ep => ep.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExamResult>()
            .HasOne(er => er.ExamSchedule)
            .WithMany()
            .HasForeignKey(er => er.ExamScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExamResult>()
            .HasOne(er => er.StudentEnrollment)
            .WithMany()
            .HasForeignKey(er => er.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExamRoom>()
            .HasOne(er => er.School)
            .WithMany()
            .HasForeignKey(er => er.SchoolId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ExamSchedule>()
            .HasOne(es => es.Exam)
            .WithMany()
            .HasForeignKey(es => es.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExamSchedule>()
            .HasOne(es => es.LevelSubject)
            .WithMany()
            .HasForeignKey(es => es.LevelSubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Level>()
            .HasOne(l => l.Stage)
            .WithMany()
            .HasForeignKey(l => l.StageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LevelSubject>()
            .HasOne(ls => ls.Level)
            .WithMany()
            .HasForeignKey(ls => ls.LevelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LevelSubject>()
            .HasOne(ls => ls.Subject)
            .WithMany()
            .HasForeignKey(ls => ls.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Person>()
            .HasOne(p => p.Role)
            .WithMany()
            .HasForeignKey(p => p.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Person>()
            .HasOne(p => p.School)
            .WithMany()
            .HasForeignKey(p => p.SchoolId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Person>()
            .HasIndex(p => p.Email)
            .IsUnique();

        modelBuilder.Entity<SchoolUser>()
            .HasOne(su => su.School)
            .WithMany()
            .HasForeignKey(su => su.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SchoolUser>()
            .HasOne(su => su.User)
            .WithMany()
            .HasForeignKey(su => su.UsersId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentEnrollment>()
            .HasOne(se => se.Stage)
            .WithMany()
            .HasForeignKey(se => se.StageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentEnrollment>()
            .HasOne(se => se.Level)
            .WithMany()
            .HasForeignKey(se => se.LevelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentEnrollment>()
            .HasOne(se => se.Class)
            .WithMany()
            .HasForeignKey(se => se.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentExit>()
            .HasOne(se => se.Student)
            .WithMany()
            .HasForeignKey(se => se.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentExit>()
            .HasOne(se => se.School)
            .WithMany()
            .HasForeignKey(se => se.SchoolId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StudentExit>()
            .HasOne(se => se.Creator)
            .WithMany()
            .HasForeignKey(se => se.CreatorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Submenu>()
            .HasOne(sm => sm.Menu)
            .WithMany(m => m.Submenus)
            .HasForeignKey(sm => sm.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Configure decimal precision for Exam
        modelBuilder.Entity<Exam>()
            .Property(e => e.Weight)
            .HasPrecision(10, 1);

        // Configure decimal precision for ExamResult
        modelBuilder.Entity<ExamResult>()
            .Property(er => er.Marks)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ExamResult>()
            .Property(er => er.FullMarks)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ExamResult>()
            .Property(er => er.Percentage)
            .HasPrecision(10, 2);
    }
}

