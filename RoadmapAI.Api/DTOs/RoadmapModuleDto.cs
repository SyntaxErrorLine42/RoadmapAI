namespace RoadmapAI.Api.DTOs;

public class RoadmapModuleDto
{
    public Guid Id { get; set; }
    public int OrderIndex { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public List<RoadmapLessonDto> Lessons { get; set; } = [];
    public List<RoadmapExamDto> Exams { get; set; } = [];
}
