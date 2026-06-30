using RoadmapAI.Api.Models.Enums;

namespace RoadmapAI.Api.Models.Learning;

public class RoadmapModule
{
    public Guid Id { get; set; }
    public Guid CourseRoadmapId { get; set; }
    public int OrderIndex { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }

    public CourseRoadmap CourseRoadmap { get; set; } = null!;
    public ICollection<RoadmapLesson> Lessons { get; set; } = [];
    public ICollection<RoadmapExam> Exams { get; set; } = [];
}
