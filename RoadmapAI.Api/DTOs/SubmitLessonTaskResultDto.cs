namespace RoadmapAI.Api.DTOs;

public class SubmitLessonTaskResultDto
{
    public Guid LessonId { get; set; }
    public bool IsCorrect { get; set; }
    public bool Completed { get; set; }
    public string Message { get; set; } = string.Empty;
    public string HelpMarkdown { get; set; } = string.Empty;
}