namespace RoadmapAI.Api.DTOs;

public class CourseReviewSummaryDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Preview { get; set; } = string.Empty;
}