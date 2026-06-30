using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RoadmapAI.Api.Models.Enums;
using RoadmapAI.Api.Models.Learning;
using RoadmapAI.Api.Models;

namespace RoadmapAI.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseMaterial> CourseMaterials => Set<CourseMaterial>();
    public DbSet<CourseRoadmap> CourseRoadmaps => Set<CourseRoadmap>();
    public DbSet<RoadmapModule> RoadmapModules => Set<RoadmapModule>();
    public DbSet<RoadmapLesson> RoadmapLessons => Set<RoadmapLesson>();
    public DbSet<RoadmapExam> RoadmapExams => Set<RoadmapExam>();
    public DbSet<RoadmapExamQuestion> RoadmapExamQuestions => Set<RoadmapExamQuestion>();
    public DbSet<RoadmapExamAttempt> RoadmapExamAttempts => Set<RoadmapExamAttempt>();
    public DbSet<RoadmapExamAttemptAnswer> RoadmapExamAttemptAnswers => Set<RoadmapExamAttemptAnswer>();
    public DbSet<CourseReview> CourseReviews => Set<CourseReview>();
    public DbSet<LessonCompletion> LessonCompletions => Set<LessonCompletion>();
    public DbSet<ModuleCompletion> ModuleCompletions => Set<ModuleCompletion>();
    public DbSet<CourseProgressSnapshot> CourseProgressSnapshots => Set<CourseProgressSnapshot>();
    public DbSet<GenerationJob> GenerationJobs => Set<GenerationJob>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Course belongs to a user
        builder.Entity<Course>(e =>
        {
            e.HasOne(c => c.User)
             .WithMany()
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(c => c.UserId);

            e.Property(c => c.Status)
             .HasConversion<string>();

              e.HasOne(c => c.Roadmap)
               .WithOne(r => r.Course)
               .HasForeignKey<CourseRoadmap>(r => r.CourseId)
               .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(c => c.Reviews)
             .WithOne(r => r.Course)
             .HasForeignKey(r => r.CourseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CourseReview>(e =>
        {
            e.Property(r => r.ContentMarkdown)
             .IsRequired();

            e.HasIndex(r => new { r.CourseId, r.CreatedAt });
        });

        // Material belongs to a course, cascade delete when course is removed
        builder.Entity<CourseMaterial>(e =>
        {
            e.HasOne(m => m.Course)
             .WithMany(c => c.Materials)
             .HasForeignKey(m => m.CourseId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(m => m.Type)
             .HasConversion<string>();

            e.Property(m => m.Status)
             .HasConversion<string>();

            e.HasIndex(m => new { m.CourseId, m.Status });
        });

        builder.Entity<CourseRoadmap>(e =>
        {
            e.HasIndex(r => r.CourseId)
             .IsUnique();
        });

        builder.Entity<RoadmapModule>(e =>
        {
            e.HasOne(m => m.CourseRoadmap)
             .WithMany(r => r.Modules)
             .HasForeignKey(m => m.CourseRoadmapId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(m => new { m.CourseRoadmapId, m.OrderIndex })
             .IsUnique();
        });

        builder.Entity<RoadmapLesson>(e =>
        {
            e.HasOne(l => l.RoadmapModule)
             .WithMany(m => m.Lessons)
             .HasForeignKey(l => l.RoadmapModuleId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(l => l.Status)
             .HasConversion<string>();

            e.HasIndex(l => new { l.RoadmapModuleId, l.OrderIndex })
             .IsUnique();

            e.HasIndex(l => new { l.RoadmapModuleId, l.Status });
        });

        builder.Entity<RoadmapExam>(e =>
        {
            e.HasOne(ex => ex.RoadmapModule)
             .WithMany(m => m.Exams)
             .HasForeignKey(ex => ex.RoadmapModuleId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(ex => ex.Status)
             .HasConversion<string>();
        });

        builder.Entity<RoadmapExamQuestion>(e =>
        {
            e.HasOne(q => q.RoadmapExam)
             .WithMany(ex => ex.Questions)
             .HasForeignKey(q => q.RoadmapExamId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(q => new { q.RoadmapExamId, q.OrderIndex })
             .IsUnique();

            e.Property(q => q.CorrectOption)
             .HasMaxLength(1);
        });

        builder.Entity<RoadmapExamAttempt>(e =>
        {
            e.HasOne(a => a.RoadmapExam)
             .WithMany(ex => ex.Attempts)
             .HasForeignKey(a => a.RoadmapExamId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.User)
             .WithMany()
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(a => new { a.RoadmapExamId, a.UserId, a.SubmittedAt });
        });

        builder.Entity<RoadmapExamAttemptAnswer>(e =>
        {
            e.Property(a => a.QuestionText)
             .IsRequired();

            e.Property(a => a.CorrectOption)
             .IsRequired()
             .HasMaxLength(1);

            e.HasOne(a => a.RoadmapExamAttempt)
             .WithMany(at => at.Answers)
             .HasForeignKey(a => a.RoadmapExamAttemptId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.RoadmapExamQuestion)
             .WithMany()
             .HasForeignKey(a => a.RoadmapExamQuestionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LessonCompletion>(e =>
        {
            e.HasOne(c => c.RoadmapLesson)
             .WithMany(l => l.Completions)
             .HasForeignKey(c => c.RoadmapLessonId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.User)
             .WithMany()
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(c => new { c.RoadmapLessonId, c.UserId })
             .IsUnique();
        });

        builder.Entity<ModuleCompletion>(e =>
        {
            e.HasOne(c => c.RoadmapModule)
             .WithMany()
             .HasForeignKey(c => c.RoadmapModuleId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.User)
             .WithMany()
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(c => new { c.RoadmapModuleId, c.UserId })
             .IsUnique();
        });

        builder.Entity<CourseProgressSnapshot>(e =>
        {
            e.HasOne(p => p.Course)
             .WithMany(c => c.ProgressSnapshots)
             .HasForeignKey(p => p.CourseId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.User)
             .WithMany()
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(p => new { p.CourseId, p.UserId })
             .IsUnique();
        });

        builder.Entity<GenerationJob>(e =>
        {
            e.HasOne(j => j.Course)
             .WithMany(c => c.GenerationJobs)
             .HasForeignKey(j => j.CourseId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(j => j.EntityType)
             .HasConversion<string>();

            e.Property(j => j.Status)
             .HasConversion<string>();

            e.HasIndex(j => new { j.CourseId, j.Status, j.CreatedAt });
        });
    }
}
