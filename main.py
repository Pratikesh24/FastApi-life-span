from fastapi import FastAPI  # type: ignore[import]
from models import TeklaRequest
from generator import generate_csharp_code

app = FastAPI(
    title="C# Code Generator API",
    version="1.0"
)

@app.get("/")
def read_root():
    return {
        "status": "ok",
        "message": "Use POST /generate-code with JSON {\"message\": \"your request\"}.",
        "docs": "http://127.0.0.1:8000/docs"
    }

@app.get("/test")
def test():
    return {"status": "ok"}

@app.get("/generate-code")
def generate_code_query(message: str):
    code = generate_csharp_code(message)
    return {
        "input": message,
        "generated_csharp_code": code,
        "status": "200 OK"
    }

@app.post("/generate-code")
def generate_code(req: TeklaRequest):
    code = generate_csharp_code(req.message)
    return {
        "input": req.message,
        "generated_csharp_code": code,
        "status": "200 OK"
    }