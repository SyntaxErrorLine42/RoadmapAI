namespace RoadmapAI.Api.Services.AiService;

public interface IAiService
{
    Task<AiIngestMaterialResponse?> IngestMaterialAsync(AiIngestMaterialRequest request, CancellationToken cancellationToken = default);
    Task<AiGenerateRoadmapResponse?> GenerateRoadmapAsync(AiGenerateRoadmapRequest request, CancellationToken cancellationToken = default);
    Task<AiGenerateLessonResponse?> GenerateLessonAsync(AiGenerateLessonRequest request, CancellationToken cancellationToken = default);
    Task<AiGenerateExamResponse?> GenerateExamAsync(AiGenerateExamRequest request, CancellationToken cancellationToken = default);
    Task<AiGenerateReviewResponse?> GenerateReviewAsync(AiGenerateReviewRequest request, CancellationToken cancellationToken = default);
    
}
