using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RoadmapAI.Api.Data;
using RoadmapAI.Api.DTOs;
using RoadmapAI.Api.Models;
using RoadmapAI.Api.Models.Enums;
using RoadmapAI.Api.Models.Learning;
using RoadmapAI.Api.Services.AiService;
using System.Collections.Concurrent;
using System.Threading;

namespace RoadmapAI.Api.Services.RoadmapService;

public class RoadmapService(AppDbContext db, IMapper mapper, IAiService aiService) : IRoadmapService
{
    private readonly AppDbContext _db = db;
    private readonly IMapper _mapper = mapper;
    private readonly IAiService _aiService = aiService;
    // Prevent concurrent generation for the same lesson id
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _lessonGenerationLocks = new();

    public async Task<GenerateRoadmapResponseDto?> GenerateRoadmapAsync(Guid courseId, string userId)
    {
        var course = await _db.Courses
            .Include(c => c.Materials)
            .Include(c => c.Roadmap)
            .FirstOrDefaultAsync(c => c.Id == courseId && c.UserId == userId);
        if (course == null) return null;

        if (course.Materials.Any(m => m.Status != MaterialStatus.Ready))
        {
            return new GenerateRoadmapResponseDto
            {
                CourseId = courseId,
                Status = course.Status.ToString(),
                Message = "All materials must be in Ready status before generating roadmap.",
            };
        }

        course.Status = CourseStatus.RoadmapGenerating;
        await _db.SaveChangesAsync();

        if (course.Roadmap != null)
        {
            var oldModules = await _db.RoadmapModules
                .Where(m => m.CourseRoadmapId == course.Roadmap.Id)
                .ToListAsync();
            if (oldModules.Count > 0)
            {
                var oldModuleIds = oldModules.Select(m => m.Id).ToList();
                var oldLessons = await _db.RoadmapLessons.Where(l => oldModuleIds.Contains(l.RoadmapModuleId)).ToListAsync();
                var oldExams = await _db.RoadmapExams.Where(ex => oldModuleIds.Contains(ex.RoadmapModuleId)).ToListAsync();
                var oldExamIds = oldExams.Select(ex => ex.Id).ToList();
                var oldQuestions = await _db.RoadmapExamQuestions.Where(q => oldExamIds.Contains(q.RoadmapExamId)).ToListAsync();

                _db.RoadmapExamQuestions.RemoveRange(oldQuestions);
                _db.RoadmapLessons.RemoveRange(oldLessons);
                _db.RoadmapExams.RemoveRange(oldExams);
                _db.RoadmapModules.RemoveRange(oldModules);
            }
            _db.CourseRoadmaps.Remove(course.Roadmap);
            await _db.SaveChangesAsync();
        }

        // Fetch roadmap and summary in ONE call
        var (summaryMarkdown, roadmapModules) = await BuildRoadmapWithSummaryAsync(course);

        if (roadmapModules == null)
        {
            // Upstream AI parsing failed; revert course status and inform client
            course.Status = CourseStatus.Ready;
            await _db.SaveChangesAsync();
            Console.WriteLine($"[RoadmapService] Roadmap generation failed for course {course.Id}");
            return new GenerateRoadmapResponseDto
            {
                CourseId = courseId,
                Status = course.Status.ToString(),
                Message = "AI parsing failed. Please try again.",
            };
        }

        var roadmap = new CourseRoadmap
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            SummaryMarkdown = summaryMarkdown ?? string.Empty,
            GeneratedAt = DateTime.UtcNow,
        };

        for (var moduleIndex = 1; moduleIndex <= roadmapModules.Count; moduleIndex++)
        {
            var generatedModule = roadmapModules[moduleIndex - 1];
            var module = new RoadmapModule
            {
                Id = Guid.NewGuid(),
                CourseRoadmapId = roadmap.Id,
                OrderIndex = moduleIndex,
                Title = generatedModule.Title,
            };

            var lessonTitles = generatedModule.Lessons.Count > 0
                ? generatedModule.Lessons
                : ["Lesson 1", "Lesson 2", "Lesson 3"];

            for (var lessonIndex = 1; lessonIndex <= lessonTitles.Count; lessonIndex++)
            {
                module.Lessons.Add(new RoadmapLesson
                {
                    Id = Guid.NewGuid(),
                    RoadmapModuleId = module.Id,
                    OrderIndex = lessonIndex,
                    Title = lessonTitles[lessonIndex - 1],
                    Status = LessonStatus.NotGenerated,
                    ContentMarkdown = string.Empty,
                });
            }

            module.Exams.Add(new RoadmapExam
            {
                Id = Guid.NewGuid(),
                RoadmapModuleId = module.Id,
                Title = $"Module {moduleIndex} Exam",
                IsFinal = false,
                Status = ExamStatus.Locked,
            });

            roadmap.Modules.Add(module);
        }

