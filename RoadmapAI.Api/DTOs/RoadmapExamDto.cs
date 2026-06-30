namespace RoadmapAI.Api.DTOs;

public class RoadmapExamDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsFinal { get; set; }
    public string Status { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
}
