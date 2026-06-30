namespace RoadmapAI.Api.DTOs;

public class RoadmapExamQuestionDto
{
    public Guid Id { get; set; }
    public int OrderIndex { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public bool IsAiGenerated { get; set; }
}
