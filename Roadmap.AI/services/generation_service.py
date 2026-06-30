import json
import re
from pathlib import Path

import requests
import logging

from services.runtime_settings import gemini_api_key, llm_endpoint


PROMPTS_DIR = Path(__file__).resolve().parents[1] / "prompts"

# Lightweight logger so we can audit outbound LLM calls during testing
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


def _load_prompt(name: str) -> str:
    try:
        return (PROMPTS_DIR / name).read_text(encoding="utf-8")
    except Exception:
        return ""


def _inject_prompt_values(prompt: str, **values: str) -> str:
    for key, value in values.items():
        prompt = prompt.replace(f"{{{key}}}", value)
    return prompt


def _call_llm(prompt: str, response_format_json: bool = False) -> str | None:
    endpoint = llm_endpoint()
    if endpoint:
        try:
            logger.info("Calling custom LLM endpoint: %s with %d chars", endpoint, len(prompt))
            response = requests.post(endpoint, json={"prompt": prompt}, timeout=90)
            response.raise_for_status()
            body = response.json()

            if isinstance(body, dict):
                for key in ("content", "text", "output", "response"):
                    value = body.get(key)
                    if isinstance(value, str) and value.strip():
                        return value.strip()

                choices = body.get("choices")
                if isinstance(choices, list) and len(choices) > 0 and isinstance(choices[0], dict):
                    message = choices[0].get("message")
                    if isinstance(message, dict):
                        content = message.get("content")
                        if isinstance(content, str) and content.strip():
                            return content.strip()
        except Exception as ex:
            logger.info("Custom LLM endpoint failed: %s", ex)
            # continue to try Gemini or other fallbacks
            

    api_key = gemini_api_key()
    if not api_key:
        return None

    for model_name in ("gemini-3.1-flash-lite-preview", "gemini-2.5-flash-lite", "gemini-2.5-flash", "gemini-2.0-flash", "gemini-1.5-flash"):
        try:
            url = f"https://generativelanguage.googleapis.com/v1beta/models/{model_name}:generateContent?key={api_key}"
            logger.info("Calling Gemini model %s with %d chars", model_name, len(prompt))
            
            payload = {
                "contents": [{"parts": [{"text": prompt}]}],
                "generationConfig": {
                    "maxOutputTokens": 8192
                }
            }
            if response_format_json:
                payload["generationConfig"]["responseMimeType"] = "application/json"

            response = requests.post(
                url,
                json=payload,
                timeout=90,
            )
            response.raise_for_status()
            body = response.json()

            candidates = body.get("candidates", []) if isinstance(body, dict) else []
            if isinstance(candidates, list) and candidates:
                content = candidates[0].get("content", {}) if isinstance(candidates[0], dict) else {}
                parts = content.get("parts", []) if isinstance(content, dict) else []
                if isinstance(parts, list) and parts:
                    text = parts[0].get("text") if isinstance(parts[0], dict) else None
                    if isinstance(text, str) and text.strip():
                        logger.info("Gemini model %s returned %d chars", model_name, len(text))
                        return text.strip()
        except Exception as ex:
            logger.info("Gemini model %s failed: %s", model_name, ex)
            continue

    logger.info("All LLM attempts failed; returning None to trigger fallback.")
    return None



def _extract_json(text: str) -> dict | list | None:
    """Parse JSON from LLM output. Used for roadmap and exam (small, structured JSON)."""
    if not text:
        return None

    cleaned = text.strip()
    if cleaned.startswith("```json"):
        cleaned = cleaned[7:]
    if cleaned.endswith("```"):
        cleaned = cleaned[:-3]
    cleaned = cleaned.strip()

    # Try direct parse
    try:
        return json.loads(cleaned, strict=False)
    except Exception:
        pass

    # Try extracting a JSON block
    match = re.search(r"\{[\s\S]*\}|\[[\s\S]*\]", cleaned)
    if match:
        try:
            return json.loads(match.group(0), strict=False)
        except Exception:
            pass

    logger.info("JSON parse failed (len=%d, excerpt=%s)", len(cleaned), cleaned[:200].replace("\n", " "))
    return None


def _ensure_markdown_structure(text: str, heading: str) -> str:
    cleaned = (text or "").replace("\r\n", "\n").strip()
    if not cleaned:
        return f"{heading}\n\nNo content available."
    return cleaned


def _parse_lesson_response(text: str, lesson_title: str) -> dict[str, object] | None:
    """Parse lesson LLM output using ---TASK--- separator."""
    if not text:
        return None

    # Split on the separator
    separator = "---TASK---"
    parts = text.split(separator, 1)

    content_markdown = parts[0].strip()
    if not content_markdown:
        return None

    # Parse the small task JSON block (if present)
    task = {}
    if len(parts) > 1:
        task_text = parts[1].strip()
        # Strip code fences if the model wrapped it
        if task_text.startswith("```json"):
            task_text = task_text[7:]
        if task_text.startswith("```"):
            task_text = task_text[3:]
        if task_text.endswith("```"):
            task_text = task_text[:-3]
        task_text = task_text.strip()
        try:
            task = json.loads(task_text, strict=False)
        except Exception:
            # Regex fallback for each field
            for field in ("question_text", "option_a", "option_b", "option_c", "option_d", "correct_option", "explanation"):
                m = re.search(f'"{field}"\\s*:\\s*"([^"]*)"', task_text)
                if m:
                    task[field] = m.group(1)

    return {
        "content_markdown": _ensure_markdown_structure(content_markdown, lesson_title),
        "task_question": str(task.get("question_text", "")).strip() or None,
        "task_option_a": str(task.get("option_a", "")).strip() or None,
        "task_option_b": str(task.get("option_b", "")).strip() or None,
        "task_option_c": str(task.get("option_c", "")).strip() or None,
        "task_option_d": str(task.get("option_d", "")).strip() or None,
        "task_correct_option": str(task.get("correct_option", "")).strip().upper() or None,
        "task_explanation": str(task.get("explanation", "")).strip() or None,
    }


