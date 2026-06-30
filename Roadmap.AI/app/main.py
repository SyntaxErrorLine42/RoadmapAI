from fastapi import FastAPI
from dotenv import load_dotenv
from pathlib import Path

load_dotenv(Path(__file__).resolve().parents[1] / ".env")

from app.routers import generate

app = FastAPI(title="RoadmapAI AI Service")

app.include_router(generate.router)

@app.get("/")
def read_root():
    return {"msg": "RoadmapAI AI microservice is running."}
