using RoadmapAI.Api.Models;

namespace RoadmapAI.Api.Models.Learning;

public class CourseReview
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string ContentMarkdown { get; set; } = string.Empty;

    public Course Course { get; set; } = null!;
}