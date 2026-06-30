namespace RoadmapAI.Api.DTOs;

public class LessonGeneratedResponseDto
{
    public Guid CourseId { get; set; }
    public Guid LessonId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public RoadmapLessonTaskDto? Task { get; set; }
}
