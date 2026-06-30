namespace RoadmapAI.Api.Models.Learning;

public class RoadmapExamAttempt
{
    public Guid Id { get; set; }
    public Guid RoadmapExamId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public decimal ScorePercent { get; set; }
    public bool Passed { get; set; }

    public RoadmapExam RoadmapExam { get; set; } = null!;
    public AppUser User { get; set; } = null!;
    public ICollection<RoadmapExamAttemptAnswer> Answers { get; set; } = [];
}
