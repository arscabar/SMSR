import os
from pathlib import Path

MODEL = 'sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2'
REVISION = 'faf4aa4225822f3bc6376869cb1164e8e3feedd0'

def model(download=False):
    if not download:
        os.environ['HF_HUB_OFFLINE'] = '1'
    from fastembed import TextEmbedding
    from huggingface_hub import snapshot_download
    cache = Path(os.environ['LOCALAPPDATA']) / 'SMSR' / 'graph-models'
    snapshot = snapshot_download('qdrant/paraphrase-multilingual-MiniLM-L12-v2-onnx-Q',
        revision=REVISION, cache_dir=str(cache), local_files_only=not download,
        allow_patterns=['config.json','tokenizer.json','tokenizer_config.json','special_tokens_map.json','model_optimized.onnx'])
    return TextEmbedding(MODEL, cache_dir=str(cache), threads=2, local_files_only=True, specific_model_path=snapshot)

def run(request):
    texts = request['texts']
    if not isinstance(texts, list) or not 1 <= len(texts) <= 256 or any(not isinstance(t, str) or len(t) > 4000 for t in texts):
        raise ValueError('Embedding batch must contain 1–256 texts of at most 4000 characters')
    if request.get('full', False):
        from full_embedding import embed_full
        return dict(model=MODEL, vectors=embed_full(model(), texts))
    return dict(model=MODEL, vectors=[v.tolist() for v in model().embed(texts, batch_size=32)])

if __name__ == '__main__':
    print(next(iter(model(download=True).embed(['모델 준비 확인']))).shape)
