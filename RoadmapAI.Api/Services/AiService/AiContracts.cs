using System.Text.Json.Serialization;

namespace RoadmapAI.Api.Services.AiService;

public sealed record AiIngestMaterialRequest(
	[property: JsonPropertyName("course_id")] string CourseId,
	[property: JsonPropertyName("material_id")] string MaterialId,
	[property: JsonPropertyName("blob_path")] string BlobPath,
	[property: JsonPropertyName("file_name")] string FileName,
	[property: JsonPropertyName("content_type")] string ContentType);
public sealed record AiIngestMaterialResponse(
	[property: JsonPropertyName("extracted_text")] string ExtractedText);

public sealed record AiGenerateRoadmapRequest(
	[property: JsonPropertyName("course_title")] string CourseTitle,
	[property: JsonPropertyName("context_chunks")] List<string> ContextChunks);

public sealed record AiGenerateRoadmapModule(
	[property: JsonPropertyName("title")] string Title,
	[property: JsonPropertyName("lessons")] List<string> Lessons);

public sealed record AiGenerateRoadmapResponse(
	[property: JsonPropertyName("summary_markdown")] string SummaryMarkdown,
	[property: JsonPropertyName("modules")] List<AiGenerateRoadmapModule> Modules);

public sealed record AiGenerateLessonRequest(
	[property: JsonPropertyName("course_title")] string CourseTitle,
	[property: JsonPropertyName("module_title")] string ModuleTitle,
	[property: JsonPropertyName("lesson_title")] string LessonTitle,
	[property: JsonPropertyName("context_chunks")] List<string> ContextChunks);

public sealed record AiGenerateLessonResponse(
	[property: JsonPropertyName("content_markdown")] string ContentMarkdown,
	[property: JsonPropertyName("task_question")] string? TaskQuestion,
	[property: JsonPropertyName("task_option_a")] string? TaskOptionA,
	[property: JsonPropertyName("task_option_b")] string? TaskOptionB,
	[property: JsonPropertyName("task_option_c")] string? TaskOptionC,
	[property: JsonPropertyName("task_option_d")] string? TaskOptionD,
	[property: JsonPropertyName("task_correct_option")] string? TaskCorrectOption,
	[property: JsonPropertyName("task_explanation")] string? TaskExplanation);

public sealed record AiGenerateExamRequest(
	[property: JsonPropertyName("course_title")] string CourseTitle,
	[property: JsonPropertyName("module_title")] string ModuleTitle,
	[property: JsonPropertyName("exam_title")] string ExamTitle,
	[property: JsonPropertyName("context_chunks")] List<string> ContextChunks,
	[property: JsonPropertyName("question_count")] int QuestionCount = 5);

public sealed record AiGenerateExamQuestion(
	[property: JsonPropertyName("question_text")] string QuestionText,
	[property: JsonPropertyName("option_a")] string OptionA,
	[property: JsonPropertyName("option_b")] string OptionB,
	[property: JsonPropertyName("option_c")] string OptionC,
	[property: JsonPropertyName("option_d")] string OptionD,
	[property: JsonPropertyName("correct_option")] string CorrectOption,
	[property: JsonPropertyName("explanation")] string Explanation,
	[property: JsonPropertyName("is_ai_generated")] bool IsAiGenerated);

public sealed record AiGenerateExamResponse(
	[property: JsonPropertyName("questions")] List<AiGenerateExamQuestion> Questions);

public sealed record AiGenerateReviewRequest(
	[property: JsonPropertyName("course_title")] string CourseTitle,
	[property: JsonPropertyName("context_chunks")] List<string> ContextChunks);

public sealed record AiGenerateReviewResponse(
	[property: JsonPropertyName("review_markdown")] string ReviewMarkdown);
