namespace RoadmapAI.Api.DTOs;

public class CourseReviewDetailDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ContentMarkdown { get; set; } = string.Empty;
}