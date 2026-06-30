using RoadmapAI.Api.Models.Enums;

namespace RoadmapAI.Api.Models;

public class CourseMaterial
{
    public Guid Id { get; set; }

    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    // Path on disk (or blob URL later)
    public string StoragePath { get; set; } = string.Empty;

    // AI auto-detects this during ingestion
    public MaterialType Type { get; set; } = MaterialType.Other;
    public MaterialStatus Status { get; set; } = MaterialStatus.Uploaded;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    // Extracted plain text from the material (stored after ingestion)
    public string? ExtractedText { get; set; }
}
