namespace RoadmapAI.Api.Models.Learning;

public class RoadmapExamAttemptAnswer
{
    public Guid Id { get; set; }
    public Guid RoadmapExamAttemptId { get; set; }
    public Guid RoadmapExamQuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string CorrectOption { get; set; } = string.Empty;
    public string SelectedOption { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    public RoadmapExamAttempt RoadmapExamAttempt { get; set; } = null!;
    public RoadmapExamQuestion RoadmapExamQuestion { get; set; } = null!;
}