def generate_roadmap_modules(course_title: str, context_chunks: list[str]) -> dict[str, object]:
    context = "\n\n".join(context_chunks)
    prompt = _load_prompt("roadmap.txt") or (
        "TODO: Replace roadmap prompt text. Return strict JSON only with shape {\"summary_markdown\":\"...\",\"modules\":[{\"title\":\"...\",\"lessons\":[\"...\"]}]}."
    )
    prompt = _inject_prompt_values(prompt, course_title=course_title, context=context)

    text = _call_llm(prompt, response_format_json=True)
    parsed = _extract_json(text or "")
    if parsed is None:
        return None

    if isinstance(parsed, dict) and isinstance(parsed.get("modules"), list):
        modules = []
        for module in parsed["modules"]:
            if not isinstance(module, dict):
                continue
            title = str(module.get("title", "")).strip()
            lessons_raw = module.get("lessons", [])
            if not title or not isinstance(lessons_raw, list):
                continue
            lessons = [str(item).strip() for item in lessons_raw if str(item).strip()]
            if lessons:
                modules.append({"title": title, "lessons": lessons[:6]})
        if modules:
            return {"summary_markdown": _ensure_markdown_structure(str(parsed.get("summary_markdown", "")), f"Course Overview - {course_title}"), "modules": modules[:6]}

    return None


def generate_lesson_combined(course_title: str, module_title: str, lesson_title: str, context_chunks: list[str]) -> dict[str, object]:
    context = "\n\n".join(context_chunks)
    prompt = _load_prompt("lesson.txt") or "Write a lesson in markdown, then ---TASK--- separator, then a JSON task object."
    prompt = _inject_prompt_values(prompt, course_title=course_title, module_title=module_title, lesson_title=lesson_title, context=context)

    # No JSON mode - lesson is raw markdown, not wrapped in JSON
    text = _call_llm(prompt, response_format_json=False)
    return _parse_lesson_response(text, lesson_title)


def generate_exam_questions(
    course_title: str,
    module_title: str,
    exam_title: str,
    context_chunks: list[str],
    question_count: int,
) -> list[dict[str, object]]:
    context = "\n\n".join(context_chunks)
    prompt = _load_prompt("exam.txt") or (
        "TODO: Replace exam prompt text. Return strict JSON only with shape {\"questions\":[{\"question_text\":\"...\",\"option_a\":\"...\",\"option_b\":\"...\",\"option_c\":\"...\",\"option_d\":\"...\",\"correct_option\":\"A|B|C|D\"}]}."
    )
    prompt = _inject_prompt_values(prompt, course_title=course_title, module_title=module_title, exam_title=exam_title, question_count=str(question_count), context=context)

    text = _call_llm(prompt, response_format_json=True)
    parsed = _extract_json(text or "")
    if parsed is None:
        return None

    if isinstance(parsed, dict) and isinstance(parsed.get("questions"), list):
        questions: list[dict[str, object]] = []
        for item in parsed["questions"]:
            if not isinstance(item, dict):
                continue
            question_text = str(item.get("question_text", "")).strip()
            option_a = str(item.get("option_a", "")).strip()
            option_b = str(item.get("option_b", "")).strip()
            option_c = str(item.get("option_c", "")).strip()
            option_d = str(item.get("option_d", "")).strip()
            correct_option = str(item.get("correct_option", "A")).strip().upper()
            if correct_option not in {"A", "B", "C", "D"}:
                correct_option = "A"
            if question_text and option_a and option_b and option_c and option_d:
                questions.append({
                    "question_text": question_text,
                    "option_a": option_a,
                    "option_b": option_b,
                    "option_c": option_c,
                    "option_d": option_d,
                    "correct_option": correct_option,
                    "is_ai_generated": True,
                })
        if questions:
            return questions[:question_count]

    return None


def generate_review_markdown(course_title: str, context_chunks: list[str]) -> str:
    context = "\n\n".join(context_chunks)
    prompt = _load_prompt("review.txt") or (
        "Write a concise HTML review with summary, skeleton, progress, exams solved/not solved, and incorrect answer guidance."
    )
    prompt = _inject_prompt_values(prompt, course_title=course_title, context=context)

    text = _call_llm(prompt)
    if not text:
        return None

    if isinstance(text, str) and text.strip():
        return _ensure_markdown_structure(text, f"Review for {course_title}")

    return None