        if (roadmap.Modules.Count > 0)
        {
            var finalModule = roadmap.Modules.Last();
            finalModule.Exams.Add(new RoadmapExam
            {
                Id = Guid.NewGuid(),
                RoadmapModuleId = finalModule.Id,
                Title = "Final Exam",
                IsFinal = true,
                Status = ExamStatus.Locked,
            });
        }

        _db.CourseRoadmaps.Add(roadmap);
        course.Summary = roadmap.SummaryMarkdown;
        course.Status = CourseStatus.Ready;

        _db.GenerationJobs.Add(new GenerationJob
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            EntityType = GenerationJobEntityType.CourseRoadmap,
            EntityId = roadmap.Id,
            Status = GenerationJobStatus.Done,
            StartedAt = DateTime.UtcNow,
            FinishedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync();

        return new GenerateRoadmapResponseDto
        {
            CourseId = course.Id,
            Status = course.Status.ToString(),
            Message = "Roadmap generated successfully.",
        };
    }

    private async Task<(string? Summary, List<AiGenerateRoadmapModule>? Modules)> BuildRoadmapWithSummaryAsync(Course course)
    {
        var chunks = await BuildContextChunksAsync(course.Id, 12);

        var aiRoadmap = await _aiService.GenerateRoadmapAsync(
            new AiGenerateRoadmapRequest(course.Name, chunks)
        );

        if (aiRoadmap == null)
        {
            Console.WriteLine($"[RoadmapService] Roadmap generation parse failure for course {course.Id}");
            return (null, null);
        }

        var modules = (aiRoadmap?.Modules ?? [])
            .Where(module => !string.IsNullOrWhiteSpace(module.Title))
            .Select(module => new AiGenerateRoadmapModule(
                module.Title.Trim(),
                (module.Lessons ?? [])
                    .Where(lesson => !string.IsNullOrWhiteSpace(lesson))
                    .Select(lesson => lesson.Trim())
                    .Take(6)
                    .ToList()
            ))
            .Where(module => module.Lessons.Count > 0)
            .Take(6)
            .ToList();

        var summary = aiRoadmap?.SummaryMarkdown?.Trim();
        if (string.IsNullOrWhiteSpace(summary))
        {
            summary = "## Course Overview\n\nStudy plan generated from your materials.";
        }

        if (modules.Count > 0)
        {
            return (summary, modules);
        }

        var fallbackSummary = "## Course Overview\n\nStudy plan generated from your materials.";
        var fallbackModules = new List<AiGenerateRoadmapModule>
        {
            new AiGenerateRoadmapModule("Module 1", ["Lesson 1.1", "Lesson 1.2", "Lesson 1.3", "Lesson 1.4"]),
            new AiGenerateRoadmapModule("Module 2", ["Lesson 2.1", "Lesson 2.2", "Lesson 2.3", "Lesson 2.4"]),
            new AiGenerateRoadmapModule("Module 3", ["Lesson 3.1", "Lesson 3.2", "Lesson 3.3", "Lesson 3.4"]),
        };

        return (fallbackSummary, fallbackModules);
    }

