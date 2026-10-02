"""Explicit setup of English/Korean OCR data from pinned upstream."""
import hashlib
import json
import os
from pathlib import Path
from urllib.request import urlopen

revision = '87416418657359cb625c412a48b6e1d6d41c29bd'
root = Path(os.environ['LOCALAPPDATA']) / 'SMSR/models/tessdata'
root.mkdir(parents=True, exist_ok=True)
hashes = {}
for name in ['eng', 'kor']:
    url = f'https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/{revision}/{name}.traineddata'
    with urlopen(url, timeout=60) as response:
        raw = response.read(20_000_001)
    if len(raw) > 20_000_000:
        raise ValueError('OCR model size limit')
    (root / (name+'.traineddata')).write_bytes(raw)
    hashes[name] = hashlib.sha256(raw).hexdigest()
(root / 'smsr-model-manifest.json').write_text(json.dumps(dict(repository='tesseract-ocr/tessdata_fast',
    revision=revision, files=hashes), indent=2), encoding='utf-8')
print(json.dumps(dict(root=str(root), revision=revision, hashes=hashes)))
