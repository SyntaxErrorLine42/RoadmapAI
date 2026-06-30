using RoadmapAI.Api.DTOs;

namespace RoadmapAI.Api.Services.RoadmapService;

public interface IRoadmapService
{
    Task<GenerateRoadmapResponseDto?> GenerateRoadmapAsync(Guid courseId, string userId);
    Task<CourseRoadmapDto?> GetRoadmapAsync(Guid courseId, string userId);
    Task<LessonGeneratedResponseDto?> GenerateLessonAsync(Guid courseId, Guid lessonId, string userId);
    Task<RoadmapLessonTaskDto?> GetLessonTaskAsync(Guid courseId, Guid lessonId, string userId);
    Task<SubmitLessonTaskResultDto?> SubmitLessonTaskAsync(Guid courseId, Guid lessonId, string userId, SubmitLessonTaskRequestDto request);
    Task<bool> CompleteLessonAsync(Guid courseId, Guid lessonId, string userId);
    Task<List<ExamAttemptSummaryDto>> GetExamAttemptsAsync(Guid courseId, string userId);
    Task<ExamAttemptDetailDto?> GetExamAttemptDetailAsync(Guid courseId, Guid attemptId, string userId);
    Task<List<RoadmapExamQuestionDto>?> GetExamQuestionsAsync(Guid courseId, Guid examId, string userId);
    Task<ExamSubmissionResultDto?> SubmitExamAsync(Guid courseId, Guid examId, string userId, SubmitExamRequestDto request);
    Task RefreshExamStatusesAsync(Guid courseId, string userId);
    Task<CourseReviewDetailDto?> GenerateReviewAsync(Guid courseId, string userId);
    Task<List<CourseReviewSummaryDto>> GetCourseReviewsAsync(Guid courseId, string userId);
    Task<CourseReviewDetailDto?> GetCourseReviewAsync(Guid courseId, Guid reviewId, string userId);
}
