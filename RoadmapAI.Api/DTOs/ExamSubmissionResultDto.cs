namespace RoadmapAI.Api.DTOs;

public class ExamSubmissionResultDto
{
    public Guid ExamId { get; set; }
    public decimal ScorePercent { get; set; }
    public bool Passed { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public string ModuleTitle { get; set; } = string.Empty;
    public string RecommendationText { get; set; } = string.Empty;
    public List<string> RecommendedLessonTitles { get; set; } = [];
}
