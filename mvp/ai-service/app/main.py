import json, os, uuid
from contextlib import asynccontextmanager
from typing import Any
import httpx, psycopg
from fastapi import FastAPI, File, Header, HTTPException, UploadFile
from pydantic import BaseModel, Field
from app.core import build_grounded_prompt, chunk_text, extract_document

DATABASE_URL=os.getenv("DATABASE_URL","postgresql://edi:edi_dev_password@postgres:5432/edi")
OLLAMA_URL=os.getenv("OLLAMA_URL","http://ollama:11434")
CHAT_MODEL=os.getenv("CHAT_MODEL","qwen2.5:3b")
EMBED_MODEL=os.getenv("EMBED_MODEL","nomic-embed-text")
KAFKA=os.getenv("KAFKA_BOOTSTRAP_SERVERS","")

def db_connect():
    return psycopg.connect(DATABASE_URL)

def init_db():
    with db_connect() as conn:
        conn.execute("CREATE EXTENSION IF NOT EXISTS vector")
        conn.execute("""CREATE TABLE IF NOT EXISTS documents(
          id UUID PRIMARY KEY, tenant_id TEXT NOT NULL, filename TEXT NOT NULL,
          content_type TEXT, created_at TIMESTAMPTZ NOT NULL DEFAULT now())""")
        conn.execute("""CREATE TABLE IF NOT EXISTS document_chunks(
          id UUID PRIMARY KEY, document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
          tenant_id TEXT NOT NULL, filename TEXT NOT NULL, page_number INTEGER NOT NULL,
          content TEXT NOT NULL, embedding vector(768) NOT NULL)""")
        conn.execute("CREATE INDEX IF NOT EXISTS ix_chunks_tenant ON document_chunks(tenant_id)")
        conn.commit()

async def embed(text: str) -> list[float]:
    async with httpx.AsyncClient(timeout=120) as client:
        r=await client.post(f"{OLLAMA_URL}/api/embed",json={"model":EMBED_MODEL,"input":text})
        r.raise_for_status()
    vectors=r.json().get("embeddings") or []
    if not vectors or len(vectors[0]) != 768:
        raise RuntimeError("Embedding model must return 768 dimensions; use nomic-embed-text.")
    return vectors[0]

async def answer(prompt: str) -> str:
    async with httpx.AsyncClient(timeout=180) as client:
        r=await client.post(f"{OLLAMA_URL}/api/chat",json={"model":CHAT_MODEL,"stream":False,
          "messages":[{"role":"system","content":"You are a private enterprise document assistant. Follow evidence-only instructions."},
                      {"role":"user","content":prompt}]})
        r.raise_for_status()
    return r.json().get("message",{}).get("content","").strip()

async def publish(event: dict[str,Any]):
    if not KAFKA: return
    from aiokafka import AIOKafkaProducer
    producer=AIOKafkaProducer(bootstrap_servers=KAFKA)
    await producer.start()
    try: await producer.send_and_wait("document-events",json.dumps(event).encode())
    finally: await producer.stop()

@asynccontextmanager
async def lifespan(app: FastAPI):
    if os.getenv("AUTO_INIT_DB","false").lower()=="true": init_db()
    yield

app=FastAPI(title="Private Enterprise Document Intelligence API",version="0.1.0",lifespan=lifespan)

class AskRequest(BaseModel):
    question: str=Field(min_length=3,max_length=2000)

@app.get("/health")
def health():
    return {"status":"ok","service":"private-ai-document-intelligence","llm_provider":"local-ollama"}

@app.post("/documents")
async def upload_document(file: UploadFile=File(...),x_tenant_id: str=Header(...,alias="X-Tenant-Id")):
    if not x_tenant_id.strip() or len(x_tenant_id)>128: raise HTTPException(400,"Invalid tenant ID")
    raw=await file.read()
    try: pages=extract_document(file.filename or "",raw)
    except ValueError as exc: raise HTTPException(400,str(exc)) from exc
    if not pages: raise HTTPException(422,"No extractable text found")
    doc_id=uuid.uuid4(); records=[]
    try:
        for page in pages:
            for part in chunk_text(page["text"]):
                records.append((uuid.uuid4(),page["page"],part,await embed(part)))
        with db_connect() as conn:
            conn.execute("INSERT INTO documents(id,tenant_id,filename,content_type) VALUES(%s,%s,%s,%s)",
                         (doc_id,x_tenant_id,file.filename or "document",file.content_type))
            conn.executemany("""INSERT INTO document_chunks(id,document_id,tenant_id,filename,page_number,content,embedding)
                VALUES(%s,%s,%s,%s,%s,%s,%s::vector)""",
                [(r[0],doc_id,x_tenant_id,file.filename or "document",r[1],r[2],"["+",".join(map(str,r[3]))+"]") for r in records])
            conn.commit()
        await publish({"type":"document.indexed","tenant_id":x_tenant_id,"document_id":str(doc_id)})
    except (httpx.HTTPError,psycopg.Error,RuntimeError) as exc:
        raise HTTPException(503,f"Document processing failed: {type(exc).__name__}") from exc
    return {"document_id":str(doc_id),"filename":file.filename,"chunks_indexed":len(records),"status":"indexed"}

@app.post("/ask")
async def ask(request: AskRequest,x_tenant_id: str=Header(...,alias="X-Tenant-Id")):
    if not x_tenant_id.strip() or len(x_tenant_id)>128: raise HTTPException(400,"Invalid tenant ID")
    try:
        vector=await embed(request.question); literal="["+",".join(map(str,vector))+"]"
        with db_connect() as conn:
            rows=conn.execute("""SELECT filename,page_number,content FROM document_chunks
              WHERE tenant_id=%s ORDER BY embedding <=> %s::vector LIMIT 5""",(x_tenant_id,literal)).fetchall()
        passages=[{"source":r[0],"page":r[1],"text":r[2]} for r in rows]
        if not passages:
            return {"answer":"I couldn't find supporting information in this tenant's uploaded documents.","citations":[],"grounded":False}
        result=await answer(build_grounded_prompt(request.question,passages))
        return {"answer":result,"citations":[{"filename":p["source"],"page":p["page"]} for p in passages],"grounded":True}
    except (httpx.HTTPError,psycopg.Error,RuntimeError) as exc:
        raise HTTPException(503,f"Question processing failed: {type(exc).__name__}") from exc
