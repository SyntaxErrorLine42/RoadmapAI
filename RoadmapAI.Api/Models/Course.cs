using RoadmapAI.Api.Models.Enums;
using RoadmapAI.Api.Models.Learning;

namespace RoadmapAI.Api.Models;

public class Course
{
    public Guid Id { get; set; }

    // Owner of this course
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Summary { get; set; }

    public CourseStatus Status { get; set; } = CourseStatus.Processing;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CourseMaterial> Materials { get; set; } = [];
    public CourseRoadmap? Roadmap { get; set; }
    public ICollection<CourseReview> Reviews { get; set; } = [];
    public ICollection<CourseProgressSnapshot> ProgressSnapshots { get; set; } = [];
    public ICollection<GenerationJob> GenerationJobs { get; set; } = [];
}
