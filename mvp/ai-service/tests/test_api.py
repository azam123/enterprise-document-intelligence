from fastapi.testclient import TestClient
from app.main import app

client = TestClient(app)

def test_health_endpoint_identifies_local_inference():
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json()["llm_provider"] == "local-ollama"

def test_ask_requires_tenant_header():
    response = client.post("/ask", json={"question": "What does the policy say?"})
    assert response.status_code == 422

def test_ask_rejects_too_short_question_before_database_access():
    response = client.post("/ask", headers={"X-Tenant-Id": "tenant-a"}, json={"question": "hi"})
    assert response.status_code == 422

def test_upload_requires_tenant_header():
    response = client.post("/documents", files={"file": ("policy.txt", b"Policy content", "text/plain")})
    assert response.status_code == 422
