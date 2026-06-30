namespace RoadmapAI.Api.Models.Learning;

public class LessonCompletion
{
    public Guid Id { get; set; }
    public Guid RoadmapLessonId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public RoadmapLesson RoadmapLesson { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
