namespace RoadmapAI.Api.DTOs;

public class RoadmapLessonTaskDto
{
    public Guid LessonId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string HelpMarkdown { get; set; } = string.Empty;
    public string? CorrectOption { get; set; }
    public string? CompletedCorrectOption { get; set; }
}