    public async Task<CourseRoadmapDto?> GetRoadmapAsync(Guid courseId, string userId)
    {
        var roadmap = await _db.CourseRoadmaps
            .AsNoTracking()
            .Where(r => r.CourseId == courseId && r.Course.UserId == userId)
            .Include(r => r.Modules.OrderBy(m => m.OrderIndex))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.OrderIndex))
            .Include(r => r.Modules)
                .ThenInclude(m => m.Exams)
                    .ThenInclude(ex => ex.Questions)
            .FirstOrDefaultAsync();
        if (roadmap == null) return null;

        return _mapper.Map<CourseRoadmapDto>(roadmap);
    }

    public async Task<LessonGeneratedResponseDto?> GenerateLessonAsync(Guid courseId, Guid lessonId, string userId)
    {
        var lessonLock = _lessonGenerationLocks.GetOrAdd(lessonId, _ => new SemaphoreSlim(1, 1));
        await lessonLock.WaitAsync();
        try
        {
            var lesson = await _db.RoadmapLessons
                .Include(l => l.RoadmapModule)
                    .ThenInclude(m => m.CourseRoadmap)
                        .ThenInclude(r => r.Course)
                .FirstOrDefaultAsync(l =>
                    l.Id == lessonId &&
                    l.RoadmapModule.CourseRoadmap.CourseId == courseId &&
                    l.RoadmapModule.CourseRoadmap.Course.UserId == userId);
            if (lesson == null) return null;

            var modules = await RefreshExamStatusesInternalAsync(courseId, userId);
            if (!IsModuleUnlocked(modules, lesson.RoadmapModuleId))
            {
                return null;
            }

            var previousStatus = lesson.Status;

            lesson.Status = LessonStatus.Generating;
            await _db.SaveChangesAsync();

        var lessonChunks = await BuildContextChunksAsync(courseId, 8);

        var aiLesson = await _aiService.GenerateLessonAsync(
            new AiGenerateLessonRequest(
                lesson.RoadmapModule.CourseRoadmap.Course.Name,
                lesson.RoadmapModule.Title,
                lesson.Title,
                lessonChunks
            )
        );

        if (aiLesson == null)
        {
            // AI response parsing failed upstream. Revert lesson state and return error to client.
            lesson.Status = LessonStatus.NotGenerated;
            await _db.SaveChangesAsync();
            Console.WriteLine($"[RoadmapService] Lesson generation parse failure for lesson {lesson.Id} in course {courseId}");

            return new LessonGeneratedResponseDto
            {
                CourseId = courseId,
                LessonId = lesson.Id,
                Status = "Error",
                ContentMarkdown = string.Empty,
                Task = null,
            };
        }

            lesson.ContentMarkdown = !string.IsNullOrWhiteSpace(aiLesson.ContentMarkdown)
                ? aiLesson.ContentMarkdown.Trim()
                : $"# {lesson.Title}\n\nThis lesson was generated from your uploaded materials.";

        // Populate interactive task fields from the combined response
            if (!string.IsNullOrWhiteSpace(aiLesson.TaskQuestion))
            {
                lesson.TaskQuestionText = aiLesson.TaskQuestion;
                lesson.TaskOptionA = aiLesson.TaskOptionA;
                lesson.TaskOptionB = aiLesson.TaskOptionB;
                lesson.TaskOptionC = aiLesson.TaskOptionC;
                lesson.TaskOptionD = aiLesson.TaskOptionD;
                lesson.TaskCorrectOption = aiLesson.TaskCorrectOption;
                lesson.TaskHelpMarkdown = aiLesson.TaskExplanation;
            }

            lesson.GeneratedAt = DateTime.UtcNow;
            lesson.Status = previousStatus == LessonStatus.Completed ? LessonStatus.Completed : LessonStatus.Generated;

            _db.GenerationJobs.Add(new GenerationJob
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                EntityType = GenerationJobEntityType.Lesson,
                EntityId = lesson.Id,
                Status = GenerationJobStatus.Done,
                StartedAt = DateTime.UtcNow,
                FinishedAt = DateTime.UtcNow,
            });

            await _db.SaveChangesAsync();

            // Build lesson task DTO to return together with content so frontend needs only one request
            var taskDto = new RoadmapLessonTaskDto
            {
                LessonId = lesson.Id,
                QuestionText = lesson.TaskQuestionText ?? "Review the lesson content and identify the key takeaway.",
                OptionA = lesson.TaskOptionA ?? "Option A",
                OptionB = lesson.TaskOptionB ?? "Option B",
                OptionC = lesson.TaskOptionC ?? "Option C",
                OptionD = lesson.TaskOptionD ?? "Option D",
                CorrectOption = lesson.Status == LessonStatus.Completed ? lesson.TaskCorrectOption : null,
                CompletedCorrectOption = lesson.Status == LessonStatus.Completed ? lesson.TaskCorrectOption : null,
                HelpMarkdown = lesson.TaskHelpMarkdown ?? "<p>No hint available for this task.</p>",
            };

            return new LessonGeneratedResponseDto
            {
                CourseId = courseId,
                LessonId = lesson.Id,
                Status = lesson.Status.ToString(),
                ContentMarkdown = lesson.ContentMarkdown,
                Task = taskDto,
            };
        }
        finally
        {
            lessonLock.Release();
        }
    }

    public async Task<bool> CompleteLessonAsync(Guid courseId, Guid lessonId, string userId)
    {
        var lesson = await _db.RoadmapLessons
            .Include(l => l.RoadmapModule)
                .ThenInclude(m => m.CourseRoadmap)
            .FirstOrDefaultAsync(l =>
                l.Id == lessonId &&
                l.RoadmapModule.CourseRoadmap.CourseId == courseId &&
                l.RoadmapModule.CourseRoadmap.Course.UserId == userId);
        if (lesson == null) return false;

        if (lesson.Status == LessonStatus.Completed)
        {
            return true;
        }

        return false;
    }

    public async Task<RoadmapLessonTaskDto?> GetLessonTaskAsync(Guid courseId, Guid lessonId, string userId)
    {
        var lesson = await _db.RoadmapLessons
            .Include(l => l.RoadmapModule)
                .ThenInclude(m => m.CourseRoadmap)
            .FirstOrDefaultAsync(l =>
                l.Id == lessonId &&
                l.RoadmapModule.CourseRoadmap.CourseId == courseId &&
                l.RoadmapModule.CourseRoadmap.Course.UserId == userId);
        if (lesson == null) return null;

        var modules = await RefreshExamStatusesInternalAsync(courseId, userId);
        if (!IsModuleUnlocked(modules, lesson.RoadmapModuleId))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(lesson.TaskQuestionText))
        {
            await EnsureLessonTaskAsync(lesson, courseId);
        }

        return new RoadmapLessonTaskDto
        {
            LessonId = lesson.Id,
            QuestionText = lesson.TaskQuestionText ?? "Review the lesson content and identify the key takeaway.",
            OptionA = lesson.TaskOptionA ?? "Option A",
            OptionB = lesson.TaskOptionB ?? "Option B",
            OptionC = lesson.TaskOptionC ?? "Option C",
            OptionD = lesson.TaskOptionD ?? "Option D",
            CorrectOption = lesson.Status == LessonStatus.Completed ? lesson.TaskCorrectOption : null,
            CompletedCorrectOption = lesson.Status == LessonStatus.Completed ? lesson.TaskCorrectOption : null,
            HelpMarkdown = lesson.TaskHelpMarkdown ?? "No hint available for this task.",
        };
    }

    public async Task<SubmitLessonTaskResultDto?> SubmitLessonTaskAsync(Guid courseId, Guid lessonId, string userId, SubmitLessonTaskRequestDto request)
    {
        var lesson = await _db.RoadmapLessons
            .Include(l => l.RoadmapModule)
                .ThenInclude(m => m.CourseRoadmap)
            .FirstOrDefaultAsync(l =>
                l.Id == lessonId &&
                l.RoadmapModule.CourseRoadmap.CourseId == courseId &&
                l.RoadmapModule.CourseRoadmap.Course.UserId == userId);
        if (lesson == null) return null;

        var modules = await RefreshExamStatusesInternalAsync(courseId, userId);
        if (!IsModuleUnlocked(modules, lesson.RoadmapModuleId))
        {
            return null;
        }

        var selectedOption = request.SelectedOption.Trim().ToUpperInvariant();
        var correctOption = (lesson.TaskCorrectOption ?? "A").Trim().ToUpperInvariant();
        var isCorrect = selectedOption == correctOption;

        if (!isCorrect)
        {
            return new SubmitLessonTaskResultDto
            {
                LessonId = lesson.Id,
                IsCorrect = false,
                Completed = lesson.Status == LessonStatus.Completed,
                Message = "Incorrect answer. Review the lesson and try again.",
                HelpMarkdown = lesson.TaskHelpMarkdown ?? "No hint available.",
            };
        }

        lesson.Status = LessonStatus.Completed;
        var completion = await _db.LessonCompletions
            .FirstOrDefaultAsync(c => c.RoadmapLessonId == lesson.Id && c.UserId == userId);
        if (completion == null)
        {
            _db.LessonCompletions.Add(new LessonCompletion
            {
                Id = Guid.NewGuid(),
                RoadmapLessonId = lesson.Id,
                UserId = userId,
                CompletedAt = DateTime.UtcNow,
            });
        }

        await _db.SaveChangesAsync();
        await RefreshExamStatusesInternalAsync(courseId, userId);

        return new SubmitLessonTaskResultDto
        {
            LessonId = lesson.Id,
            IsCorrect = true,
            Completed = true,
            Message = "Correct answer! Lesson completed.",
            HelpMarkdown = "Great job! You have mastered this lesson's objective.",
        };
    }

    private async Task EnsureLessonTaskAsync(RoadmapLesson lesson, Guid courseId)
    {
        var taskChunks = await BuildContextChunksAsync(courseId, 5);

        var aiResponse = await _aiService.GenerateExamAsync(
            new AiGenerateExamRequest(
                lesson.RoadmapModule.CourseRoadmap.Course.Name,
                lesson.RoadmapModule.Title,
                lesson.Title,
                taskChunks,
                1
            )
        );

        if (aiResponse?.Questions != null && aiResponse.Questions.Count > 0)
        {
            var q = aiResponse.Questions[0];
            lesson.TaskQuestionText = q.QuestionText;
            lesson.TaskOptionA = q.OptionA;
            lesson.TaskOptionB = q.OptionB;
            lesson.TaskOptionC = q.OptionC;
            lesson.TaskOptionD = q.OptionD;
            lesson.TaskCorrectOption = q.CorrectOption;
            lesson.TaskHelpMarkdown = q.Explanation;
        }
        else
        {
            lesson.TaskQuestionText = $"What is the main objective of {lesson.Title}?";
            lesson.TaskOptionA = "To understand the core principles";
            lesson.TaskOptionB = "To ignore the practical application";
            lesson.TaskOptionC = "To focus on unrelated details";
            lesson.TaskOptionD = "To skip the theoretical foundation";
            lesson.TaskCorrectOption = "A";
            lesson.TaskHelpMarkdown = null;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<RoadmapExamQuestionDto>?> GetExamQuestionsAsync(Guid courseId, Guid examId, string userId)
    {
        var exam = await _db.RoadmapExams
            .Include(ex => ex.Questions)
            .Include(ex => ex.RoadmapModule)
                .ThenInclude(m => m.CourseRoadmap)
                    .ThenInclude(r => r.Course)
            .FirstOrDefaultAsync(ex =>
                ex.Id == examId &&
                ex.RoadmapModule.CourseRoadmap.CourseId == courseId &&
                ex.RoadmapModule.CourseRoadmap.Course.UserId == userId);
        if (exam == null) return null;

            var modules = await RefreshExamStatusesInternalAsync(courseId, userId);
            if (!CanOpenExam(modules, exam.Id))
            {
                return null;
            }

        var questions = await EnsureExamQuestionsAsync(exam, courseId);

        return questions
            .OrderBy(q => q.OrderIndex)
            .Select(q => new RoadmapExamQuestionDto
            {
                Id = q.Id,
                OrderIndex = q.OrderIndex,
                QuestionText = q.QuestionText,
                OptionA = q.OptionA,
                OptionB = q.OptionB,
                OptionC = q.OptionC,
                OptionD = q.OptionD,
                IsAiGenerated = q.IsAiGenerated,
            })
            .ToList();
    }

    public async Task<ExamSubmissionResultDto?> SubmitExamAsync(Guid courseId, Guid examId, string userId, SubmitExamRequestDto request)
    {
        var exam = await _db.RoadmapExams
            .Include(ex => ex.Questions)
            .Include(ex => ex.RoadmapModule)
                .ThenInclude(m => m.Lessons)
            .Include(ex => ex.RoadmapModule)
                .ThenInclude(m => m.CourseRoadmap)
                    .ThenInclude(r => r.Course)
            .FirstOrDefaultAsync(ex =>
                ex.Id == examId &&
                ex.RoadmapModule.CourseRoadmap.CourseId == courseId &&
                ex.RoadmapModule.CourseRoadmap.Course.UserId == userId);
        if (exam == null) return null;

            var modules = await RefreshExamStatusesInternalAsync(courseId, userId);
            if (!CanOpenExam(modules, exam.Id))
            {
                return null;
            }

        var questions = await EnsureExamQuestionsAsync(exam, courseId);

        var normalized = request.Answers
            .GroupBy(a => a.QuestionId)
            .Select(g => g.Last())
            .ToDictionary(a => a.QuestionId, a => a.SelectedOption.Trim().ToUpperInvariant());

        var correctCount = questions.Count(q => normalized.TryGetValue(q.Id, out var selected) && selected == q.CorrectOption);
        var incorrectQuestions = questions
            .Where(question => !normalized.TryGetValue(question.Id, out var selected) || selected != question.CorrectOption)
            .ToList();
        var total = questions.Count;
        var scorePercent = total == 0 ? 0 : (decimal)correctCount / total * 100m;
        var passed = scorePercent >= 50m;

        var attempt = new RoadmapExamAttempt
        {
            Id = Guid.NewGuid(),
            RoadmapExamId = exam.Id,
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
            ScorePercent = scorePercent,
            Passed = passed,
        };

        foreach (var question in questions)
        {
            var selected = normalized.TryGetValue(question.Id, out var option) ? option : string.Empty;
            attempt.Answers.Add(new RoadmapExamAttemptAnswer
            {
                Id = Guid.NewGuid(),
                RoadmapExamAttemptId = attempt.Id,
                RoadmapExamQuestionId = question.Id,
                QuestionText = question.QuestionText,
                CorrectOption = question.CorrectOption,
                SelectedOption = selected,
                IsCorrect = selected == question.CorrectOption,
            });
        }

        _db.RoadmapExamAttempts.Add(attempt);

        if (passed)
        {
            exam.Status = ExamStatus.Completed;
        }

        await _db.SaveChangesAsync();
        await RefreshExamStatusesInternalAsync(courseId, userId);

        var moduleTitle = exam.RoadmapModule.Title;
        var recommendedLessonTitles = new List<string>();
        if (!passed && exam.RoadmapModule.Lessons.Count > 0)
        {
            var orderedLessons = exam.RoadmapModule.Lessons.OrderBy(lesson => lesson.OrderIndex).ToList();
            var pickedLessons = incorrectQuestions
                .Select(question => orderedLessons[(question.OrderIndex - 1) % orderedLessons.Count].Title)
                .Distinct()
                .Take(3)
                .ToList();

            recommendedLessonTitles.AddRange(pickedLessons);
        }

        var recommendationText = passed
            ? $"Great result in {moduleTitle}. Continue to the next module."
            : recommendedLessonTitles.Count == 0
                ? $"Review {moduleTitle} lessons and retry this exam."
                : $"Repeat these lessons in {moduleTitle}, then retry the exam.";

        return new ExamSubmissionResultDto
        {
            ExamId = exam.Id,
            ScorePercent = scorePercent,
            Passed = passed,
            CorrectAnswers = correctCount,
            TotalQuestions = total,
            ModuleTitle = moduleTitle,
            RecommendationText = recommendationText,
            RecommendedLessonTitles = recommendedLessonTitles,
        };
    }


    public async Task RefreshExamStatusesAsync(Guid courseId, string userId)
    {
        await RefreshExamStatusesInternalAsync(courseId, userId);
    }
    public async Task<List<ExamAttemptSummaryDto>> GetExamAttemptsAsync(Guid courseId, string userId)
    {
        return await _db.RoadmapExamAttempts
            .AsNoTracking()
            .Where(attempt =>
                attempt.UserId == userId &&
                attempt.RoadmapExam.RoadmapModule.CourseRoadmap.CourseId == courseId)
            .OrderByDescending(attempt => attempt.SubmittedAt)
            .Select(attempt => new ExamAttemptSummaryDto
            {
                Id = attempt.Id,
                ExamId = attempt.RoadmapExamId,
                ExamTitle = attempt.RoadmapExam.Title,
                ModuleTitle = attempt.RoadmapExam.RoadmapModule.Title,
                IsFinal = attempt.RoadmapExam.IsFinal,
                ScorePercent = attempt.ScorePercent,
                Passed = attempt.Passed,
                SubmittedAt = attempt.SubmittedAt ?? attempt.StartedAt,
            })
            .Take(30)
            .ToListAsync();
    }

    public async Task<ExamAttemptDetailDto?> GetExamAttemptDetailAsync(Guid courseId, Guid attemptId, string userId)
    {
        var attempt = await _db.RoadmapExamAttempts
            .AsNoTracking()
            .Where(item =>
                item.Id == attemptId &&
                item.UserId == userId &&
                item.RoadmapExam.RoadmapModule.CourseRoadmap.CourseId == courseId)
            .Select(item => new ExamAttemptDetailDto
            {
                Id = item.Id,
                ExamId = item.RoadmapExamId,
                ExamTitle = item.RoadmapExam.Title,
                ModuleTitle = item.RoadmapExam.RoadmapModule.Title,
                IsFinal = item.RoadmapExam.IsFinal,
                ScorePercent = item.ScorePercent,
                Passed = item.Passed,
                SubmittedAt = item.SubmittedAt ?? item.StartedAt,
                Questions = item.Answers
                    .OrderBy(answer => answer.RoadmapExamQuestion.OrderIndex)
                    .Select(answer => new ExamAttemptQuestionReviewDto
                    {
                        QuestionId = answer.RoadmapExamQuestionId,
                        OrderIndex = answer.RoadmapExamQuestion.OrderIndex,
                        QuestionText = string.IsNullOrWhiteSpace(answer.QuestionText) ? answer.RoadmapExamQuestion.QuestionText : answer.QuestionText,
                        OptionA = answer.RoadmapExamQuestion.OptionA,
                        OptionB = answer.RoadmapExamQuestion.OptionB,
                        OptionC = answer.RoadmapExamQuestion.OptionC,
                        OptionD = answer.RoadmapExamQuestion.OptionD,
                        IsAiGenerated = answer.RoadmapExamQuestion.IsAiGenerated,
                        SelectedOption = answer.SelectedOption,
                        CorrectOption = string.IsNullOrWhiteSpace(answer.CorrectOption) ? answer.RoadmapExamQuestion.CorrectOption : answer.CorrectOption,
                        IsCorrect = answer.IsCorrect,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync();

        return attempt;
    }

    public async Task<CourseReviewDetailDto?> GenerateReviewAsync(Guid courseId, string userId)
    {
        var course = await _db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.UserId == userId);
        if (course == null)
        {
            return null;
        }

        var modules = await _db.RoadmapModules
            .AsNoTracking()
            .Where(m => m.CourseRoadmap.CourseId == courseId)
            .Include(m => m.Lessons)
            .Include(m => m.Exams)
            .OrderBy(m => m.OrderIndex)
            .ToListAsync();

        var attempts = await _db.RoadmapExamAttempts
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.RoadmapExam.RoadmapModule.CourseRoadmap.CourseId == courseId)
            .Include(a => a.RoadmapExam)
                .ThenInclude(ex => ex.RoadmapModule)
            .Include(a => a.Answers)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        var contextChunks = BuildReviewContextChunks(course.Name, modules, attempts);
        var aiResponse = await _aiService.GenerateReviewAsync(new AiGenerateReviewRequest(course.Name, contextChunks));
        var reviewMarkdown = aiResponse?.ReviewMarkdown?.Trim();
        if (string.IsNullOrWhiteSpace(reviewMarkdown))
        {
            reviewMarkdown = "Review unavailable.";
        }

        var review = new CourseReview
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            CreatedAt = DateTime.UtcNow,
            ContentMarkdown = reviewMarkdown,
        };

        _db.CourseReviews.Add(review);
        await _db.SaveChangesAsync();

        return new CourseReviewDetailDto
        {
            Id = review.Id,
            CourseId = review.CourseId,
            CreatedAt = review.CreatedAt,
            ContentMarkdown = review.ContentMarkdown,
        };
    }

    public async Task<List<CourseReviewSummaryDto>> GetCourseReviewsAsync(Guid courseId, string userId)
    {
        var hasCourse = await _db.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Id == courseId && c.UserId == userId);
        if (!hasCourse)
        {
            return [];
        }

        var reviews = await _db.CourseReviews
            .AsNoTracking()
            .Where(r => r.CourseId == courseId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(30)
            .Select(r => new
            {
                r.Id,
                r.CreatedAt,
                r.ContentMarkdown,
            })
            .ToListAsync();

        return reviews
            .Select(r => new CourseReviewSummaryDto
            {
                Id = r.Id,
                CreatedAt = r.CreatedAt,
                Preview = BuildReviewPreview(r.ContentMarkdown),
            })
            .ToList();
    }

    public async Task<CourseReviewDetailDto?> GetCourseReviewAsync(Guid courseId, Guid reviewId, string userId)
    {
        var review = await _db.CourseReviews
            .AsNoTracking()
            .Where(r => r.Id == reviewId && r.CourseId == courseId && r.Course.UserId == userId)
            .Select(r => new CourseReviewDetailDto
            {
                Id = r.Id,
                CourseId = r.CourseId,
                CreatedAt = r.CreatedAt,
                ContentMarkdown = r.ContentMarkdown,
            })
            .FirstOrDefaultAsync();

        return review;
    }

    private async Task<List<RoadmapExamQuestion>> EnsureExamQuestionsAsync(RoadmapExam exam, Guid courseId)
    {
        var existingQuestions = await _db.RoadmapExamQuestions
            .Where(question => question.RoadmapExamId == exam.Id)
            .OrderBy(question => question.OrderIndex)
            .ToListAsync();

        if (existingQuestions.Count > 0)
        {
            return existingQuestions;
        }

        var examChunks = await BuildContextChunksAsync(courseId, 10);
        var count = exam.IsFinal ? 20 : 5;
        var takeCount = exam.IsFinal ? 20 : 10;

        var aiExam = await _aiService.GenerateExamAsync(
            new AiGenerateExamRequest(
                exam.RoadmapModule.CourseRoadmap.Course.Name,
                exam.RoadmapModule.Title,
                exam.Title,
                examChunks,
                count
            )
        );

        var generatedQuestions = (aiExam?.Questions ?? [])
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.QuestionText) &&
                !string.IsNullOrWhiteSpace(item.OptionA) &&
                !string.IsNullOrWhiteSpace(item.OptionB) &&
                !string.IsNullOrWhiteSpace(item.OptionC) &&
                !string.IsNullOrWhiteSpace(item.OptionD))
            .Take(takeCount)
            .Select((item, index) =>
            {
                var correctOption = item.CorrectOption.Trim().ToUpperInvariant();
                if (correctOption is not ("A" or "B" or "C" or "D"))
                {
                    correctOption = "A";
                }

                return new RoadmapExamQuestion
                {
                    Id = Guid.NewGuid(),
                    RoadmapExamId = exam.Id,
                    OrderIndex = index + 1,
                    QuestionText = item.QuestionText,
                    OptionA = item.OptionA,
                    OptionB = item.OptionB,
                    OptionC = item.OptionC,
                    OptionD = item.OptionD,
                    CorrectOption = correctOption,
                    IsAiGenerated = item.IsAiGenerated,
                };
            })
            .ToList();

        if (generatedQuestions.Count == 0)
        {
            generatedQuestions = Enumerable.Range(1, takeCount)
                .Select(index => new RoadmapExamQuestion
                {
                    Id = Guid.NewGuid(),
                    RoadmapExamId = exam.Id,
                    OrderIndex = index,
                    QuestionText = $"Question {index} for {exam.Title}",
                    OptionA = "Option A",
                    OptionB = "Option B",
                    OptionC = "Option C",
                    OptionD = "Option D",
                    CorrectOption = "A",
                    IsAiGenerated = true,
                })
                .ToList();
        }

        _db.RoadmapExamQuestions.AddRange(generatedQuestions);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            _db.ChangeTracker.Clear();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
        }

        return await _db.RoadmapExamQuestions
            .Where(question => question.RoadmapExamId == exam.Id)
            .OrderBy(question => question.OrderIndex)
            .ToListAsync();
    }

    private static List<string> BuildReviewContextChunks(string courseName, List<RoadmapModule> modules, List<RoadmapExamAttempt> attempts)
    {
        var totalLessons = modules.Sum(m => m.Lessons.Count);
        var completedLessons = modules.Sum(m => m.Lessons.Count(l => l.Status == LessonStatus.Completed));
        var completionPercent = totalLessons == 0 ? 0m : Math.Round((decimal)completedLessons / totalLessons * 100m, 2);
        var totalExams = modules.Sum(m => m.Exams.Count);
        var completedExams = modules.Sum(m => m.Exams.Count(ex => ex.Status == ExamStatus.Completed));

        var skeletonLines = modules
            .OrderBy(m => m.OrderIndex)
            .SelectMany(module => new[] { $"Module {module.OrderIndex}: {module.Title}" }
                .Concat(module.Lessons.OrderBy(l => l.OrderIndex)
                    .Select(lesson => $"- Lesson {lesson.OrderIndex}: {lesson.Title} [{(lesson.Status == LessonStatus.Completed ? "COMPLETED" : "NOT_COMPLETED")}]")));

        var examLines = modules
            .SelectMany(module => module.Exams.Select(exam =>
            {
                var examAttempts = attempts.Where(a => a.RoadmapExamId == exam.Id).ToList();
                var failedCount = examAttempts.Count(a => !a.Passed);
                var bestScore = examAttempts.Count == 0 ? 0m : examAttempts.Max(a => a.ScorePercent);
                var solved = exam.Status == ExamStatus.Completed ? "SOLVED" : "NOT_SOLVED";
                return $"- {exam.Title} ({module.Title}) [{solved}] attempts={examAttempts.Count} failed={failedCount} best_score={bestScore:0.##}%";
            }));

        var wrongLines = attempts
            .SelectMany(attempt => attempt.Answers
                .Where(answer => !answer.IsCorrect)
                .Take(10)
                .Select(answer =>
                {
                    var questionText = string.IsNullOrWhiteSpace(answer.QuestionText)
                        ? "Question text unavailable"
                        : answer.QuestionText.Replace("\n", " ").Trim();
                    return $"- {attempt.RoadmapExam.Title}: {questionText} | selected={answer.SelectedOption}, correct={answer.CorrectOption}";
                }))
            .Take(40);

        return
        [
            $"Course: {courseName}\nLessons: {completedLessons}/{totalLessons} ({completionPercent}%)\nExams: {completedExams}/{totalExams}\nAttempts: {attempts.Count}",
            "Lesson skeleton:\n" + string.Join("\n", skeletonLines),
            "Exams:\n" + string.Join("\n", examLines),
            "Incorrect answers:\n" + string.Join("\n", wrongLines),
        ];
    }

    private static string BuildReviewPreview(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return "Course review";

        var text = markdown.Replace("\r", " ").Replace("\n", " ").Trim();

        return text.Length <= 140 ? text : text[..140].TrimEnd() + "...";
    }

    private async Task<List<string>> BuildContextChunksAsync(Guid courseId, int _)
    {
        // Simple flow: one context chunk per material, using stored extracted text.
        var materials = await _db.CourseMaterials
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.UploadedAt)
            .ToListAsync();

        var results = new List<string>();

        foreach (var material in materials)
        {
            var parts = new List<string>();
            var displayName = string.IsNullOrWhiteSpace(material.OriginalFileName)
                ? material.FileName
                : material.OriginalFileName;

            parts.Add($"Material: {displayName}");
            parts.Add($"Type: {material.Type}");

            if (!string.IsNullOrWhiteSpace(material.ExtractedText))
            {
                parts.Add("Extracted text:\n" + material.ExtractedText.Trim());
            }

            var piece = string.Join("\n\n", parts).Trim();
            if (string.IsNullOrWhiteSpace(piece))
            {
                continue;
            }

            results.Add(piece);
        }

        return results;
    }

    private async Task<List<RoadmapModule>> RefreshExamStatusesInternalAsync(Guid courseId, string userId)
    {
        var modules = await _db.RoadmapModules
            .Where(m => m.CourseRoadmap.CourseId == courseId && m.CourseRoadmap.Course.UserId == userId)
            .Include(m => m.Lessons)
            .Include(m => m.Exams)
            .OrderBy(m => m.OrderIndex)
            .ToListAsync();

        var statusChanged = false;
        foreach (var module in modules)
        {
            foreach (var exam in module.Exams)
            {
                if (exam.Status == ExamStatus.Completed)
                {
                    continue;
                }

                var shouldBeReady = CanOpenExam(modules, exam.Id);
                var nextStatus = shouldBeReady ? ExamStatus.Ready : ExamStatus.Locked;
                if (exam.Status != nextStatus)
                {
                    exam.Status = nextStatus;
                    statusChanged = true;
                }
            }
        }

        if (statusChanged)
        {
            await _db.SaveChangesAsync();
        }

        return modules;
    }

    private static bool IsModuleUnlocked(List<RoadmapModule> modules, Guid moduleId)
    {
        return true;
    }

    private static bool CanOpenExam(List<RoadmapModule> modules, Guid examId)
    {
        var ownerModule = modules.FirstOrDefault(module => module.Exams.Any(exam => exam.Id == examId));
        if (ownerModule == null)
        {
            return false;
        }

        var exam = ownerModule.Exams.FirstOrDefault(item => item.Id == examId);
        if (exam == null)
        {
            return false;
        }

        if (!exam.IsFinal)
        {
            return ownerModule.Lessons.Count > 0 && ownerModule.Lessons.All(lesson => lesson.Status == LessonStatus.Completed);
        }

        var allLessonsCompleted = modules.All(module => module.Lessons.All(lesson => lesson.Status == LessonStatus.Completed));
        var allModuleExamsCompleted = modules
            .SelectMany(module => module.Exams)
            .Where(moduleExam => !moduleExam.IsFinal)
            .All(moduleExam => moduleExam.Status == ExamStatus.Completed);

        return allLessonsCompleted && allModuleExamsCompleted;
    }
}
