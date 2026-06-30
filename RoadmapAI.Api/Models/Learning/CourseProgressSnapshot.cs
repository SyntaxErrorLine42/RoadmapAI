namespace RoadmapAI.Api.Models.Learning;

public class CourseProgressSnapshot
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal CompletionPercent { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Course Course { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
