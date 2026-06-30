namespace RoadmapAI.Api.DTOs;

public class ExamAttemptQuestionReviewDto
{
    public Guid QuestionId { get; set; }
    public int OrderIndex { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public bool IsAiGenerated { get; set; }
    public string SelectedOption { get; set; } = string.Empty;
    public string CorrectOption { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
