using AutoMapper;
using AutoMapper.QueryableExtensions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadmapAI.Api.Data;
using RoadmapAI.Api.DTOs;
using RoadmapAI.Api.Models.Enums;
using RoadmapAI.Api.Models;
using RoadmapAI.Api.Models.Configuration;
using RoadmapAI.Api.Services.AiService;

namespace RoadmapAI.Api.Services.CourseService;

public class CourseService(
    AppDbContext db,
    IMapper mapper,
    IAiService aiService,
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> blobOptions) : ICourseService
{
    private readonly AppDbContext _db = db;
    private readonly IMapper _mapper = mapper;
    private readonly IAiService _aiService = aiService;
    private readonly BlobServiceClient _blobServiceClient = blobServiceClient;
    private readonly BlobStorageOptions _blobOptions = blobOptions.Value;

    public async Task<List<CourseSummaryDto>> GetAllAsync(string userId)
    {
        var courses = await _db.Courses
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Include(c => c.Materials)
            .Include(c => c.Roadmap!)
                .ThenInclude(r => r.Modules)
                    .ThenInclude(m => m.Lessons)
            .Include(c => c.Roadmap!)
                .ThenInclude(r => r.Modules)
                    .ThenInclude(m => m.Exams)
            .ToListAsync();

        return courses.Select(course => new CourseSummaryDto
        {
            Id = course.Id,
            Name = course.Name,
            Status = course.Status.ToString(),
            CreatedAt = course.CreatedAt,
            MaterialCount = course.Materials.Count,
            CompletionPercent = ComputeCompletionPercent(course),
        }).ToList();
    }

    public async Task<CourseDetailDto?> GetByIdAsync(Guid id, string userId)
    {
        return await _db.Courses
            .Where(c => c.Id == id && c.UserId == userId)
            .ProjectTo<CourseDetailDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
    }

    public async Task<CourseDetailDto?> CreateAsync(string userId, string name, List<IFormFile> files, string basePath)
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            Status = CourseStatus.Processing
        };

        var containerClient = await GetContainerClientAsync();

        foreach (var file in files)
        {
            var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blobPath = $"{course.Id}/{safeFileName}";
            var blobClient = containerClient.GetBlobClient(blobPath);

            await using var uploadStream = file.OpenReadStream();
            await blobClient.UploadAsync(
                uploadStream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
                });

            course.Materials.Add(new CourseMaterial
            {
                Id = Guid.NewGuid(),
                FileName = safeFileName,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                StoragePath = blobPath,
                Type = InferMaterialType(file.FileName),
                Status = MaterialStatus.Uploaded
            });
        }

        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        return _mapper.Map<CourseDetailDto>(course);
    }

    public async Task<CourseSummaryDto?> RenameAsync(Guid id, string userId, string name)
    {
        var course = await _db.Courses
            .Include(c => c.Materials)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (course == null) return null;

        course.Name = name.Trim();
        await _db.SaveChangesAsync();

        return _mapper.Map<CourseSummaryDto>(course);
    }

    public async Task<CourseDetailDto?> ResetHistoryAsync(Guid id, string userId)
    {
        var course = await _db.Courses
            .Include(c => c.Materials)
            .Include(c => c.Roadmap)
            .Include(c => c.ProgressSnapshots)
            .Include(c => c.GenerationJobs)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (course == null) return null;

        if (course.Roadmap != null)
        {
            _db.CourseRoadmaps.Remove(course.Roadmap);
        }

        if (course.ProgressSnapshots.Count > 0)
        {
            _db.CourseProgressSnapshots.RemoveRange(course.ProgressSnapshots);
        }

        if (course.GenerationJobs.Count > 0)
        {
            _db.GenerationJobs.RemoveRange(course.GenerationJobs);
        }

        course.Summary = null;
        course.UpdatedAt = DateTime.UtcNow;
        course.Status = course.Materials.Count > 0 && course.Materials.All(m => m.Status == MaterialStatus.Ready)
            ? CourseStatus.RoadmapReady
            : CourseStatus.Processing;

        await _db.SaveChangesAsync();

        return await _db.Courses
            .Where(c => c.Id == id && c.UserId == userId)
            .ProjectTo<CourseDetailDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
    }

    public async Task<CourseDetailDto?> StartIngestionAsync(Guid id, string userId)
    {
        var course = await _db.Courses
            .Include(c => c.Materials)
            .Include(c => c.Roadmap)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (course == null) return null;

        foreach (var material in course.Materials)
        {
            if (material.Status == MaterialStatus.Uploaded)
            {
                material.Status = MaterialStatus.Ingesting;
            }
        }

        await _db.SaveChangesAsync();

        foreach (var material in course.Materials)
        {
            if (material.Status == MaterialStatus.Ingesting)
            {
                var ingested = await IngestMaterialAsync(course.Id, material);
                material.Status = ingested ? MaterialStatus.Ready : MaterialStatus.Error;
            }
        }

        if (course.Materials.All(m => m.Status == MaterialStatus.Error))
        {
            course.Status = CourseStatus.Error;
        }
        else if (course.Materials.All(m => m.Status == MaterialStatus.Ready))
        {
            course.Status = course.Roadmap == null ? CourseStatus.RoadmapReady : CourseStatus.Ready;
        }
        else
        {
            course.Status = CourseStatus.Processing;
        }

        await _db.SaveChangesAsync();

        return _mapper.Map<CourseDetailDto>(course);
    }

    public async Task<List<CourseMaterialDto>?> AddMaterialsAsync(Guid id, string userId, List<IFormFile> files, string basePath)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (course == null) return null;

        var containerClient = await GetContainerClientAsync();

        var addedMaterials = new List<CourseMaterial>();

        foreach (var file in files)
        {
            var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var blobPath = $"{course.Id}/{safeFileName}";
            var blobClient = containerClient.GetBlobClient(blobPath);

            await using var uploadStream = file.OpenReadStream();
            await blobClient.UploadAsync(
                uploadStream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
                });

            var material = new CourseMaterial
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                FileName = safeFileName,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                StoragePath = blobPath,
                Type = InferMaterialType(file.FileName),
                Status = MaterialStatus.Uploaded
            };

            _db.CourseMaterials.Add(material);
            addedMaterials.Add(material);
        }

        await _db.SaveChangesAsync();

        if (course.Status == CourseStatus.Ready)
        {
            course.Status = CourseStatus.Processing;
            await _db.SaveChangesAsync();
        }

        return _mapper.Map<List<CourseMaterialDto>>(addedMaterials);
    }

    public async Task<bool> DeleteAsync(Guid id, string userId, string basePath)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (course == null) return false;

        // no extra cleanup needed

        var containerClient = await GetContainerClientAsync();
        await foreach (var blob in containerClient.GetBlobsAsync(prefix: $"{course.Id}/"))
        {
            await containerClient.DeleteBlobIfExistsAsync(blob.Name);
        }

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMaterialAsync(Guid courseId, Guid materialId, string userId)
    {
        var material = await _db.CourseMaterials
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == materialId && m.CourseId == courseId && m.Course.UserId == userId);

        if (material == null) return false;

        var containerClient = await GetContainerClientAsync();
        if (!string.IsNullOrWhiteSpace(material.StoragePath))
        {
            await containerClient.DeleteBlobIfExistsAsync(material.StoragePath);
        }

        // no extra cleanup needed

        var course = material.Course;
        _db.CourseMaterials.Remove(material);
        await _db.SaveChangesAsync();

        var remainingStatuses = await _db.CourseMaterials
            .Where(m => m.CourseId == course.Id)
            .Select(m => m.Status)
            .ToListAsync();

        if (remainingStatuses.Count == 0 || remainingStatuses.Any(status => status != MaterialStatus.Ready))
        {
            course.Status = CourseStatus.Processing;
        }
        else
        {
            var hasRoadmap = await _db.CourseRoadmaps.AnyAsync(r => r.CourseId == course.Id);
            course.Status = hasRoadmap ? CourseStatus.Ready : CourseStatus.RoadmapReady;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    private static MaterialType InferMaterialType(string fileName)
    {
        var normalized = fileName.Trim().ToLowerInvariant();
        if (normalized.Contains("lab") || normalized.Contains("vjezba") || normalized.Contains("exercise"))
        {
            return MaterialType.LabExercise;
        }

        if (normalized.Contains("exam") || normalized.Contains("ispit") || normalized.Contains("kolokvij"))
        {
            return MaterialType.Exam;
        }

        if (normalized.Contains("lecture") || normalized.Contains("predavanje") || normalized.Contains("slides"))
        {
            return MaterialType.Lecture;
        }

        return MaterialType.Other;
    }

    private static int ComputeCompletionPercent(Course course)
    {
        if (course.Status == CourseStatus.Completed)
        {
            return 100;
        }

        var modules = course.Roadmap?.Modules?.ToList() ?? [];
        var totalLessons = modules.Sum(module => module.Lessons.Count);
        if (totalLessons == 0)
        {
            return 0;
        }

        var completedLessons = modules.Sum(module => module.Lessons.Count(lesson => lesson.Status == LessonStatus.Completed));
        var basePercent = (int)Math.Round((decimal)completedLessons / totalLessons * 100m);

        var hasPendingModuleExam = modules.Any(module =>
            module.Lessons.Count > 0 &&
            module.Lessons.All(lesson => lesson.Status == LessonStatus.Completed) &&
            module.Exams.Any(exam => !exam.IsFinal && exam.Status != ExamStatus.Completed));

        var examPenaltyPercent = hasPendingModuleExam ? (int)Math.Round(100m / totalLessons) : 0;
        return Math.Max(0, basePercent - examPenaltyPercent);
    }

    private async Task<bool> IngestMaterialAsync(Guid courseId, CourseMaterial material, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(material.StoragePath))
        {
            return false;
        }

        var response = await _aiService.IngestMaterialAsync(
            new AiIngestMaterialRequest(
                courseId.ToString(),
                material.Id.ToString(),
                material.StoragePath,
                material.OriginalFileName,
                material.ContentType),
            cancellationToken
        );

        if (response == null)
        {
            material.Status = MaterialStatus.Error;
            await _db.SaveChangesAsync(cancellationToken);
            return false;
        }

        material.ExtractedText = response.ExtractedText;
        material.Status = MaterialStatus.Ready;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_blobOptions.ContainerName))
        {
            throw new InvalidOperationException("BlobStorage:ContainerName is not configured.");
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(_blobOptions.ContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        return containerClient;
    }
}
