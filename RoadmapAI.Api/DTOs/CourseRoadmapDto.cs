namespace RoadmapAI.Api.DTOs;

public class CourseRoadmapDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string SummaryMarkdown { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<RoadmapModuleDto> Modules { get; set; } = [];
}
