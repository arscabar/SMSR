"""Embed every token window rather than silently dropping a passage tail."""
import numpy as np
from tokenizers import Tokenizer

def windows(text, tokenizer, budget):
    start = 0
    while start < len(text):
        end = min(start + 1000, len(text))
        while len(tokenizer.encode(text[start:end]).ids) > budget:
            end = start + max(1, (end - start) // 2)
            if end == start + 1:
                break
        yield text[start:end]
        start = end

def embed_full(model, texts):
    model.model.load_onnx_model() if model.model.tokenizer is None else None
    tokenizer = Tokenizer.from_str(model.model.tokenizer.to_str())
    budget = min(192, (tokenizer.truncation or {}).get('max_length', 512) - 2)
    tokenizer.no_truncation()
    tokenizer.no_padding()
    parts, counts = [], []
    for text in texts:
        segments = list(windows(text, tokenizer, budget)) or ['']
        counts.append(len(segments))
        parts.extend(segments)
    values = np.asarray(list(model.embed(parts, batch_size=32)), dtype=np.float32)
    offset, result = 0, []
    for count in counts:
        pooled = values[offset:offset + count].mean(axis=0)
        norm = np.linalg.norm(pooled)
        result.append((pooled / norm if norm else pooled).tolist())
        offset += count
    return result
