using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadmapAI.Api.DTOs;
using RoadmapAI.Api.Services.CourseService;
using RoadmapAI.Api.Services.RoadmapService;

namespace RoadmapAI.Api.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class CourseController(ICourseService courseService, IRoadmapService roadmapService, IWebHostEnvironment env) : ControllerBase
{
    private readonly ICourseService _courseService = courseService;
    private readonly IRoadmapService _roadmapService = roadmapService;
    private readonly IWebHostEnvironment _env = env;

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /courses - all courses for current user
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var courses = await _courseService.GetAllAsync(GetUserId());
        return Ok(courses);
    }

    // GET /courses/{id} - single course with materials
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        await _roadmapService.RefreshExamStatusesAsync(id, GetUserId());
        var course = await _courseService.GetByIdAsync(id, GetUserId());

        if (course == null)
            return NotFound();

        return Ok(course);
    }

    // POST /courses - create course with file uploads
    [HttpPost]
    [RequestSizeLimit(200_000_000)] // 200MB max total
    public async Task<IActionResult> Create([FromForm] string name, [FromForm] List<IFormFile> files)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Course name is required" });

        if (files == null || files.Count == 0)
            return BadRequest(new { message = "At least one file is required" });

        var course = await _courseService.CreateAsync(GetUserId(), name, files, _env.ContentRootPath);

        if (course != null)
        {
            await _courseService.StartIngestionAsync(course.Id, GetUserId());
        }

        return Ok(new { course!.Id, course.Name });
    }

    // POST /courses/{id}/materials - append new materials
    [HttpPost("{id:guid}/materials")]
    [RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> AddMaterials(Guid id, [FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { message = "At least one file is required" });

        var newMaterials = await _courseService.AddMaterialsAsync(id, GetUserId(), files, _env.ContentRootPath);

        if (newMaterials == null)
            return NotFound();

        return Ok(newMaterials);
    }

    // POST /courses/{id}/ingest/start
    [HttpPost("{id:guid}/ingest/start")]
    public async Task<IActionResult> StartIngestion(Guid id)
    {
        var course = await _courseService.StartIngestionAsync(id, GetUserId());
        if (course == null)
            return NotFound();

        return Ok(course);
    }

    // PUT /courses/{id} - rename course
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] RenameCourseDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Course name is required" });

        var trimmedName = request.Name.Trim();
        if (trimmedName.Length > 120)
            return BadRequest(new { message = "Course name must be 120 characters or fewer" });

        var renamedCourse = await _courseService.RenameAsync(id, GetUserId(), trimmedName);
        if (renamedCourse == null)
            return NotFound();

        return Ok(renamedCourse);
    }

    // POST /courses/{id}/reset
    [HttpPost("{id:guid}/reset")]
    public async Task<IActionResult> ResetCourse(Guid id)
    {
        var course = await _courseService.ResetHistoryAsync(id, GetUserId());
        if (course == null)
            return NotFound();

        return Ok(course);
    }

    // POST /courses/{id}/roadmap/generate
    [HttpPost("{id:guid}/roadmap/generate")]
    public async Task<IActionResult> GenerateRoadmap(Guid id)
    {
        var result = await _roadmapService.GenerateRoadmapAsync(id, GetUserId());
        if (result == null)
            return NotFound();

        if (result.Message.Contains("must be in Ready status", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = result.Message, status = result.Status });

        return Ok(result);
    }

    // GET /courses/{id}/roadmap
    [HttpGet("{id:guid}/roadmap")]
    public async Task<IActionResult> GetRoadmap(Guid id)
    {
        var roadmap = await _roadmapService.GetRoadmapAsync(id, GetUserId());
        if (roadmap == null)
            return NotFound();

        return Ok(roadmap);
    }

    // POST /courses/{courseId}/lessons/{lessonId}/generate
    [HttpPost("{courseId:guid}/lessons/{lessonId:guid}/generate")]
    public async Task<IActionResult> GenerateLesson(Guid courseId, Guid lessonId)
    {
        var result = await _roadmapService.GenerateLessonAsync(courseId, lessonId, GetUserId());
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    

    // GET /courses/{courseId}/lessons/{lessonId}/task
    [HttpGet("{courseId:guid}/lessons/{lessonId:guid}/task")]
    public async Task<IActionResult> GetLessonTask(Guid courseId, Guid lessonId)
    {
        var task = await _roadmapService.GetLessonTaskAsync(courseId, lessonId, GetUserId());
        if (task == null)
            return NotFound();

        return Ok(task);
    }

    // POST /courses/{courseId}/lessons/{lessonId}/task/submit
    [HttpPost("{courseId:guid}/lessons/{lessonId:guid}/task/submit")]
    public async Task<IActionResult> SubmitLessonTask(Guid courseId, Guid lessonId, [FromBody] SubmitLessonTaskRequestDto request)
    {
        var result = await _roadmapService.SubmitLessonTaskAsync(courseId, lessonId, GetUserId(), request);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // POST /courses/{courseId}/lessons/{lessonId}/complete
    [HttpPost("{courseId:guid}/lessons/{lessonId:guid}/complete")]
    public async Task<IActionResult> CompleteLesson(Guid courseId, Guid lessonId)
    {
        var completed = await _roadmapService.CompleteLessonAsync(courseId, lessonId, GetUserId());
        if (!completed)
            return NotFound();

        return Ok(new { message = "Lesson completed" });
    }

    // POST /courses/{courseId}/exams/{examId}/submit
    [HttpPost("{courseId:guid}/exams/{examId:guid}/submit")]
    public async Task<IActionResult> SubmitExam(Guid courseId, Guid examId, [FromBody] SubmitExamRequestDto request)
    {
        var result = await _roadmapService.SubmitExamAsync(courseId, examId, GetUserId(), request);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // GET /courses/{courseId}/exams/{examId}/questions
    [HttpGet("{courseId:guid}/exams/{examId:guid}/questions")]
    public async Task<IActionResult> GetExamQuestions(Guid courseId, Guid examId)
    {
        var questions = await _roadmapService.GetExamQuestionsAsync(courseId, examId, GetUserId());
        if (questions == null)
        {
            var roadmap = await _roadmapService.GetRoadmapAsync(courseId, GetUserId());
            if (roadmap == null)
                return NotFound(new { message = "Course roadmap not found." });

            var examEntry = roadmap.Modules
                .SelectMany(module => module.Exams.Select(exam => new { Module = module, Exam = exam }))
                .FirstOrDefault(item => item.Exam.Id == examId);

            if (examEntry == null)
                return NotFound(new { message = "Exam not found." });

            if (!examEntry.Exam.Status.Equals("Ready", StringComparison.OrdinalIgnoreCase) &&
                !examEntry.Exam.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                if (examEntry.Exam.IsFinal)
                {
                    return BadRequest(new { message = "Final exam is locked. Complete all lessons and all module exams first." });
                }

                return BadRequest(new { message = "Module exam is locked. Complete all lessons in this module first." });
            }

            return BadRequest(new { message = "Exam is marked ready but cannot be opened right now. Refresh the course page and try again." });
        }

        return Ok(questions);
    }

    // GET /courses/{courseId}/exams/attempts
    [HttpGet("{courseId:guid}/exams/attempts")]
    public async Task<IActionResult> GetExamAttempts(Guid courseId)
    {
        var attempts = await _roadmapService.GetExamAttemptsAsync(courseId, GetUserId());
        return Ok(attempts);
    }

    // GET /courses/{courseId}/exams/attempts/{attemptId}
    [HttpGet("{courseId:guid}/exams/attempts/{attemptId:guid}")]
    public async Task<IActionResult> GetExamAttemptDetail(Guid courseId, Guid attemptId)
    {
        var attempt = await _roadmapService.GetExamAttemptDetailAsync(courseId, attemptId, GetUserId());
        if (attempt == null)
            return NotFound();

        return Ok(attempt);
    }

    // POST /courses/{courseId}/reviews/generate
    [HttpPost("{courseId:guid}/reviews/generate")]
    public async Task<IActionResult> GenerateReview(Guid courseId)
    {
        var review = await _roadmapService.GenerateReviewAsync(courseId, GetUserId());
        if (review == null)
            return NotFound();

        return Ok(review);
    }

    // GET /courses/{courseId}/reviews
    [HttpGet("{courseId:guid}/reviews")]
    public async Task<IActionResult> GetReviews(Guid courseId)
    {
        var reviews = await _roadmapService.GetCourseReviewsAsync(courseId, GetUserId());
        return Ok(reviews);
    }

    // GET /courses/{courseId}/reviews/{reviewId}
    [HttpGet("{courseId:guid}/reviews/{reviewId:guid}")]
    public async Task<IActionResult> GetReview(Guid courseId, Guid reviewId)
    {
        var review = await _roadmapService.GetCourseReviewAsync(courseId, reviewId, GetUserId());
        if (review == null)
            return NotFound();

        return Ok(review);
    }

    // DELETE /courses/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _courseService.DeleteAsync(id, GetUserId(), _env.ContentRootPath);

        if (!success)
            return NotFound();

        return Ok(new { message = "Course deleted" });
    }

    // DELETE /courses/{courseId}/materials/{materialId}
    [HttpDelete("{courseId:guid}/materials/{materialId:guid}")]
    public async Task<IActionResult> DeleteMaterial(Guid courseId, Guid materialId)
    {
        var success = await _courseService.DeleteMaterialAsync(courseId, materialId, GetUserId());

        if (!success)
            return NotFound();

        return Ok(new { message = "Material deleted" });
    }
}
