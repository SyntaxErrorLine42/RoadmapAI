from fastapi import APIRouter

from api.schemas import (
    GenerateExamRequest,
    GenerateExamResponse,
    GenerateExamQuestion,
    GenerateLessonRequest,
    GenerateLessonResponse,
    GenerateReviewRequest,
    GenerateReviewResponse,
    GenerateRoadmapModule,
    GenerateRoadmapRequest,
    GenerateRoadmapResponse,
    IngestMaterialRequest,
    IngestMaterialResponse,
)
from services.generation_service import (
    generate_exam_questions,
    generate_lesson_combined,
    generate_review_markdown,
    generate_roadmap_modules,
)
from services.ingestion_service import ingest_material_from_blob
from fastapi import HTTPException

router = APIRouter(prefix="/generate", tags=["generate"])


@router.post("/roadmap", response_model=GenerateRoadmapResponse)
def generate_roadmap(request: GenerateRoadmapRequest) -> GenerateRoadmapResponse:
    roadmap = generate_roadmap_modules(request.course_title, request.context_chunks)
    if roadmap is None:
        raise HTTPException(status_code=502, detail="Parsing of AI response failed. Something went wrong - please try again.")
    return GenerateRoadmapResponse(
        summary_markdown=str(roadmap.get("summary_markdown", "")),
        modules=[
            GenerateRoadmapModule(
                title=str(module["title"]),
                lessons=[str(item) for item in module["lessons"]],
            )
            for module in roadmap.get("modules", [])
        ]
    )


@router.post("/lesson", response_model=GenerateLessonResponse)
def generate_lesson(request: GenerateLessonRequest) -> GenerateLessonResponse:
    result = generate_lesson_combined(
        request.course_title,
        request.module_title,
        request.lesson_title,
        request.context_chunks,
    )
    if result is None:
        raise HTTPException(status_code=502, detail="Parsing of AI response failed. Something went wrong - please try again.")
    return GenerateLessonResponse(
        content_markdown=str(result["content_markdown"]),
        task_question=result.get("task_question"),
        task_option_a=result.get("task_option_a"),
        task_option_b=result.get("task_option_b"),
        task_option_c=result.get("task_option_c"),
        task_option_d=result.get("task_option_d"),
        task_correct_option=result.get("task_correct_option"),
        task_explanation=result.get("task_explanation"),
    )


@router.post("/exam", response_model=GenerateExamResponse)
def generate_exam(request: GenerateExamRequest) -> GenerateExamResponse:
    questions = generate_exam_questions(
        request.course_title,
        request.module_title,
        request.exam_title,
        request.context_chunks,
        request.question_count,
    )
    if questions is None:
        raise HTTPException(status_code=502, detail="Parsing of AI response failed. Something went wrong - please try again.")

    return GenerateExamResponse(
        questions=[
            GenerateExamQuestion(
                question_text=str(item["question_text"]),
                option_a=str(item["option_a"]),
                option_b=str(item["option_b"]),
                option_c=str(item["option_c"]),
                option_d=str(item["option_d"]),
                correct_option=str(item["correct_option"]),
                is_ai_generated=bool(item.get("is_ai_generated", True)),
            )
            for item in questions
        ]
    )


@router.post("/review", response_model=GenerateReviewResponse)
def generate_review(request: GenerateReviewRequest) -> GenerateReviewResponse:
    review_markdown = generate_review_markdown(
        request.course_title,
        request.context_chunks,
    )
    if review_markdown is None:
        raise HTTPException(status_code=502, detail="Parsing of AI response failed. Something went wrong - please try again.")
    return GenerateReviewResponse(review_markdown=review_markdown)


@router.post("/ingest/material", response_model=IngestMaterialResponse)
def ingest_material(request: IngestMaterialRequest) -> IngestMaterialResponse:
    extracted_text = ingest_material_from_blob(
        course_id=request.course_id,
        material_id=request.material_id,
        blob_path=request.blob_path,
        file_name=request.file_name,
        content_type=request.content_type,
    )

    return IngestMaterialResponse(extracted_text=extracted_text)
