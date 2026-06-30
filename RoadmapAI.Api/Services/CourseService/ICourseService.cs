using RoadmapAI.Api.DTOs;

namespace RoadmapAI.Api.Services.CourseService;

public interface ICourseService
{
    Task<List<CourseSummaryDto>> GetAllAsync(string userId);
    Task<CourseDetailDto?> GetByIdAsync(Guid id, string userId);
    Task<CourseDetailDto?> CreateAsync(string userId, string name, List<IFormFile> files, string basePath);
    Task<CourseSummaryDto?> RenameAsync(Guid id, string userId, string name);
    Task<CourseDetailDto?> ResetHistoryAsync(Guid id, string userId);
    Task<CourseDetailDto?> StartIngestionAsync(Guid id, string userId);
    Task<List<CourseMaterialDto>?> AddMaterialsAsync(Guid id, string userId, List<IFormFile> files, string basePath);
    Task<bool> DeleteAsync(Guid id, string userId, string basePath);
    Task<bool> DeleteMaterialAsync(Guid courseId, Guid materialId, string userId);
}
