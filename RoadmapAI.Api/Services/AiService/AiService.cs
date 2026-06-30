using System.Net.Http.Json;

namespace RoadmapAI.Api.Services.AiService;

public class AiService(HttpClient httpClient) : IAiService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<AiIngestMaterialResponse?> IngestMaterialAsync(AiIngestMaterialRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("/generate/ingest/material", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadFromJsonAsync<AiIngestMaterialResponse>(cancellationToken);
    }

    public async Task<AiGenerateRoadmapResponse?> GenerateRoadmapAsync(AiGenerateRoadmapRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("/generate/roadmap", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AiGenerateRoadmapResponse>(cancellationToken);
    }

    public async Task<AiGenerateLessonResponse?> GenerateLessonAsync(AiGenerateLessonRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("/generate/lesson", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AiGenerateLessonResponse>(cancellationToken);
    }

    public async Task<AiGenerateExamResponse?> GenerateExamAsync(AiGenerateExamRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("/generate/exam", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AiGenerateExamResponse>(cancellationToken);
    }

    public async Task<AiGenerateReviewResponse?> GenerateReviewAsync(AiGenerateReviewRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("/generate/review", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AiGenerateReviewResponse>(cancellationToken);
    }
}
