namespace RoadmapAI.Api.DTOs;

public class ExamAttemptSummaryDto
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string ModuleTitle { get; set; } = string.Empty;
    public bool IsFinal { get; set; }
    public decimal ScorePercent { get; set; }
    public bool Passed { get; set; }
    public DateTime SubmittedAt { get; set; }
}