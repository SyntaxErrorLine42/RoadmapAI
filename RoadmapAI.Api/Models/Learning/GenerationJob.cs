using RoadmapAI.Api.Models.Enums;

namespace RoadmapAI.Api.Models.Learning;

public class GenerationJob
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public GenerationJobEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public GenerationJobStatus Status { get; set; } = GenerationJobStatus.Queued;
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    public Course Course { get; set; } = null!;
}
