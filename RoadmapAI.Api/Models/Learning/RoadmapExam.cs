using RoadmapAI.Api.Models.Enums;

namespace RoadmapAI.Api.Models.Learning;

public class RoadmapExam
{
    public Guid Id { get; set; }
    public Guid RoadmapModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsFinal { get; set; }
    public ExamStatus Status { get; set; } = ExamStatus.Locked;

    public RoadmapModule RoadmapModule { get; set; } = null!;
    public ICollection<RoadmapExamQuestion> Questions { get; set; } = [];
    public ICollection<RoadmapExamAttempt> Attempts { get; set; } = [];
}
