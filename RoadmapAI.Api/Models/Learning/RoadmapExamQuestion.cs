namespace RoadmapAI.Api.Models.Learning;

public class RoadmapExamQuestion
{
    public Guid Id { get; set; }
    public Guid RoadmapExamId { get; set; }
    public int OrderIndex { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string CorrectOption { get; set; } = "A";
    public bool IsAiGenerated { get; set; }
    public Guid? SourceMaterialId { get; set; }

    public RoadmapExam RoadmapExam { get; set; } = null!;
}
