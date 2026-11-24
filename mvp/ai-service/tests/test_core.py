import pytest
from app.core import build_grounded_prompt, chunk_text, extract_document, validate_filename

def test_chunk_text_empty():
    assert chunk_text(" \n ") == []

def test_chunk_text_covers_input():
    text = " ".join(f"word{i}" for i in range(150))
    chunks = chunk_text(text, size=120, overlap=20)
    assert len(chunks) > 1 and "word0" in chunks[0] and "word149" in chunks[-1]

def test_chunk_text_rejects_bad_overlap():
    with pytest.raises(ValueError):
        chunk_text("hello", size=10, overlap=10)

def test_validate_supported_extensions():
    assert validate_filename("policy.PDF") == ".pdf"
    assert validate_filename("notes.txt") == ".txt"

def test_reject_unsupported_extension():
    with pytest.raises(ValueError):
        validate_filename("payload.exe")

def test_extract_plain_text():
    assert extract_document("policy.txt", b"Coverage begins immediately.") == [{"page": 1, "text": "Coverage begins immediately."}]

def test_reject_empty_file():
    with pytest.raises(ValueError):
        extract_document("empty.txt", b"")

def test_grounded_prompt_cites_source_and_treats_document_as_untrusted():
    prompt = build_grounded_prompt("Renewal date?", [{"source":"contract.pdf","page":4,"text":"Renewal is 1 January."}])
    assert "[Source: contract.pdf, page 4]" in prompt
    assert "untrusted data" in prompt
    assert "Renewal date?" in prompt

def test_grounded_prompt_without_evidence():
    assert "No matching evidence" in build_grounded_prompt("Question?", [])
