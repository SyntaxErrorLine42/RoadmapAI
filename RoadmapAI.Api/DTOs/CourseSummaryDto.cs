namespace RoadmapAI.Api.DTOs;

public class CourseSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MaterialCount { get; set; }
    public int CompletionPercent { get; set; }
}
