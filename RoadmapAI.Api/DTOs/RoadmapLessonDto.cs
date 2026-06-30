namespace RoadmapAI.Api.DTOs;

public class RoadmapLessonDto
{
    public Guid Id { get; set; }
    public int OrderIndex { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? GeneratedAt { get; set; }
}
