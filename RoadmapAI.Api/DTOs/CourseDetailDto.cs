namespace RoadmapAI.Api.DTOs;

public class CourseDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<CourseMaterialDto> Materials { get; set; } = [];
    public CourseRoadmapDto? Roadmap { get; set; }
}
