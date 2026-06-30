import os
from pathlib import Path


def _load_env_file() -> None:
    env_path = Path(__file__).resolve().parents[1] / ".env"
    if not env_path.exists():
        return

    for line in env_path.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith("#") or "=" not in stripped:
            continue
        name, value = stripped.split("=", 1)
        os.environ.setdefault(name.strip(), value.strip())


_load_env_file()


def _env(name: str, default: str = "") -> str:
    return (os.getenv(name) or default).strip()


def blob_connection_string() -> str:
    return _env("BLOB_CONNECTION_STRING")


def blob_container_name() -> str:
    return "course-materials"


def blob_api_version() -> str:
    return _env("BLOB_API_VERSION", "2023-11-03")


def llm_endpoint() -> str:
    return _env("LLM_ENDPOINT")


def gemini_api_key() -> str:
    return _env("GEMINI_API_KEY")
