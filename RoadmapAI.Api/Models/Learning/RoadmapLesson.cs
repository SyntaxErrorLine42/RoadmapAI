using RoadmapAI.Api.Models.Enums;

namespace RoadmapAI.Api.Models.Learning;

public class RoadmapLesson
{
    public Guid Id { get; set; }
    public Guid RoadmapModuleId { get; set; }
    public int OrderIndex { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentMarkdown { get; set; } = string.Empty;
    public string? TaskQuestionText { get; set; }
    public string? TaskOptionA { get; set; }
    public string? TaskOptionB { get; set; }
    public string? TaskOptionC { get; set; }
    public string? TaskOptionD { get; set; }
    public string? TaskCorrectOption { get; set; }
    public string? TaskHelpMarkdown { get; set; }
    public LessonStatus Status { get; set; } = LessonStatus.NotGenerated;
    public DateTime? GeneratedAt { get; set; }

    public RoadmapModule RoadmapModule { get; set; } = null!;
    public ICollection<LessonCompletion> Completions { get; set; } = [];
}
