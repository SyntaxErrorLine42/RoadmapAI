namespace RoadmapAI.Api.Models.Learning;

public class CourseRoadmap
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string SummaryMarkdown { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public Course Course { get; set; } = null!;
    public ICollection<RoadmapModule> Modules { get; set; } = [];
}
