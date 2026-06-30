namespace RoadmapAI.Api.Models.Learning;

public class ModuleCompletion
{
    public Guid Id { get; set; }
    public Guid RoadmapModuleId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public RoadmapModule RoadmapModule { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
