"""Explicit setup only. Runtime analysis never downloads a model."""
import hashlib
import json
from pathlib import Path
import os
from huggingface_hub import snapshot_download

root = Path(os.environ['LOCALAPPDATA']) / 'SMSR' / 'models'
target = root / 'whisper-tiny'
revision = 'd90ca5fe260221311c53c58e660288d3deb8d356'
snapshot_download('Systran/faster-whisper-tiny', revision=revision, local_dir=target,
                  allow_patterns=['config.json', 'model.bin', 'tokenizer.json', 'vocabulary.txt'])
files = {}
for name in ['config.json', 'model.bin', 'tokenizer.json', 'vocabulary.txt']:
    path = target / name
    files[name] = hashlib.sha256(path.read_bytes()).hexdigest()
(target / 'smsr-model-manifest.json').write_text(json.dumps(dict(repository='Systran/faster-whisper-tiny',
    revision=revision, files=files), indent=2), encoding='utf-8')
print(json.dumps(dict(model=str(target), revision=revision, files=files)))
