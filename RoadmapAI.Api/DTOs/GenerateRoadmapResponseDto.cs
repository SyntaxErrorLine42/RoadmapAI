namespace RoadmapAI.Api.DTOs;

public class GenerateRoadmapResponseDto
{
    public Guid CourseId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
