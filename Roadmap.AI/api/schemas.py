from pydantic import BaseModel, Field


class GenerateRoadmapRequest(BaseModel):
    course_title: str = Field(min_length=1, max_length=200)
    context_chunks: list[str] = Field(default_factory=list)


class GenerateRoadmapModule(BaseModel):
    title: str
    lessons: list[str]


class GenerateRoadmapResponse(BaseModel):
    summary_markdown: str = Field(default="")
    modules: list[GenerateRoadmapModule]


class GenerateLessonRequest(BaseModel):
    course_title: str = Field(min_length=1, max_length=200)
    module_title: str = Field(min_length=1, max_length=200)
    lesson_title: str = Field(min_length=1, max_length=200)
    context_chunks: list[str] = Field(default_factory=list)


class GenerateLessonResponse(BaseModel):
    content_markdown: str
    task_question: str | None = None
    task_option_a: str | None = None
    task_option_b: str | None = None
    task_option_c: str | None = None
    task_option_d: str | None = None
    task_correct_option: str | None = None
    task_explanation: str | None = None


class GenerateExamRequest(BaseModel):
    course_title: str = Field(min_length=1, max_length=200)
    module_title: str = Field(min_length=1, max_length=200)
    exam_title: str = Field(min_length=1, max_length=200)
    context_chunks: list[str] = Field(default_factory=list)
    question_count: int = Field(default=5, ge=1, le=20)


class GenerateExamQuestion(BaseModel):
    question_text: str
    option_a: str
    option_b: str
    option_c: str
    option_d: str
    correct_option: str
    explanation: str = Field(default="")
    is_ai_generated: bool = True


class GenerateExamResponse(BaseModel):
    questions: list[GenerateExamQuestion]


class GenerateReviewRequest(BaseModel):
    course_title: str = Field(min_length=1, max_length=200)
    context_chunks: list[str] = Field(default_factory=list)


class GenerateReviewResponse(BaseModel):
    review_markdown: str


class IngestMaterialRequest(BaseModel):
    course_id: str = Field(min_length=1, max_length=120)
    material_id: str = Field(min_length=1, max_length=120)
    blob_path: str = Field(min_length=1)
    file_name: str = Field(min_length=1, max_length=300)
    content_type: str = Field(default="application/octet-stream", max_length=200)


class IngestMaterialResponse(BaseModel):
    extracted_text: str = Field(default="")


